namespace VendasApi.Application.Veiculos.Listagem;

public sealed class ListarVeiculosDisponiveisCasoDeUso(IListagemVeiculosConsulta consulta)
{
    public const int TamanhoPadrao = 20;
    public const int TamanhoMaximo = 100;

    public Task<ResultadoPaginado<VeiculoListagemDto>> ExecutarAsync(int pagina, int tamanho, CancellationToken cancellationToken)
    {
        if (pagina < 1)
            throw new ParametrosPaginacaoInvalidosException("A página deve ser maior ou igual a 1.");
        if (tamanho < 1 || tamanho > TamanhoMaximo)
            throw new ParametrosPaginacaoInvalidosException($"O tamanho deve estar entre 1 e {TamanhoMaximo}.");

        return consulta.ListarDisponiveisAsync(pagina, tamanho, cancellationToken);
    }
}
