namespace VendasApi.Vendas;

/// <summary>Corpo de POST /vendas. Campos `required` fazem a ausência de qualquer um resultar em 400.</summary>
public sealed class EfetuarVendaRequest
{
    public required Guid VeiculoId { get; init; }
    public required string Cpf { get; init; }
    public required DateOnly DataVenda { get; init; }
}
