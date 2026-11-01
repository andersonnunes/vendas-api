namespace VendasApi.Domain.Veiculos;

/// <summary>Veículo reservado ou vendido não pode ser reservado de novo.</summary>
public sealed class VeiculoIndisponivelParaVendaException()
    : Exception("Só um veículo disponível pode ser reservado.");
