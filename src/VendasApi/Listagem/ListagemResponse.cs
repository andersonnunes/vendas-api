using VendasApi.Application.Veiculos.Listagem;

namespace VendasApi.Listagem;

/// <summary>Envelope paginado comum às listagens da API.</summary>
public sealed record ListagemResponse<T>(IReadOnlyList<T> Itens, int Pagina, int Tamanho, int Total)
{
    public static ListagemResponse<TSaida> De<TEntrada, TSaida>(
        ResultadoPaginado<TEntrada> resultado,
        Func<TEntrada, TSaida> mapear) =>
        new(resultado.Itens.Select(mapear).ToList(), resultado.Pagina, resultado.Tamanho, resultado.Total);
}
