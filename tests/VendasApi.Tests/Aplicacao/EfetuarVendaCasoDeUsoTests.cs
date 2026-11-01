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
    public async Task ExecutarAsync_DadosValidos_RegistraAVendaEDevolveCpfMascarado()
    {
        var repositorio = new RepositorioFalso();
        var casoDeUso = new EfetuarVendaCasoDeUso(repositorio, new FakeTimeProvider(Agora));
        var veiculoId = Guid.NewGuid();

        var dto = await casoDeUso.ExecutarAsync(new EntradaEfetuarVenda(veiculoId, "52998224725", new DateOnly(2026, 11, 2)), CancellationToken.None);

        Assert.Equal(1, repositorio.Chamadas);
        Assert.Equal(veiculoId, repositorio.UltimoVeiculoId);
        Assert.Equal("***.982.247-**", dto.CpfMascarado);
        Assert.Equal("reservada", dto.Status);
    }

    [Fact]
    public async Task ExecutarAsync_VeiculoIndisponivel_PropagaAoChamador()
    {
        var repositorio = new RepositorioFalso { Excecao = new VeiculoIndisponivelParaVendaException() };
        var casoDeUso = new EfetuarVendaCasoDeUso(repositorio, new FakeTimeProvider(Agora));

        await Assert.ThrowsAsync<VeiculoIndisponivelParaVendaException>(() =>
            casoDeUso.ExecutarAsync(new EntradaEfetuarVenda(Guid.NewGuid(), "52998224725", new DateOnly(2026, 11, 2)), CancellationToken.None));
    }

    private sealed class RepositorioFalso : IVendasRepositorio
    {
        public int Chamadas { get; private set; }
        public Guid UltimoVeiculoId { get; private set; }
        public Exception? Excecao { get; init; }

        public Task ReservarVeiculoERegistrarVendaAsync(Guid veiculoId, Venda venda, CancellationToken cancellationToken)
        {
            Chamadas++;
            UltimoVeiculoId = veiculoId;
            if (Excecao is not null)
                throw Excecao;
            return Task.CompletedTask;
        }
    }
}
