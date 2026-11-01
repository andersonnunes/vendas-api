namespace VendasApi.Application.Pagamentos;

public sealed class NotificacaoVeiculosFalhouException(Guid veiculoId, Exception? causa = null)
    : Exception("Não foi possível notificar veiculos-api sobre a venda.", causa)
{
    public Guid VeiculoId { get; } = veiculoId;
}
