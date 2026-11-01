namespace VendasApi.Application.Veiculos.Listagem;

/// <summary>Porta de leitura da listagem pública. Separada do repositório de escrita da cópia.</summary>
public interface IListagemVeiculosConsulta
{
    /// <summary>Veículos com status disponível, por preço crescente e, em empate, por id crescente.</summary>
    Task<ResultadoPaginado<VeiculoListagemDto>> ListarDisponiveisAsync(int pagina, int tamanho, CancellationToken cancellationToken);
}
