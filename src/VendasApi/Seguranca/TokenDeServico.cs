using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace VendasApi.Seguranca;

public sealed class TokenDeServico
{
    private const int MargemExpiracaoSegundos = 30;

    private readonly IHttpClientFactory _fabricaClientes;
    private readonly IOptions<KeycloakOptions> _keycloak;
    private readonly TimeProvider _relogio;
    private readonly SemaphoreSlim _trava;
    private string? _token;
    private DateTimeOffset _expiraEm;

    public TokenDeServico(IHttpClientFactory fabricaClientes, IOptions<KeycloakOptions> keycloak, TimeProvider relogio)
    {
        _fabricaClientes = fabricaClientes;
        _keycloak = keycloak;
        _relogio = relogio;
        _trava = new SemaphoreSlim(1, 1);
    }

    public async Task<string> ObterAsync(CancellationToken cancellationToken)
    {
        await _trava.WaitAsync(cancellationToken);
        try
        {
            if (_token is not null && _relogio.GetUtcNow() < _expiraEm)
                return _token;

            var opcoes = _keycloak.Value;
            using var cliente = _fabricaClientes.CreateClient("keycloak-token");
            using var resposta = await cliente.PostAsync(
                opcoes.EndpointToken,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = opcoes.ClientId,
                    ["client_secret"] = opcoes.ClientSecret
                }),
                cancellationToken);

            resposta.EnsureSuccessStatusCode();

            var dados = await resposta.Content.ReadFromJsonAsync<RespostaToken>(cancellationToken)
                ?? throw new InvalidOperationException("Resposta de token vazia do Keycloak.");

            _token = dados.AccessToken;
            _expiraEm = _relogio.GetUtcNow().AddSeconds(dados.ExpiresIn - MargemExpiracaoSegundos);
            return _token;
        }
        finally
        {
            _trava.Release();
        }
    }

    private sealed record RespostaToken(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
