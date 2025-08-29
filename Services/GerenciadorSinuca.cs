using FilaSinuca.Models;
using System.Collections.Generic;
using System.Linq;

namespace FilaSinuca.Services;

public enum ResultadoPartida
{
    VitoriaP1,
    VitoriaP2
}

public class GerenciadorSinuca
{
    private Queue<Pessoa> _fila = new();
    private Pessoa? _jogador1;
    private Pessoa? _jogador2;

    private readonly Dictionary<string, int> _vitoriasSeguidas = new();

    public int LimiteVitoriasConsecutivas { get; set; } = 3;

    public IReadOnlyList<Pessoa> GetFila() => _fila.ToList();
    public (Pessoa? Jogador1, Pessoa? Jogador2) GetMesa() => (_jogador1, _jogador2);
    public Dictionary<string, int> GetMapaVitorias() => new(_vitoriasSeguidas);

    public void Entrar(Pessoa pessoa) => _fila.Enqueue(pessoa);

    public void ComecarPartida()
    {
        if (_jogador1 == null && _fila.Count > 0) _jogador1 = _fila.Dequeue();
        if (_jogador2 == null && _fila.Count > 0) _jogador2 = _fila.Dequeue();

        if (_jogador1 == null || _jogador2 == null)
            throw new InvalidOperationException("Não há jogadores suficientes para começar a partida.");
    }

    public void LimparMesa()
    {
        if (_jogador1 != null) _fila.Enqueue(_jogador1);
        if (_jogador2 != null) _fila.Enqueue(_jogador2);
        _jogador1 = null;
        _jogador2 = null;
    }

    public void LimparFila()
    {
        _fila = new Queue<Pessoa>();
    }

    public void LimparTudo()
    {
        LimparMesa();
        LimparFila();
    }

    public void RemoverJogadorMesa(int posicao)
    {
        if (posicao == 1 && _jogador1 != null) _jogador1 = null;
        else if (posicao == 2 && _jogador2 != null) _jogador2 = null;
    }

    public bool RemoverDaFilaPorId(string id)
        => RebuildQueueExcluding(p => p.Id == id) > 0;

    public int RemoverDaFilaPorNome(string nome)
        => RebuildQueueExcluding(p => string.Equals(p.Nome.Trim(), nome.Trim(), StringComparison.OrdinalIgnoreCase));

    public bool RemoverDaFilaPorIndice(int indiceZeroBase)
    {
        if (indiceZeroBase < 0 || indiceZeroBase >= _fila.Count) return false;
        var list = _fila.ToList();
        list.RemoveAt(indiceZeroBase);
        _fila = new Queue<Pessoa>(list);
        return true;
    }

    public bool RemoverPessoa(Pessoa pessoa)
    {
        if (_jogador1?.Id == pessoa.Id) { _jogador1 = null; return true; }
        if (_jogador2?.Id == pessoa.Id) { _jogador2 = null; return true; }
        return RemoverDaFilaPorId(pessoa.Id);
    }

    private int RebuildQueueExcluding(Func<Pessoa, bool> predicate)
    {
        var list = _fila.ToList();
        int removed = list.RemoveAll(p => predicate(p));
        _fila = new Queue<Pessoa>(list);
        return removed;
    }

    public void RegistrarResultado(ResultadoPartida resultado)
    {
        if (_jogador1 == null || _jogador2 == null)
            throw new InvalidOperationException("Não há partida em andamento.");

        var p1 = _jogador1;
        var p2 = _jogador2;

        var vencedor = (resultado == ResultadoPartida.VitoriaP1) ? p1 : p2;
        var perdedor = (resultado == ResultadoPartida.VitoriaP1) ? p2 : p1;

        if (!_vitoriasSeguidas.ContainsKey(vencedor.Id)) _vitoriasSeguidas[vencedor.Id] = 0;
        _vitoriasSeguidas[vencedor.Id] += 1;
        _vitoriasSeguidas[perdedor.Id] = 0;

        // Perdedor sai e vai para o fim
        _fila.Enqueue(perdedor);

        // Vencedor atingiu limite -> ambos saem; ordem: vencedor, perdedor
        if (_vitoriasSeguidas[vencedor.Id] >= LimiteVitoriasConsecutivas)
        {
            // remover "perdedor" que acabamos de enfileirar e reordenar para vencedor->perdedor
            var list = _fila.ToList();
            var removed = list.RemoveAll(x => x.Id == perdedor.Id);
            list.Add(vencedor);
            list.Add(perdedor);
            _fila = new Queue<Pessoa>(list);

            _jogador1 = null;
            _jogador2 = null;

            _vitoriasSeguidas[vencedor.Id] = 0; // zera ao sair
            return;
        }

        // Caso normal: vencedor permanece
        _jogador1 = vencedor;
        _jogador2 = null;
        if (_fila.Count > 0) _jogador2 = _fila.Dequeue();
    }

    // ---------- Persistência (DTO) ----------
    public EstadoDTO ExportarEstado()
    {
        var dto = new EstadoDTO
        {
            Fila = _fila.Select(p => (p.Id, p.Nome)).ToList(),
            Jogador1 = (_jogador1?.Id, _jogador1?.Nome),
            Jogador2 = (_jogador2?.Id, _jogador2?.Nome),
            VitoriasSeguidas = new Dictionary<string, int>(_vitoriasSeguidas),
            LimiteVitoriasConsecutivas = this.LimiteVitoriasConsecutivas
        };
        return dto;
    }

    public void ImportarEstado(EstadoDTO dto)
    {
        _fila = new Queue<Pessoa>(dto.Fila.Select(x => new Pessoa { Id = x.Id, Nome = x.Nome }));

        _jogador1 = dto.Jogador1.Id != null ? new Pessoa { Id = dto.Jogador1.Id, Nome = dto.Jogador1.Nome ?? "-" } : null;
        _jogador2 = dto.Jogador2.Id != null ? new Pessoa { Id = dto.Jogador2.Id, Nome = dto.Jogador2.Nome ?? "-" } : null;

        _vitoriasSeguidas.Clear();
        foreach (var kv in dto.VitoriasSeguidas) _vitoriasSeguidas[kv.Key] = kv.Value;

        this.LimiteVitoriasConsecutivas = dto.LimiteVitoriasConsecutivas;
    }
}
