namespace VendasApi.Application.Veiculos.Listagem;

/// <summary>Dados de um veículo à venda expostos ao público. Chassi e placa ficam de fora de propósito.</summary>
public sealed record VeiculoListagemDto(Guid Id, string Marca, string Modelo, int Ano, string Cor, decimal Preco);
