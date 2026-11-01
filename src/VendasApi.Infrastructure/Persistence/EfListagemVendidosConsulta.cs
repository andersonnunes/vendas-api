using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Veiculos.Listagem;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Infrastructure.Persistence;

internal sealed class EfListagemVendidosConsulta(VendasDbContext contexto) : IListagemVendidosConsulta
{
    public Task<ResultadoPaginado<VeiculoVendidoDto>> ListarVendidosAsync(int pagina, int tamanho, CancellationToken cancellationToken) =>
        PaginacaoVeiculosCopia.PaginarAsync(
            contexto.VeiculosCopia.AsNoTracking().Where(VeiculoCopia.EstaVendido),
            pagina,
            tamanho,
            v => new VeiculoVendidoDto(v.Id, v.Marca, v.Modelo, v.Ano, v.Cor, v.Preco, v.Chassi, v.Placa),
            cancellationToken);
}
