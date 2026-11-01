using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Veiculos.Listagem;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Infrastructure.Persistence;

internal sealed class EfListagemVeiculosConsulta(VendasDbContext contexto) : IListagemVeiculosConsulta
{
    public async Task<ResultadoPaginado<VeiculoListagemDto>> ListarDisponiveisAsync(int pagina, int tamanho, CancellationToken cancellationToken)
    {
        var disponiveis = contexto.VeiculosCopia
            .AsNoTracking()
            .Where(v => v.Status == StatusVeiculoCopia.Disponivel);

        var total = await disponiveis.CountAsync(cancellationToken);

        // long evita estouro quando a página pedida é muito alta; além do total não há itens para buscar.
        var deslocamento = (long)(pagina - 1) * tamanho;
        if (deslocamento >= total)
            return new ResultadoPaginado<VeiculoListagemDto>([], pagina, tamanho, total);

        var itens = await disponiveis
            .OrderBy(v => v.Preco)
            .ThenBy(v => v.Id)
            .Skip((int)deslocamento)
            .Take(tamanho)
            .Select(v => new VeiculoListagemDto(v.Id, v.Marca, v.Modelo, v.Ano, v.Cor, v.Preco))
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<VeiculoListagemDto>(itens, pagina, tamanho, total);
    }
}
