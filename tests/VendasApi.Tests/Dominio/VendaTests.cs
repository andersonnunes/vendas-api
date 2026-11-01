using VendasApi.Domain.Vendas;

namespace VendasApi.Tests.Dominio;

public class VendaTests
{
    // CPF de exemplo com dígitos verificadores válidos (usado apenas em testes).
    private const string CpfValido = "52998224725";
    private static readonly DateOnly Hoje = new(2026, 11, 2);
    private static readonly DateTimeOffset Agora = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    private static Venda Criar(string cpf = CpfValido, DateOnly? dataVenda = null) => Venda.Criar(
        Guid.NewGuid(), Guid.NewGuid(), cpf, dataVenda ?? Hoje, Hoje, "codigo", Agora);

    [Fact]
    public void Criar_DadosValidos_NasceReservadaComCpfNormalizado()
    {
        var venda = Criar("529.982.247-25");

        Assert.Equal(StatusVenda.Reservada, venda.Status);
        Assert.Equal(CpfValido, venda.Cpf);
        Assert.Equal("***.982.247-**", VendasApi.Domain.Vendas.CpfValidacao.Mascarar(venda.Cpf));
    }

    [Theory]
    [InlineData("52998224726")]
    [InlineData("11111111111")]
    [InlineData("1234")]
    [InlineData("1234567890a")]
    public void Criar_CpfInvalido_LancaErroNoCampoCpf(string cpf)
    {
        var excecao = Assert.Throws<VendaInvalidaException>(() => Criar(cpf));

        Assert.True(excecao.Erros.ContainsKey("cpf"));
    }

    [Fact]
    public void Criar_DataFutura_LancaErroNoCampoDataVenda()
    {
        var excecao = Assert.Throws<VendaInvalidaException>(() => Criar(dataVenda: Hoje.AddDays(1)));

        Assert.True(excecao.Erros.ContainsKey("dataVenda"));
    }

    [Fact]
    public void Criar_DataMaisDeTrintaDiasAntes_LancaErroNoCampoDataVenda()
    {
        var excecao = Assert.Throws<VendaInvalidaException>(() => Criar(dataVenda: Hoje.AddDays(-31)));

        Assert.True(excecao.Erros.ContainsKey("dataVenda"));
    }

    [Fact]
    public void Criar_DataTrintaDiasAntes_EhAceita()
    {
        var venda = Criar(dataVenda: Hoje.AddDays(-30));

        Assert.Equal(Hoje.AddDays(-30), venda.DataVenda);
    }
}
