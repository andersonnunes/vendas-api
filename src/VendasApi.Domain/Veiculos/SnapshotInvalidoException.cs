namespace VendasApi.Domain.Veiculos;

public sealed class SnapshotInvalidoException(IReadOnlyDictionary<string, string[]> erros)
    : Exception("Snapshot de veículo inválido.")
{
    public IReadOnlyDictionary<string, string[]> Erros { get; } = erros;
}
