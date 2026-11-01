namespace VendasApi.Domain.Vendas;

public sealed class VendaInvalidaException(IReadOnlyDictionary<string, string[]> erros)
    : Exception("Dados da venda inválidos.")
{
    public IReadOnlyDictionary<string, string[]> Erros { get; } = erros;
}
