using SQLite;

namespace FilaSinuca.Services;

// ------------------ Tabelas SQLite ------------------
[Table("QueueEntry")]
public class QueueEntry
{
    [PrimaryKey, AutoIncrement] public int Seq { get; set; }
    [Indexed] public string PessoaId { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
}

[Table("TableState")]
public class TableState
{
    [PrimaryKey] public int Id { get; set; } = 1;
    public string? Jogador1Id { get; set; }
    public string? Jogador2Id { get; set; }
}

[Table("VitoriaSeq")]
public class VitoriaSeq
{
    [PrimaryKey] public string PessoaId { get; set; } = string.Empty;
    public int Vitorias { get; set; }
}

// ------------------ DTO do estado ------------------
public class EstadoDTO
{
    public List<(string Id, string Nome)> Fila { get; set; } = new();
    public (string? Id, string? Nome) Jogador1 { get; set; }
    public (string? Id, string? Nome) Jogador2 { get; set; }
    public Dictionary<string, int> VitoriasSeguidas { get; set; } = new();
    public int LimiteVitoriasConsecutivas { get; set; } = 3;
}
