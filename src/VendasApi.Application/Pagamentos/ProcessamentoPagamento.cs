namespace VendasApi.Application.Pagamentos;

/// <summary>
/// Resultado da transição feita dentro da unidade de trabalho. `Alterou` é false quando o resultado
/// repete o estado atual (idempotente). `Concluiu` indica que o veículo foi vendido e precisa ser notificado.
/// </summary>
public sealed record ProcessamentoPagamento(bool Alterou, bool Concluiu, Guid VeiculoId);
