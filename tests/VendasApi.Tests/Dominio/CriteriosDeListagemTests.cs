using VendasApi.Domain.Veiculos;

namespace VendasApi.Tests.Dominio;

public class CriteriosDeListagemTests
{
    private static VeiculoCopia Copia(StatusVeiculoCopia status)
    {
        var dados = new DadosSnapshot(
            Guid.NewGuid(), "Toyota", "Corolla", 2024, "Prata", 100m, "9BWZZZ377VT004251", null, status, DateTimeOffset.UtcNow);
        return VeiculoCopia.Criar(dados, DateTime.UtcNow.Year);
    }

    [Fact]
    public void EstaDisponivelParaVenda_SoAceitaDisponivel()
    {
        var criterio = VeiculoCopia.EstaDisponivelParaVenda.Compile();

        Assert.True(criterio(Copia(StatusVeiculoCopia.Disponivel)));
        Assert.False(criterio(Copia(StatusVeiculoCopia.Vendido)));
    }

    [Fact]
    public void EstaVendido_SoAceitaVendido()
    {
        var criterio = VeiculoCopia.EstaVendido.Compile();

        Assert.True(criterio(Copia(StatusVeiculoCopia.Vendido)));
        Assert.False(criterio(Copia(StatusVeiculoCopia.Disponivel)));
    }
}
