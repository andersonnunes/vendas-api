namespace VendasApi.Application.Veiculos;

public sealed class SnapshotVeiculoDuplicadoException(string campo)
    : Exception("Já existe um veículo com este dado na cópia local.")
{
    public string Campo { get; } = campo;
}
