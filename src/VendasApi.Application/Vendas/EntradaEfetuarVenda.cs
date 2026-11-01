namespace VendasApi.Application.Vendas;

public sealed record EntradaEfetuarVenda(Guid VeiculoId, string Cpf, DateOnly DataVenda);
