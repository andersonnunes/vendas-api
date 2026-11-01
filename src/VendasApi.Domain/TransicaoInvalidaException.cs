namespace VendasApi.Domain;

/// <summary>Transição de estado não permitida pelo fluxo da venda ou do veículo.</summary>
public sealed class TransicaoInvalidaException(string mensagem) : Exception(mensagem);
