namespace VendasApi.Application.Veiculos.Listagem;

public sealed record ResultadoPaginado<T>(IReadOnlyList<T> Itens, int Pagina, int Tamanho, int Total);
