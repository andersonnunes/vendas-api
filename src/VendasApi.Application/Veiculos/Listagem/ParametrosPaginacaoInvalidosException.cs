namespace VendasApi.Application.Veiculos.Listagem;

public sealed class ParametrosPaginacaoInvalidosException(string mensagem) : Exception(mensagem);
