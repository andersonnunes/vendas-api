namespace VendasApi.Application.Veiculos;

/// <summary>Dois envios tentaram criar a mesma cópia; o caso de uso tenta de novo.</summary>
public sealed class ConcorrenciaCopiaVeiculoException() : Exception("A cópia do veículo foi alterada por outro envio.");
