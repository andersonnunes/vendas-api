using Microsoft.Extensions.Options;
using VendasApi.Infrastructure.Integracao;
using VendasApi.Seguranca;

namespace VendasApi.Integracao;

public static class IntegracaoVeiculosExtensions
{
    /// <summary>
    /// Cliente para veiculos-api: token de serviço do próprio vendas-api e retry padrão (só PUT é repetido,
    /// por ser idempotente), com 3 segundos por tentativa.
    /// </summary>
    public static IServiceCollection AddIntegracaoVeiculosApi(this IServiceCollection services, IConfiguration configuracao)
    {
        services.AddOptions<VeiculosApiOptions>()
            .Bind(configuracao.GetSection(VeiculosApiOptions.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient(VeiculosApiStatusNotificador.NomeCliente, (provedor, cliente) =>
            {
                cliente.BaseAddress = new Uri(provedor.GetRequiredService<IOptions<VeiculosApiOptions>>().Value.BaseUrl);
            })
            .AddHttpMessageHandler<ServicoAutenticadoHandler>()
            .AddStandardResilienceHandler(opcoes => opcoes.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3));

        return services;
    }
}
