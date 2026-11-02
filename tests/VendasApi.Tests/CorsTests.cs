using System.Net;
using VendasApi.Tests.Infraestrutura;
using VendasApi.Tests.Seguranca;

namespace VendasApi.Tests;

[Collection(AmbienteCollection.NomeColecao)]
public class CorsTests(KeycloakFixture keycloak, PostgresFixture postgres)
{
    [Fact]
    public async Task Origem_configurada_recebe_cabecalho_de_CORS()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var client = fabrica.CreateClient();
        var requisicao = new HttpRequestMessage(HttpMethod.Get, "/health");
        requisicao.Headers.Add("Origin", "http://localhost:4200");

        var response = await client.SendAsync(requisicao);

        Assert.Equal("http://localhost:4200", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Origem_nao_configurada_nao_recebe_cabecalho_de_CORS()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var client = fabrica.CreateClient();
        var requisicao = new HttpRequestMessage(HttpMethod.Get, "/health");
        requisicao.Headers.Add("Origin", "https://site-estranho.example");

        var response = await client.SendAsync(requisicao);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Preflight_de_GET_da_listagem_aceita_a_origem_configurada()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var client = fabrica.CreateClient();
        var requisicao = new HttpRequestMessage(HttpMethod.Options, "/veiculos");
        requisicao.Headers.Add("Origin", "http://localhost:4200");
        requisicao.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(requisicao);

        Assert.Equal("http://localhost:4200", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("GET", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
    }
}
