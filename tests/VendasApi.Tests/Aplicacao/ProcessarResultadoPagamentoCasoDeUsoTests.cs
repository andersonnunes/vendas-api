using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using VendasApi.Application.Pagamentos;
using VendasApi.Application.Vendas;
using VendasApi.Domain;
using VendasApi.Domain.Veiculos;
using VendasApi.Domain.Vendas;

namespace VendasApi.Tests.Aplicacao;

public class ProcessarResultadoPagamentoCasoDeUsoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
    private const string Codigo = "codigo-teste";

    [Fact]
    public async Task Aprovado_VendaReservada_ConcluiENotificaVeiculosApi()
    {
        var repositorio = RepositorioComVenda(StatusVenda.Reservada);
        var notificador = new NotificadorFalso();
        var casoDeUso = Criar(repositorio, notificador);

        var alterou = await casoDeUso.ExecutarAsync(Codigo, ResultadoPagamento.Aprovado, CancellationToken.None);

        Assert.True(alterou);
        Assert.Equal(StatusVenda.Concluida, repositorio.Venda!.Status);
        Assert.Equal(StatusVeiculoCopia.Vendido, repositorio.Copia!.Status);
        Assert.Equal(1, notificador.Chamadas);
    }

    [Fact]
    public async Task Cancelado_VendaReservada_CancelaLiberaVeiculoENaoNotifica()
    {
        var repositorio = RepositorioComVenda(StatusVenda.Reservada);
        var notificador = new NotificadorFalso();
        var casoDeUso = Criar(repositorio, notificador);

        var alterou = await casoDeUso.ExecutarAsync(Codigo, ResultadoPagamento.Cancelado, CancellationToken.None);

        Assert.True(alterou);
        Assert.Equal(StatusVenda.Cancelada, repositorio.Venda!.Status);
        Assert.Equal(StatusVeiculoCopia.Disponivel, repositorio.Copia!.Status);
        Assert.Equal(0, notificador.Chamadas);
    }

    [Fact]
    public async Task Aprovado_VendaJaConcluida_EIdempotenteENaoNotificaDeNovo()
    {
        var repositorio = RepositorioComVenda(StatusVenda.Concluida);
        var notificador = new NotificadorFalso();
        var casoDeUso = Criar(repositorio, notificador);

        var alterou = await casoDeUso.ExecutarAsync(Codigo, ResultadoPagamento.Aprovado, CancellationToken.None);

        Assert.False(alterou);
        Assert.Equal(0, notificador.Chamadas);
    }

    [Fact]
    public async Task Cancelado_VendaConcluida_LancaTransicaoInvalida()
    {
        var repositorio = RepositorioComVenda(StatusVenda.Concluida);
        var casoDeUso = Criar(repositorio, new NotificadorFalso());

        await Assert.ThrowsAsync<TransicaoInvalidaException>(() =>
            casoDeUso.ExecutarAsync(Codigo, ResultadoPagamento.Cancelado, CancellationToken.None));
    }

    [Fact]
    public async Task CodigoDesconhecido_LancaVendaNaoEncontrada()
    {
        var repositorio = new RepositorioFalso { Venda = null, Copia = null };
        var casoDeUso = Criar(repositorio, new NotificadorFalso());

        await Assert.ThrowsAsync<VendaPagamentoNaoEncontradaException>(() =>
            casoDeUso.ExecutarAsync(Codigo, ResultadoPagamento.Aprovado, CancellationToken.None));
    }

    [Fact]
    public async Task FalhaNaNotificacao_NaoDesfazAConclusao()
    {
        var repositorio = RepositorioComVenda(StatusVenda.Reservada);
        var notificador = new NotificadorFalso { Falhar = true };
        var casoDeUso = Criar(repositorio, notificador);

        var alterou = await casoDeUso.ExecutarAsync(Codigo, ResultadoPagamento.Aprovado, CancellationToken.None);

        Assert.True(alterou);
        Assert.Equal(StatusVenda.Concluida, repositorio.Venda!.Status);
        Assert.Equal(1, notificador.Chamadas);
    }

    private static ProcessarResultadoPagamentoCasoDeUso Criar(RepositorioFalso repositorio, NotificadorFalso notificador) =>
        new(repositorio, notificador, new FakeTimeProvider(Agora), NullLogger<ProcessarResultadoPagamentoCasoDeUso>.Instance);

    private static RepositorioFalso RepositorioComVenda(StatusVenda status)
    {
        var copia = VeiculoCopia.Criar(
            new DadosSnapshot(Guid.NewGuid(), "Toyota", "Corolla", 2024, "Prata", 100m, "9BWZZZ377VT004251", null, StatusVeiculoCopia.Disponivel, Agora),
            Agora.Year);
        copia.Reservar();
        var venda = Venda.Criar(Guid.NewGuid(), copia.Id, "52998224725", new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2), Codigo, Agora);
        if (status == StatusVenda.Concluida)
        {
            venda.Concluir();
            copia.MarcarVendido();
        }
        return new RepositorioFalso { Venda = venda, Copia = copia };
    }

    private sealed class RepositorioFalso : IProcessamentoPagamentoRepositorio
    {
        public Venda? Venda { get; init; }
        public VeiculoCopia? Copia { get; init; }

        public Task<ProcessamentoPagamento> ProcessarAsync(string codigoPagamento, Func<Venda?, VeiculoCopia?, ProcessamentoPagamento> aplicar, CancellationToken cancellationToken) =>
            Task.FromResult(aplicar(Venda, Copia));
    }

    private sealed class NotificadorFalso : IVeiculosStatusNotificador
    {
        public int Chamadas { get; private set; }
        public bool Falhar { get; init; }

        public Task NotificarVendidoAsync(Guid veiculoId, DateTimeOffset ocorridoEm, CancellationToken cancellationToken)
        {
            Chamadas++;
            if (Falhar)
                throw new NotificacaoVeiculosFalhouException(veiculoId);
            return Task.CompletedTask;
        }
    }
}
