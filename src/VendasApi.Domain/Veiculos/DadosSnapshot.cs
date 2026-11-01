namespace VendasApi.Domain.Veiculos;

/// <summary>Estado completo do veículo enviado por veiculos-api (contrato da US1.3).</summary>
public sealed record DadosSnapshot(
    Guid Id,
    string Marca,
    string Modelo,
    int Ano,
    string Cor,
    decimal Preco,
    string Chassi,
    string? Placa,
    StatusVeiculoCopia Status,
    DateTimeOffset AtualizadoEm);
