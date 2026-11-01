using Microsoft.Extensions.Time.Testing;
using VendasApi.Application.Vendas;
using VendasApi.Domain.Veiculos;
using VendasApi.Domain.Vendas;

namespace VendasApi.Tests.Aplicacao;

public class EfetuarVendaCasoDeUsoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecutarAsync_CpfInvalido_NaoChegaAoRepositorio()
    {
        var repositorio = new RepositorioFalso();
        var casoDeUso = new EfetuarVendaCasoDeUso(repositorio, new FakeTimeProvider(Agora));

        await Assert.ThrowsAsync<VendaInvalidaException>(() =>
            casoDeUso.ExecutarAsync(new EntradaEfetuarVenda(Guid.NewGuid(), "11111111111", new DateOnly(2026, 11, 2)), CancellationToken.None));

        Assert.Equal(0, repositorio.Chamadas);
    }

    [Fact]
    public async Task ExecutarAsync_VeiculoDisponivel_ReservaAcopiaERegistraAVenda()
    {
        var copia = Copia();
        var repositorio = new RepositorioFalso { Copia = copia };
        var casoDeUso = new EfetuarVendaCasoDeUso(repositorio, new FakeTimeProvider(Agora));

        var dto = await casoDeUso.ExecutarAsync(new EntradaEfetuarVenda(copia.Id, "52998224725", new DateOnly(2026, 11, 2)), CancellationToken.None);

        Assert.Equal(StatusVeiculoCopia.Reservado, copia.Status);
        Assert.Equal(1, repositorio.VendasRegistradas);
        Assert.Equal("***.982.247-**", dto.CpfMascarado);
        Assert.Equal("reservada", dto.Status);
    }

    [Fact]
    public async Task ExecutarAsync_VeiculoNaoExiste_LancaVeiculoNaoEncontrado()
    {
        var repositorio = new RepositorioFalso { Copia = null };
        var casoDeUso = new EfetuarVendaCasoDeUso(repositorio, new FakeTimeProvider(Agora));

        await Assert.ThrowsAsync<VeiculoNaoEncontradoException>(() =>
            casoDeUso.ExecutarAsync(new EntradaEfetuarVenda(Guid.NewGuid(), "52998224725", new DateOnly(2026, 11, 2)), CancellationToken.None));

        Assert.Equal(0, repositorio.VendasRegistradas);
    }

    [Fact]
    public async Task ExecutarAsync_VeiculoJaReservado_PropagaSemRegistrarVenda()
    {
        var copia = Copia();
        copia.Reservar();
        var repositorio = new RepositorioFalso { Copia = copia };
        var casoDeUso = new EfetuarVendaCasoDeUso(repositorio, new FakeTimeProvider(Agora));

        await Assert.ThrowsAsync<VeiculoIndisponivelParaVendaException>(() =>
            casoDeUso.ExecutarAsync(new EntradaEfetuarVenda(copia.Id, "52998224725", new DateOnly(2026, 11, 2)), CancellationToken.None));

        Assert.Equal(0, repositorio.VendasRegistradas);
    }

    private static VeiculoCopia Copia() => VeiculoCopia.Criar(
        new DadosSnapshot(Guid.NewGuid(), "Toyota", "Corolla", 2024, "Prata", 100m, "9BWZZZ377VT004251", null, StatusVeiculoCopia.Disponivel, Agora),
        Agora.Year);

    private sealed class RepositorioFalso : IVendasRepositorio
    {
        public VeiculoCopia? Copia { get; init; }
        public int Chamadas { get; private set; }
        public int VendasRegistradas { get; private set; }

        public Task EfetuarVendaAsync(Guid veiculoId, Func<VeiculoCopia?, Venda> efetuar, CancellationToken cancellationToken)
        {
            Chamadas++;
            efetuar(Copia);
            VendasRegistradas++;
            return Task.CompletedTask;
        }
    }
}
