using System.ComponentModel.DataAnnotations;

namespace VendasApi.Pagamentos;

public sealed class PagamentosOptions
{
    public const string Secao = "Pagamentos";

    /// <summary>Segredo compartilhado com o processador de pagamento para validar a assinatura do webhook.</summary>
    [Required]
    public string SegredoWebhook { get; set; } = string.Empty;
}
