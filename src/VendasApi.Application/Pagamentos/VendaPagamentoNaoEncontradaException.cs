namespace VendasApi.Application.Pagamentos;

public sealed class VendaPagamentoNaoEncontradaException(string codigoPagamento)
    : Exception("Nenhuma venda corresponde ao código de pagamento informado.")
{
    public string CodigoPagamento { get; } = codigoPagamento;
}
