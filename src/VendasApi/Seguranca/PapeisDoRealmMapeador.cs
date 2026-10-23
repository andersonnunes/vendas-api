using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace VendasApi.Seguranca;

internal static class PapeisDoRealmMapeador
{
    public static Task AdicionarPapeis(TokenValidatedContext contexto)
    {
        var identidade = contexto.Principal?.Identities.FirstOrDefault();
        var acessoRealm = contexto.Principal?.FindFirst("realm_access")?.Value;
        if (identidade is null || acessoRealm is null)
            return Task.CompletedTask;

        using var documento = JsonDocument.Parse(acessoRealm);
        if (!documento.RootElement.TryGetProperty("roles", out var papeis))
            return Task.CompletedTask;

        foreach (var papel in papeis.EnumerateArray())
            identidade.AddClaim(new Claim(ClaimTypes.Role, papel.GetString()!));

        return Task.CompletedTask;
    }
}
