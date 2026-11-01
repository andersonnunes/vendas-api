using Microsoft.Extensions.Logging;
using VendasApi.Application.Vendas;
using VendasApi.Domain;
using VendasApi.Domain.Veiculos;
using VendasApi.Domain.Vendas;

namespace VendasApi.Application.Pagamentos;

public sealed class ProcessarResultadoPagamentoCasoDeUso(
    IProcessamentoPagamentoRepositorio repositorio,
    IVeiculosStatusNotificador notificador,
    TimeProvider relogio,
    ILogger<ProcessarResultadoPagamentoCasoDeUso> logger)
{
    /// <returns>True quando a venda mudou de estado; false quando o resultado repetia o estado atual.</returns>
    public async Task<bool> ExecutarAsync(string codigoPagamento, ResultadoPagamento resultado, CancellationToken cancellationToken)
    {
        var ocorridoEm = relogio.GetUtcNow();

        var processamento = await repositorio.ProcessarAsync(
            codigoPagamento,
            (venda, copia) => Aplicar(codigoPagamento, venda, copia, resultado),
            cancellationToken);

        if (processamento.Concluiu)
            await NotificarAsync(processamento.VeiculoId, ocorridoEm, cancellationToken);

        return processamento.Alterou;
    }

    private static ProcessamentoPagamento Aplicar(
        string codigoPagamento,
        Venda? venda,
        VeiculoCopia? copia,
        ResultadoPagamento resultado)
    {
        if (venda is null)
            throw new VendaPagamentoNaoEncontradaException(codigoPagamento);
        if (copia is null)
            throw new VeiculoNaoEncontradoException(venda.VeiculoId);

        switch (resultado, venda.Status)
        {
            case (ResultadoPagamento.Aprovado, StatusVenda.Reservada):
                venda.Concluir();
                copia.MarcarVendido();
                return new ProcessamentoPagamento(Alterou: true, Concluiu: true, copia.Id);

            case (ResultadoPagamento.Cancelado, StatusVenda.Reservada):
                venda.Cancelar();
                copia.Liberar();
                return new ProcessamentoPagamento(Alterou: true, Concluiu: false, copia.Id);

            case (ResultadoPagamento.Aprovado, StatusVenda.Concluida):
            case (ResultadoPagamento.Cancelado, StatusVenda.Cancelada):
                return new ProcessamentoPagamento(Alterou: false, Concluiu: false, copia.Id);

            default:
                throw new TransicaoInvalidaException(
                    $"Pagamento {resultado.ToString().ToLowerInvariant()} não se aplica a uma venda {venda.Status.ToString().ToLowerInvariant()}.");
        }
    }

    /// <summary>Best-effort: a venda já está concluída; a falha só é registrada para acompanhamento.</summary>
    private async Task NotificarAsync(Guid veiculoId, DateTimeOffset ocorridoEm, CancellationToken cancellationToken)
    {
        try
        {
            await notificador.NotificarVendidoAsync(veiculoId, ocorridoEm, cancellationToken);
        }
        catch (NotificacaoVeiculosFalhouException excecao)
        {
            logger.LogWarning(excecao, "Venda do veículo {VeiculoId} concluída, mas veiculos-api não foi notificada.", veiculoId);
        }
    }
}
