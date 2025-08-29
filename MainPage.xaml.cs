using FilaSinuca.Models;
using FilaSinuca.Services;

namespace FilaSinuca;

public partial class MainPage : ContentPage
{
    private readonly GerenciadorSinuca _g = new();
    private readonly IStorageService _storage = new StorageService();
    private Pessoa? _selecionadoFila;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Load persisted state
        var estado = await _storage.LoadAsync();
        _g.ImportarEstado(estado);
        EntryLimiteVits.Text = _g.LimiteVitoriasConsecutivas.ToString();
        RefreshUI();
    }

    // ----------------- Helpers -----------------
    private void RefreshUI()
    {
        var (p1, p2) = _g.GetMesa();
        Jogador1Label.Text = p1?.Nome ?? "-";
        Jogador2Label.Text = p2?.Nome ?? "-";

        if (p1 != null) VitsP1.Text = $"Vitórias seguidas: {_g.GetMapaVitorias().GetValueOrDefault(p1.Id, 0)}";
        else VitsP1.Text = string.Empty;

        if (p2 != null) VitsP2.Text = $"Vitórias seguidas: {_g.GetMapaVitorias().GetValueOrDefault(p2.Id, 0)}";
        else VitsP2.Text = string.Empty;

        FilaView.ItemsSource = _g.GetFila();
    }

    private async Task PersistAsync()
    {
        // Sincroniza limite vindo da UI
        if (int.TryParse(EntryLimiteVits.Text, out var v) && v > 0)
            _g.LimiteVitoriasConsecutivas = v;

        await _storage.SaveAsync(_g.ExportarEstado());
    }

    private async Task<bool> Confirm(string titulo, string msg)
    {
        return await DisplayAlert(titulo, msg, "Sim", "Não");
    }

    // ----------------- Ações UI -----------------
    private async void OnEntrarClicked(object sender, EventArgs e)
    {
        var nome = (NomeEntry.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            await DisplayAlert("Aviso", "Informe um nome.", "OK");
            return;
        }

        _g.Entrar(new Pessoa { Nome = nome });
        NomeEntry.Text = string.Empty;
        RefreshUI();
        await PersistAsync();
    }

    private async void OnComecarClicked(object sender, EventArgs e)
    {
        try
        {
            _g.ComecarPartida();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Aviso", ex.Message, "OK");
        }
        finally
        {
            RefreshUI();
            await PersistAsync();
        }
    }

    private async void OnVitoriaP1(object sender, EventArgs e)
    {
        try { _g.RegistrarResultado(ResultadoPartida.VitoriaP1); }
        catch (Exception ex) { await DisplayAlert("Erro", ex.Message, "OK"); }
        RefreshUI();
        await PersistAsync();
    }

    private async void OnVitoriaP2(object sender, EventArgs e)
    {
        try { _g.RegistrarResultado(ResultadoPartida.VitoriaP2); }
        catch (Exception ex) { await DisplayAlert("Erro", ex.Message, "OK"); }
        RefreshUI();
        await PersistAsync();
    }

    private async void OnLimparMesa(object sender, EventArgs e)
    {
        if (!await Confirm("Limpar mesa", "Enviar P1 e P2 para o fim da fila e esvaziar a mesa?")) return;
        _g.LimparMesa();
        RefreshUI();
        await PersistAsync();
    }

    private async void OnLimparFila(object sender, EventArgs e)
    {
        if (!await Confirm("Limpar fila", "Remover TODAS as pessoas da fila?")) return;
        _g.LimparFila();
        RefreshUI();
        await PersistAsync();
    }

    private async void OnLimparTudo(object sender, EventArgs e)
    {
        if (!await Confirm("Limpar TUDO", "Limpar mesa (mandando P1/P2 para fila) e remover TODA a fila?")) return;
        _g.LimparTudo();
        RefreshUI();
        await PersistAsync();
    }

    private async void OnRemoverP1(object sender, EventArgs e)
    {
        if (!await Confirm("Remover P1", "Remover o Jogador 1 da mesa?")) return;
        _g.RemoverJogadorMesa(1);
        RefreshUI();
        await PersistAsync();
    }

    private async void OnRemoverP2(object sender, EventArgs e)
    {
        if (!await Confirm("Remover P2", "Remover o Jogador 2 da mesa?")) return;
        _g.RemoverJogadorMesa(2);
        RefreshUI();
        await PersistAsync();
    }

    // -------- Remoções na Fila --------
    private void OnFilaSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selecionadoFila = e.CurrentSelection?.FirstOrDefault() as Pessoa;
    }

    private async void OnSwipeRemover(object sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.BindingContext is Pessoa p)
        {
            if (!await Confirm("Remover da fila", $"Remover {p.Nome}?")) return;
            _g.RemoverPessoa(p);
            RefreshUI();
            await PersistAsync();
        }
    }

    private async void OnRemoverSelecionado(object sender, EventArgs e)
    {
        if (_selecionadoFila == null) { await DisplayAlert("Aviso", "Selecione alguém da fila.", "OK"); return; }
        if (!await Confirm("Remover selecionado", $"Remover {_selecionadoFila.Nome}?")) return;
        _g.RemoverPessoa(_selecionadoFila);
        _selecionadoFila = null;
        FilaView.SelectedItem = null;
        RefreshUI();
        await PersistAsync();
    }

    private async void OnRemoverPorNome(object sender, EventArgs e)
    {
        var nome = (EntryNomeRemover.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome)) { await DisplayAlert("Aviso", "Informe um nome.", "OK"); return; }
        if (!await Confirm("Remover por nome", $"Remover todos com nome exato \"{nome}\"?")) return;
        var qtd = _g.RemoverDaFilaPorNome(nome);
        await DisplayAlert("Remoção por nome", qtd > 0 ? $"Removidos: {qtd}" : "Nenhum encontrado.", "OK");
        EntryNomeRemover.Text = string.Empty;
        RefreshUI();
        await PersistAsync();
    }

    private async void OnRemoverPorId(object sender, EventArgs e)
    {
        var id = (EntryIdRemover.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(id)) { await DisplayAlert("Aviso", "Informe um ID.", "OK"); return; }
        if (!await Confirm("Remover por ID", $"Remover ID {id}?")) return;
        var ok = _g.RemoverDaFilaPorId(id);
        await DisplayAlert("Remoção por ID", ok ? "Removido." : "ID não encontrado na fila.", "OK");
        EntryIdRemover.Text = string.Empty;
        RefreshUI();
        await PersistAsync();
    }

}
