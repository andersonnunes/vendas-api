using VendasApi.Domain.Vendas;

namespace VendasApi.Application.Vendas;

/// <summary>Venda como exposta pela API. O CPF sai mascarado: o valor completo fica só no banco.</summary>
public sealed record VendaDto(
    Guid Id,
    Guid VeiculoId,
    string CpfMascarado,
    DateOnly DataVenda,
    string Status,
    string CodigoPagamento,
    DateTimeOffset CriadoEm)
{
    public static VendaDto De(Venda venda) => new(
        venda.Id,
        venda.VeiculoId,
        CpfValidacao.Mascarar(venda.Cpf),
        venda.DataVenda,
        venda.Status.ToString().ToLowerInvariant(),
        venda.CodigoPagamento,
        venda.CriadoEm);
}
