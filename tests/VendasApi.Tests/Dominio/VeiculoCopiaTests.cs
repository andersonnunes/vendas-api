using VendasApi.Domain;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Tests.Dominio;

public class VeiculoCopiaTests
{
    private const int AnoAtual = 2026;
    private static readonly DateTimeOffset Antes = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Depois = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    private static DadosSnapshot Dados(
        StatusVeiculoCopia status = StatusVeiculoCopia.Disponivel,
        DateTimeOffset? atualizadoEm = null,
        decimal preco = 139900m,
        string modelo = "Corolla",
        Guid? id = null) => new(
            id ?? Guid.Parse("0196f0a4-0000-7000-8000-000000000001"),
            "Toyota",
            modelo,
            2024,
            "Prata",
            preco,
            "9BWZZZ377VT004251",
            "BRA2E19",
            status,
            atualizadoEm ?? Antes);

    [Fact]
    public void Criar_SnapshotValido_CriaACopia()
    {
        var copia = VeiculoCopia.Criar(Dados(), AnoAtual);

        Assert.Equal("Corolla", copia.Modelo);
        Assert.Equal(StatusVeiculoCopia.Disponivel, copia.Status);
        Assert.Equal(Antes, copia.AtualizadoEm);
    }

    [Fact]
    public void Criar_CamposInvalidos_ListaErrosPorCampo()
    {
        var invalido = Dados() with { Chassi = "CURTO", Preco = 10.005m, Ano = 1800, Placa = "XX" };

        var excecao = Assert.Throws<SnapshotInvalidoException>(() => VeiculoCopia.Criar(invalido, AnoAtual));

        Assert.Contains("chassi", excecao.Erros.Keys);
        Assert.Contains("preco", excecao.Erros.Keys);
        Assert.Contains("ano", excecao.Erros.Keys);
        Assert.Contains("placa", excecao.Erros.Keys);
    }

    [Fact]
    public void Criar_StatusReservado_NaoVemDoCatalogo()
    {
        Assert.Throws<SnapshotInvalidoException>(() =>
            VeiculoCopia.Criar(Dados(StatusVeiculoCopia.Reservado), AnoAtual));
    }

    [Fact]
    public void AplicarSnapshot_MaisRecente_AtualizaOsCampos()
    {
        var copia = VeiculoCopia.Criar(Dados(), AnoAtual);

        var aplicado = copia.AplicarSnapshot(Dados(atualizadoEm: Depois, modelo: "Corolla XEi"), AnoAtual);

        Assert.True(aplicado);
        Assert.Equal("Corolla XEi", copia.Modelo);
        Assert.Equal(Depois, copia.AtualizadoEm);
    }

    [Fact]
    public void AplicarSnapshot_MaisAntigo_NaoAlteraNada()
    {
        var copia = VeiculoCopia.Criar(Dados(atualizadoEm: Depois), AnoAtual);

        var aplicado = copia.AplicarSnapshot(Dados(atualizadoEm: Antes, modelo: "Antigo"), AnoAtual);

        Assert.False(aplicado);
        Assert.Equal("Corolla", copia.Modelo);
        Assert.Equal(Depois, copia.AtualizadoEm);
    }

    [Fact]
    public void AplicarSnapshot_MesmoSnapshot_EIdempotente()
    {
        var copia = VeiculoCopia.Criar(Dados(), AnoAtual);

        copia.AplicarSnapshot(Dados(), AnoAtual);

        Assert.Equal(Antes, copia.AtualizadoEm);
        Assert.Equal(StatusVeiculoCopia.Disponivel, copia.Status);
    }

    [Fact]
    public void AplicarSnapshot_DisponivelNaoSobrescreveReserva()
    {
        var copia = VeiculoCopia.Criar(Dados(), AnoAtual);
        copia.Reservar();

        copia.AplicarSnapshot(Dados(atualizadoEm: Depois, preco: 120000m), AnoAtual);

        Assert.Equal(StatusVeiculoCopia.Reservado, copia.Status);
        Assert.Equal(120000m, copia.Preco);
    }

    [Fact]
    public void AplicarSnapshot_VendidoPrevaleceSobreReserva()
    {
        var copia = VeiculoCopia.Criar(Dados(), AnoAtual);
        copia.Reservar();

        copia.AplicarSnapshot(Dados(StatusVeiculoCopia.Vendido, Depois), AnoAtual);

        Assert.Equal(StatusVeiculoCopia.Vendido, copia.Status);
    }

    [Fact]
    public void AplicarSnapshot_DisponivelDepoisDeVendidoReverte()
    {
        var copia = VeiculoCopia.Criar(Dados(StatusVeiculoCopia.Vendido), AnoAtual);

        copia.AplicarSnapshot(Dados(StatusVeiculoCopia.Disponivel, Depois), AnoAtual);

        Assert.Equal(StatusVeiculoCopia.Disponivel, copia.Status);
    }

    [Fact]
    public void Reservar_VeiculoJaReservado_LancaVeiculoIndisponivel()
    {
        var copia = VeiculoCopia.Criar(Dados(), AnoAtual);
        copia.Reservar();

        Assert.Throws<VeiculoIndisponivelParaVendaException>(() => copia.Reservar());
    }

    [Fact]
    public void MarcarVendido_VeiculoReservado_MudaParaVendido()
    {
        var copia = VeiculoCopia.Criar(Dados(), AnoAtual);
        copia.Reservar();

        copia.MarcarVendido();

        Assert.Equal(StatusVeiculoCopia.Vendido, copia.Status);
    }

    [Fact]
    public void Liberar_VeiculoReservado_VoltaParaDisponivel()
    {
        var copia = VeiculoCopia.Criar(Dados(), AnoAtual);
        copia.Reservar();

        copia.Liberar();

        Assert.Equal(StatusVeiculoCopia.Disponivel, copia.Status);
    }

    [Fact]
    public void MarcarVendido_VeiculoDisponivel_LancaTransicaoInvalida()
    {
        var copia = VeiculoCopia.Criar(Dados(), AnoAtual);

        Assert.Throws<TransicaoInvalidaException>(() => copia.MarcarVendido());
    }
}
