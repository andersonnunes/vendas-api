using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using VendasApi.Seguranca;
using VendasApi.Tests.Infraestrutura;

namespace VendasApi.Tests.Seguranca;

public sealed class ApiComKeycloak(KeycloakFixture keycloak, PostgresFixture postgres, string clientId, string clientSecret)
    : WebApplicationFactory<Program>
{
    public ApiComKeycloak(KeycloakFixture keycloak, PostgresFixture postgres) : this(keycloak, postgres, "vendas-api", "dev-vendas-api-secret")
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:VendasDb", postgres.ConnectionString);
        // Segredo de teste do webhook e veiculos-api inalcançável: a notificação falha de forma determinística.
        builder.UseSetting("Pagamentos:SegredoWebhook", "segredo-de-teste");
        builder.UseSetting("VeiculosApi:BaseUrl", "http://localhost:9");
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
