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

    private void ResetStreak(Pessoa? p)
    {
        if (p != null) _vitoriasSeguidas[p.Id] = 0;
    }

    public void LimparMesa()
    {
        if (_jogador1 != null)
        {
            ResetStreak(_jogador1);
            _fila.Enqueue(_jogador1);
        }
        if (_jogador2 != null)
        {
            ResetStreak(_jogador2);
            _fila.Enqueue(_jogador2);
        }
        _jogador1 = null;
        _jogador2 = null;
    }

    public void LimparFila()
    {
        var ativosMesa = new HashSet<string>();
        if (_jogador1 != null) ativosMesa.Add(_jogador1.Id);
        if (_jogador2 != null) ativosMesa.Add(_jogador2.Id);

        foreach (var id in _vitoriasSeguidas.Keys.ToList())
            if (!ativosMesa.Contains(id)) _vitoriasSeguidas.Remove(id);

        _fila = new Queue<Pessoa>();
    }

    public void LimparTudo()
    {
        LimparMesa();
        LimparFila();
        _vitoriasSeguidas.Clear();
    }

    public void RemoverJogadorMesa(int posicao)
    {
        if (posicao == 1 && _jogador1 != null)
        {
            ResetStreak(_jogador1);
            _fila.Enqueue(_jogador1);
            _jogador1 = null;
        }
        else if (posicao == 2 && _jogador2 != null)
        {
            ResetStreak(_jogador2);
            _fila.Enqueue(_jogador2);
            _jogador2 = null;
        }
    }

    public bool RemoverDaFilaPorId(string id)
    {
        var removed = RebuildQueueExcluding(p => p.Id == id) > 0;
        if (removed) _vitoriasSeguidas.Remove(id);
        return removed;
    }

    public int RemoverDaFilaPorNome(string nome)
    {
        var removidos = new List<Pessoa>(_fila.Where(p => string.Equals(p.Nome.Trim(), nome.Trim(), StringComparison.OrdinalIgnoreCase)));
        var qtd = RebuildQueueExcluding(p => string.Equals(p.Nome.Trim(), nome.Trim(), StringComparison.OrdinalIgnoreCase));
        foreach (var p in removidos) _vitoriasSeguidas.Remove(p.Id);
        return qtd;
    }

    public bool RemoverDaFilaPorIndice(int indiceZeroBase)
    {
        if (indiceZeroBase < 0 || indiceZeroBase >= _fila.Count) return false;
        var list = _fila.ToList();
        var removido = list[indiceZeroBase];
        list.RemoveAt(indiceZeroBase);
        _fila = new Queue<Pessoa>(list);
        _vitoriasSeguidas.Remove(removido.Id);
        return true;
    }

    public bool RemoverPessoa(Pessoa pessoa)
    {
        if (_jogador1?.Id == pessoa.Id)
        {
            ResetStreak(_jogador1);
            _fila.Enqueue(_jogador1);
            _jogador1 = null;
            return true;
        }
        if (_jogador2?.Id == pessoa.Id)
        {
            ResetStreak(_jogador2);
            _fila.Enqueue(_jogador2);
            _jogador2 = null;
            return true;
        }

        var removed = RemoverDaFilaPorId(pessoa.Id);
        if (removed) _vitoriasSeguidas.Remove(pessoa.Id);
        return removed;
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
