using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Veiculos.Listagem;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Infrastructure.Persistence;

/// <summary>Paginação por preço e id, compartilhada pelas consultas de listagem.</summary>
internal static class PaginacaoVeiculosCopia
{
    public static async Task<ResultadoPaginado<TDto>> PaginarAsync<TDto>(
        IQueryable<VeiculoCopia> veiculos,
        int pagina,
        int tamanho,
        Expression<Func<VeiculoCopia, TDto>> projecao,
        CancellationToken cancellationToken)
    {
        var total = await veiculos.CountAsync(cancellationToken);

        // long evita estouro quando a página pedida é muito alta; além do total não há itens para buscar.
        var deslocamento = (long)(pagina - 1) * tamanho;
        if (deslocamento >= total)
            return new ResultadoPaginado<TDto>([], pagina, tamanho, total);

        var itens = await veiculos
            .OrderBy(v => v.Preco)
            .ThenBy(v => v.Id)
            .Skip((int)deslocamento)
            .Take(tamanho)
            .Select(projecao)
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<TDto>(itens, pagina, tamanho, total);
    }
}
