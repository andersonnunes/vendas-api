using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace VendasApi.Tests.Seguranca;

[Collection(KeycloakCollection.NomeColecao)]
public sealed class SegurancaHttpTests(KeycloakFixture keycloak)
{
    [Fact]
    public async Task SemToken_RetornaNaoAutorizado()
    {
        using var fabrica = new ApiComKeycloak(keycloak);
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/testes/operacao");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task TokenDeVendedor_AcessaOperacaoDoCatalogo()
    {
        using var fabrica = new ApiComKeycloak(keycloak);
        var token = await keycloak.ObterTokenUsuarioAsync("vendedor-dev", "dev-vendedor");
        var cliente = ClienteComToken(fabrica, token);

        var resposta = await cliente.GetAsync("/testes/operacao");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task TokenDeVendedor_NaoAcessaEndpointDeServico()
    {
        using var fabrica = new ApiComKeycloak(keycloak);
        var token = await keycloak.ObterTokenUsuarioAsync("vendedor-dev", "dev-vendedor");
        var cliente = ClienteComToken(fabrica, token);

        var resposta = await cliente.GetAsync("/testes/interno");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task TokenDeOutroEmissor_RetornaNaoAutorizado()
    {
        using var fabrica = new ApiComKeycloak(keycloak);
        using var rsa = RSA.Create(2048);
        var credenciais = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            issuer: keycloak.EmissorRealm,
            audience: "vendas-api",
            claims: [new Claim("realm_access", "{\"roles\":[\"administrador\"]}")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credenciais);
        var forjado = new JwtSecurityTokenHandler().WriteToken(token);
        var cliente = ClienteComToken(fabrica, forjado);

        var resposta = await cliente.GetAsync("/testes/operacao");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task ChamadaDeServicoVeiculosApi_AcessaEndpointInterno()
    {
        using var fabrica = new ApiComKeycloak(keycloak, "veiculos-api", "dev-veiculos-api-secret");
        var cliente = ClienteDeSaida(fabrica);

        var resposta = await cliente.GetAsync("/testes/interno");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task ChamadaDeServicoVendasApi_NaoAcessaEndpointInterno()
    {
        using var fabrica = new ApiComKeycloak(keycloak, "vendas-api", "dev-vendas-api-secret");
        var cliente = ClienteDeSaida(fabrica);

        var resposta = await cliente.GetAsync("/testes/interno");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    private static HttpClient ClienteComToken(ApiComKeycloak fabrica, string token)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    private static HttpClient ClienteDeSaida(ApiComKeycloak fabrica)
    {
        var cliente = fabrica.Services.GetRequiredService<IHttpClientFactory>().CreateClient("teste-saida");
        cliente.BaseAddress = new Uri("http://localhost");
        return cliente;
    }
}
