using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Veiculos.Listagem;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Infrastructure.Persistence;

internal sealed class EfListagemVeiculosConsulta(VendasDbContext contexto) : IListagemVeiculosConsulta
{
    public Task<ResultadoPaginado<VeiculoListagemDto>> ListarDisponiveisAsync(int pagina, int tamanho, CancellationToken cancellationToken) =>
        PaginacaoVeiculosCopia.PaginarAsync(
            contexto.VeiculosCopia.AsNoTracking().Where(v => v.Status == StatusVeiculoCopia.Disponivel),
            pagina,
            tamanho,
            v => new VeiculoListagemDto(v.Id, v.Marca, v.Modelo, v.Ano, v.Cor, v.Preco),
            cancellationToken);
}
