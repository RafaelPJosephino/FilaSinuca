using SQLite;
using Microsoft.Maui.Storage;
using FilaSinuca.Models;

namespace FilaSinuca.Services;

public interface IStorageService
{
    Task SaveAsync(EstadoDTO estado);
    Task<EstadoDTO> LoadAsync();
    Task ClearQueueAsync();
    Task ClearTableAsync();
}

public class StorageService : IStorageService
{
    private readonly string _dbPath;
    private SQLiteAsyncConnection _conn => _lazyConn.Value;
    private readonly Lazy<SQLiteAsyncConnection> _lazyConn;

    private const string PrefLimite = "LimiteVitoriasConsecutivas";

    public StorageService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "fila_sinuca.db3");
        _lazyConn = new Lazy<SQLiteAsyncConnection>(() => new SQLiteAsyncConnection(_dbPath));
    }

    private async Task EnsureCreatedAsync()
    {
        await _conn.CreateTableAsync<QueueEntry>();
        await _conn.CreateTableAsync<TableState>();
        await _conn.CreateTableAsync<VitoriaSeq>();
        // Garante registro único de estado da mesa
        var ts = await _conn.Table<TableState>().FirstOrDefaultAsync();
        if (ts == null) await _conn.InsertAsync(new TableState { Id = 1 });
    }

    public async Task SaveAsync(EstadoDTO estado)
    {
        await EnsureCreatedAsync();

        // Limpa e recria fila
        await _conn.DeleteAllAsync<QueueEntry>();
        foreach (var (id, nome) in estado.Fila)
            await _conn.InsertAsync(new QueueEntry { PessoaId = id, Nome = nome });

        // Mesa
        await _conn.UpdateAsync(new TableState
        {
            Id = 1,
            Jogador1Id = estado.Jogador1.Id,
            Jogador2Id = estado.Jogador2.Id
        });

        // Vitórias
        await _conn.DeleteAllAsync<VitoriaSeq>();
        foreach (var kv in estado.VitoriasSeguidas)
            await _conn.InsertAsync(new VitoriaSeq { PessoaId = kv.Key, Vitorias = kv.Value });

        // Regras (Preferences)
        Preferences.Set(PrefLimite, estado.LimiteVitoriasConsecutivas);
    }

    public async Task<EstadoDTO> LoadAsync()
    {
        await EnsureCreatedAsync();
        var dto = new EstadoDTO();

        // Fila
        var fila = await _conn.Table<QueueEntry>().OrderBy(q => q.Seq).ToListAsync();
        dto.Fila = fila.Select(f => (f.PessoaId, f.Nome)).ToList();

        // Mesa
        var ts = await _conn.Table<TableState>().FirstAsync();
        // Para nomes da mesa, procura na fila; se não tiver, deixam Nome nulo e UI mostra "-"
        dto.Jogador1 = (ts.Jogador1Id, fila.FirstOrDefault(x => x.PessoaId == ts.Jogador1Id)?.Nome);
        dto.Jogador2 = (ts.Jogador2Id, fila.FirstOrDefault(x => x.PessoaId == ts.Jogador2Id)?.Nome);

        // Vitórias
        var vits = await _conn.Table<VitoriaSeq>().ToListAsync();
        dto.VitoriasSeguidas = vits.ToDictionary(v => v.PessoaId, v => v.Vitorias);

        // Regras
        dto.LimiteVitoriasConsecutivas = Preferences.Get(PrefLimite, 3);

        return dto;
    }

    public async Task ClearQueueAsync()
    {
        await EnsureCreatedAsync();
        await _conn.DeleteAllAsync<QueueEntry>();
    }

    public async Task ClearTableAsync()
    {
        await EnsureCreatedAsync();
        await _conn.UpdateAsync(new TableState { Id = 1, Jogador1Id = null, Jogador2Id = null });
    }
}
