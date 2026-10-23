using System.ComponentModel.DataAnnotations;

namespace VendasApi.Seguranca;

public sealed class KeycloakOptions
{
    public const string Secao = "Keycloak";

    [Required] public string EmissorPublico { get; init; } = string.Empty;
    [Required] public string MetadataInterna { get; init; } = string.Empty;
    [Required] public string EndpointToken { get; init; } = string.Empty;
    [Required] public string Audiencia { get; init; } = string.Empty;
    [Required] public string ClientId { get; init; } = string.Empty;
    [Required] public string ClientSecret { get; init; } = string.Empty;
    [Required] public string ClienteChamadorPermitido { get; init; } = string.Empty;
}
