using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using VendasApi.Pagamentos;

namespace VendasApi.Tests.Pagamentos;

public class AssinaturaWebhookVerificadorTests
{
    private const string Segredo = "segredo-de-teste";
    private static readonly byte[] Corpo = Encoding.UTF8.GetBytes("""{"codigoPagamento":"x","resultado":"aprovado"}""");

    private static readonly AssinaturaWebhookVerificador Verificador = new(
        Options.Create(new PagamentosOptions { SegredoWebhook = Segredo }));

    [Fact]
    public void Valida_AssinaturaCorreta_EhAceita()
    {
        Assert.True(Verificador.Valida(Assinar(Corpo), Corpo));
    }

    [Fact]
    public void Valida_AssinaturaDeOutroCorpo_EhRecusada()
    {
        var outro = Encoding.UTF8.GetBytes("""{"codigoPagamento":"y","resultado":"aprovado"}""");

        Assert.False(Verificador.Valida(Assinar(outro), Corpo));
    }

    [Fact]
    public void Valida_AssinaturaAusenteOuVazia_EhRecusada()
    {
        Assert.False(Verificador.Valida(null, Corpo));
        Assert.False(Verificador.Valida("   ", Corpo));
    }

    [Fact]
    public void Valida_AssinaturaEmMaiusculas_EhAceitaComoEquivalente()
    {
        Assert.True(Verificador.Valida(Assinar(Corpo).ToUpperInvariant(), Corpo));
    }

    private static string Assinar(byte[] corpo) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Segredo), corpo)).ToLowerInvariant();
}
