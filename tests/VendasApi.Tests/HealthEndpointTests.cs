using System.Net;
using VendasApi.Tests.Infraestrutura;
using VendasApi.Tests.Seguranca;

namespace VendasApi.Tests;

[Collection(AmbienteCollection.NomeColecao)]
public class HealthEndpointTests(KeycloakFixture keycloak, PostgresFixture postgres)
{
    [Fact]
    public async Task Health_RetornaOk()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var client = fabrica.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
