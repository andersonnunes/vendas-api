namespace VendasApi.Application.Veiculos.Listagem;

public sealed class ListarVeiculosDisponiveisCasoDeUso(IListagemVeiculosConsulta consulta)
{
    public Task<ResultadoPaginado<VeiculoListagemDto>> ExecutarAsync(int pagina, int tamanho, CancellationToken cancellationToken)
    {
        ValidacaoPaginacao.Validar(pagina, tamanho);
        return consulta.ListarDisponiveisAsync(pagina, tamanho, cancellationToken);
    }
}
