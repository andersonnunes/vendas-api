using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace VendasApi.Seguranca;

internal sealed class ConfigurarJwtBearer(IOptions<KeycloakOptions> keycloak) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions opcoes)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
            return;

        var kc = keycloak.Value;
        opcoes.MetadataAddress = kc.MetadataInterna;
        opcoes.RequireHttpsMetadata = kc.EmissorPublico.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        opcoes.MapInboundClaims = false;
        opcoes.TokenValidationParameters.ValidIssuer = kc.EmissorPublico;
        opcoes.TokenValidationParameters.ValidAudience = kc.Audiencia;
        opcoes.Events.OnTokenValidated = PapeisDoRealmMapeador.AdicionarPapeis;
    }

    public void Configure(JwtBearerOptions opcoes) => Configure(JwtBearerDefaults.AuthenticationScheme, opcoes);
}
