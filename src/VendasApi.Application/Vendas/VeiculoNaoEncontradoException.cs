namespace VendasApi.Application.Vendas;

public sealed class VeiculoNaoEncontradoException(Guid veiculoId)
    : Exception("Veículo não encontrado na cópia local.")
{
    public Guid VeiculoId { get; } = veiculoId;
}
