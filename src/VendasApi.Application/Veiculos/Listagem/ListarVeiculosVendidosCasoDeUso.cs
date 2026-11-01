namespace VendasApi.Application.Veiculos.Listagem;

public sealed class ListarVeiculosVendidosCasoDeUso(IListagemVendidosConsulta consulta)
{
    public Task<ResultadoPaginado<VeiculoVendidoDto>> ExecutarAsync(int pagina, int tamanho, CancellationToken cancellationToken)
    {
        ValidacaoPaginacao.Validar(pagina, tamanho);
        return consulta.ListarVendidosAsync(pagina, tamanho, cancellationToken);
    }
}
