using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace VendasApi.Pagamentos;

/// <summary>Confere a assinatura HMAC-SHA256 do corpo bruto do webhook de pagamento.</summary>
public sealed class AssinaturaWebhookVerificador(IOptions<PagamentosOptions> opcoes)
{
    public bool Valida(string? assinaturaRecebida, byte[] corpo)
    {
        if (string.IsNullOrWhiteSpace(assinaturaRecebida))
            return false;

        var chave = Encoding.UTF8.GetBytes(opcoes.Value.SegredoWebhook);
        var esperada = Convert.ToHexString(HMACSHA256.HashData(chave, corpo)).ToLowerInvariant();

        // Comparação em tempo constante: não vaza, pelo tempo de resposta, quantos caracteres batem.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(esperada),
            Encoding.UTF8.GetBytes(assinaturaRecebida.Trim().ToLowerInvariant()));
    }
}
