using VendasApi.Domain.Veiculos;
using VendasApi.Domain.Vendas;

namespace VendasApi.Application.Pagamentos;

public interface IProcessamentoPagamentoRepositorio
{
    /// <summary>
    /// Trava a venda pelo código de pagamento e a cópia do veículo, entrega ambas a <paramref name="aplicar"/>
    /// e confirma as alterações juntas. Se <paramref name="aplicar"/> lançar, nada é gravado.
    /// </summary>
    Task<ProcessamentoPagamento> ProcessarAsync(
        string codigoPagamento,
        Func<Venda?, VeiculoCopia?, ProcessamentoPagamento> aplicar,
        CancellationToken cancellationToken);
}
