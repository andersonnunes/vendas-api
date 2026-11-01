using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VendasApi.Application.Veiculos;
using VendasApi.Infrastructure.Persistence;

namespace VendasApi.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuracao)
    {
        services.AddDbContext<VendasDbContext>(opcoes =>
            opcoes.UseNpgsql(configuracao.GetConnectionString("VendasDb")));

        services.AddScoped<IVeiculoCopiaRepositorio, EfVeiculoCopiaRepositorio>();

        return services;
    }
}
