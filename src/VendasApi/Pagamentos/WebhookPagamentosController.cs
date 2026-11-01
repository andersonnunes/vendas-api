using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendasApi.Application.Pagamentos;
using VendasApi.Domain.Vendas;

namespace VendasApi.Pagamentos;

/// <summary>
/// Webhook do processador de pagamento. Anônimo para o Keycloak: quem chama é um sistema externo, e a
/// autenticidade vem da assinatura HMAC-SHA256 do corpo bruto, no cabeçalho X-Assinatura.
/// </summary>
[ApiController]
[Route("webhooks/pagamentos")]
[AllowAnonymous]
public sealed class WebhookPagamentosController(
    ProcessarResultadoPagamentoCasoDeUso processar,
    AssinaturaWebhookVerificador verificador) : ControllerBase
{
    private const string CabecalhoAssinatura = "X-Assinatura";
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Receber(CancellationToken cancellationToken)
    {
        // A assinatura é conferida sobre os bytes exatos recebidos, antes de qualquer desserialização.
        using var memoria = new MemoryStream();
        await Request.Body.CopyToAsync(memoria, cancellationToken);
        var corpo = memoria.ToArray();

        if (!verificador.Valida(Request.Headers[CabecalhoAssinatura], corpo))
            return Unauthorized();

        WebhookPagamentoRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<WebhookPagamentoRequest>(corpo, OpcoesJson);
        }
        catch (JsonException)
        {
            ModelState.AddModelError("corpo", "Corpo do webhook inválido.");
            return ValidationProblem(ModelState);
        }

        if (request is null || string.IsNullOrWhiteSpace(request.CodigoPagamento))
        {
            ModelState.AddModelError("codigoPagamento", "Informe o código do pagamento.");
            return ValidationProblem(ModelState);
        }
        if (!ResultadosAceitos.TryGetValue(request.Resultado ?? string.Empty, out var resultado))
        {
            ModelState.AddModelError("resultado", "Resultado deve ser 'aprovado' ou 'cancelado'.");
            return ValidationProblem(ModelState);
        }

        var alterou = await processar.ExecutarAsync(request.CodigoPagamento, resultado, cancellationToken);
        return Ok(new { alterou });
    }

    private static readonly Dictionary<string, ResultadoPagamento> ResultadosAceitos = new()
    {
        ["aprovado"] = ResultadoPagamento.Aprovado,
        ["cancelado"] = ResultadoPagamento.Cancelado,
    };
}

public sealed class WebhookPagamentoRequest
{
    public string? CodigoPagamento { get; init; }
    public string? Resultado { get; init; }
}
