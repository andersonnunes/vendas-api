namespace VendasApi.Veiculos;

/// <summary>
/// Snapshot enviado por veiculos-api (contrato da US1.3). Campos `required` fazem a ausência de
/// qualquer campo resultar em `400` antes de chegar ao domínio.
/// </summary>
public sealed class SnapshotVeiculoRequest
{
    public required Guid Id { get; init; }
    public required string Marca { get; init; }
    public required string Modelo { get; init; }
    public required int Ano { get; init; }
    public required string Cor { get; init; }
    public required decimal Preco { get; init; }
    public required string Chassi { get; init; }
    public string? Placa { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset AtualizadoEm { get; init; }
}
