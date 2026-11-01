namespace VendasApi.Application.Pagamentos;

/// <summary>Avisa veiculos-api que o veículo foi vendido. Falhas definitivas viram <see cref="NotificacaoVeiculosFalhouException"/>.</summary>
public interface IVeiculosStatusNotificador
{
    Task NotificarVendidoAsync(Guid veiculoId, DateTimeOffset ocorridoEm, CancellationToken cancellationToken);
}
