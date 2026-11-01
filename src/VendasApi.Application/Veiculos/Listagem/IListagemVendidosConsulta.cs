namespace VendasApi.Application.Veiculos.Listagem;

/// <summary>Porta de leitura da listagem administrativa de vendidos. Separada da listagem pública.</summary>
public interface IListagemVendidosConsulta
{
    /// <summary>Veículos vendidos, por preço crescente e, em empate, por id crescente.</summary>
    Task<ResultadoPaginado<VeiculoVendidoDto>> ListarVendidosAsync(int pagina, int tamanho, CancellationToken cancellationToken);
}
