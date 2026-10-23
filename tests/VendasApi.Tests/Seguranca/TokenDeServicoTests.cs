using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using VendasApi.Seguranca;

namespace VendasApi.Tests.Seguranca;

[Collection(KeycloakCollection.NomeColecao)]
public sealed class TokenDeServicoTests(KeycloakFixture keycloak)
{
    [Fact]
    public async Task ObterAsync_ReutilizaTokenEnquantoValidoERenovaAposExpiracao()
    {
        var relogio = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var servicos = new ServiceCollection();
        servicos.AddHttpClient("keycloak-token");
        using var provedor = servicos.BuildServiceProvider();
        var opcoes = Options.Create(new KeycloakOptions
        {
            EmissorPublico = keycloak.EmissorRealm,
            MetadataInterna = keycloak.MetadataRealm,
            EndpointToken = keycloak.EndpointToken,
            Audiencia = "vendas-api",
            ClientId = "vendas-api",
            ClientSecret = "dev-vendas-api-secret"
        });
        var tokens = new TokenDeServico(provedor.GetRequiredService<IHttpClientFactory>(), opcoes, relogio);

        var primeiro = await tokens.ObterAsync(CancellationToken.None);
        var segundo = await tokens.ObterAsync(CancellationToken.None);
        relogio.Advance(TimeSpan.FromHours(1));
        var terceiro = await tokens.ObterAsync(CancellationToken.None);

        Assert.Equal(primeiro, segundo);
        Assert.NotEqual(primeiro, terceiro);
    }
}
