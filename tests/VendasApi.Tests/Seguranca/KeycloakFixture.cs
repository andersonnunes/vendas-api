using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace VendasApi.Tests.Seguranca;

public sealed class KeycloakFixture : IAsyncLifetime
{
    private const string ImagemKeycloak = "quay.io/keycloak/keycloak:26.8.1";
    private const int PortaInterna = 8080;

    private IContainer? _container;

    public string UrlBase { get; private set; } = string.Empty;
    public string EmissorRealm => $"{UrlBase}/realms/revendax";
    public string EndpointToken => $"{EmissorRealm}/protocol/openid-connect/token";
    public string MetadataRealm => $"{EmissorRealm}/.well-known/openid-configuration";

    public async Task InitializeAsync()
    {
        var realm = Path.Combine(AppContext.BaseDirectory, "keycloak", "realm-revendax.json");

        _container = new ContainerBuilder(ImagemKeycloak)
            .WithPortBinding(PortaInterna, true)
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
            .WithEnvironment("KC_HOSTNAME_STRICT", "false")
            .WithBindMount(realm, "/opt/keycloak/data/import/realm-revendax.json", AccessMode.ReadOnly)
            .WithCommand("start-dev", "--import-realm")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(requisicao =>
                requisicao.ForPort(PortaInterna).ForPath("/realms/revendax")))
            .Build();

        await _container.StartAsync();
        UrlBase = $"http://localhost:{_container.GetMappedPublicPort(PortaInterna)}";
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    public Task<string> ObterTokenUsuarioAsync(string usuario, string senha) =>
        PedirTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "revendax-dev-cli",
            ["username"] = usuario,
            ["password"] = senha
        });

    public Task<string> ObterTokenServicoAsync(string clientId, string clientSecret) =>
        PedirTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });

    private async Task<string> PedirTokenAsync(Dictionary<string, string> campos)
    {
        using var cliente = new HttpClient();
        using var resposta = await cliente.PostAsync(EndpointToken, new FormUrlEncodedContent(campos));
        resposta.EnsureSuccessStatusCode();
        var dados = await resposta.Content.ReadFromJsonAsync<RespostaToken>();
        return dados!.AccessToken;
    }

    private sealed record RespostaToken([property: JsonPropertyName("access_token")] string AccessToken);
}

[CollectionDefinition(NomeColecao)]
public sealed class KeycloakCollection : ICollectionFixture<KeycloakFixture>
{
    public const string NomeColecao = "keycloak";
}
