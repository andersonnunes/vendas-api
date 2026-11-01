namespace VendasApi.Domain.Veiculos;

/// <summary>
/// Status do veículo na cópia local. `Reservado` existe só aqui (US2.4): veiculos-api não o conhece.
/// </summary>
public enum StatusVeiculoCopia
{
    Disponivel,
    Reservado,
    Vendido
}
