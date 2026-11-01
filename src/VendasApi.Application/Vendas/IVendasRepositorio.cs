using VendasApi.Domain.Vendas;

namespace VendasApi.Application.Vendas;

public interface IVendasRepositorio
{
    /// <summary>
    /// Reserva o veículo e registra a venda numa única operação atômica: a linha do veículo é travada,
    /// a reserva é aplicada e a venda é gravada, ou nada é gravado.
    /// </summary>
    /// <exception cref="VeiculoNaoEncontradoException">O veículo não existe na cópia local.</exception>
    /// <exception cref="Domain.Veiculos.VeiculoIndisponivelParaVendaException">O veículo já está reservado ou vendido.</exception>
    Task ReservarVeiculoERegistrarVendaAsync(Guid veiculoId, Venda venda, CancellationToken cancellationToken);
}
