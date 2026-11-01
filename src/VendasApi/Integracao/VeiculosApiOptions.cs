using System.ComponentModel.DataAnnotations;

namespace VendasApi.Integracao;

public sealed class VeiculosApiOptions
{
    public const string Secao = "VeiculosApi";

    [Required]
    public string BaseUrl { get; set; } = string.Empty;
}
