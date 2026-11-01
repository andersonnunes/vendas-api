using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VendasApi.Application.Veiculos;
using VendasApi.Application.Pagamentos;
using VendasApi.Application.Veiculos.Listagem;
using VendasApi.Application.Vendas;
using VendasApi.Infrastructure.Integracao;
using VendasApi.Infrastructure.Persistence;

namespace VendasApi.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuracao)
    {
        services.AddDbContext<VendasDbContext>(opcoes =>
            opcoes.UseNpgsql(configuracao.GetConnectionString("VendasDb")));

        services.AddScoped<IVeiculoCopiaRepositorio, EfVeiculoCopiaRepositorio>();
        services.AddScoped<IListagemVeiculosConsulta, EfListagemVeiculosConsulta>();
        services.AddScoped<IListagemVendidosConsulta, EfListagemVendidosConsulta>();
        services.AddScoped<IVendasRepositorio, EfVendasRepositorio>();
        services.AddScoped<IProcessamentoPagamentoRepositorio, EfVendasRepositorio>();
        services.AddScoped<IVeiculosStatusNotificador, VeiculosApiStatusNotificador>();

        return services;
    }
}
