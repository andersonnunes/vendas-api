using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using VendasApi.Seguranca;

namespace VendasApi.Tests.Seguranca;

public sealed class ApiComKeycloak(KeycloakFixture keycloak, string clientId, string clientSecret)
    : WebApplicationFactory<Program>
{
    public ApiComKeycloak(KeycloakFixture keycloak) : this(keycloak, "vendas-api", "dev-vendas-api-secret")
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Keycloak:EmissorPublico", keycloak.EmissorRealm);
        builder.UseSetting("Keycloak:MetadataInterna", keycloak.MetadataRealm);
        builder.UseSetting("Keycloak:EndpointToken", keycloak.EndpointToken);
        builder.UseSetting("Keycloak:ClientId", clientId);
        builder.UseSetting("Keycloak:ClientSecret", clientSecret);

        builder.ConfigureTestServices(servicos =>
        {
            servicos.AddControllers().AddApplicationPart(typeof(TestesSegurancaController).Assembly);

            servicos.AddHttpClient("teste-saida")
                .AddHttpMessageHandler<ServicoAutenticadoHandler>()
                .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler());
        });
    }
}
