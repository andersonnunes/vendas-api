namespace VendasApi.Application.Veiculos.Listagem;

/// <summary>Dados de um veículo vendido para o acompanhamento administrativo. Inclui chassi e placa.</summary>
public sealed record VeiculoVendidoDto(Guid Id, string Marca, string Modelo, int Ano, string Cor, decimal Preco, string Chassi, string? Placa);
