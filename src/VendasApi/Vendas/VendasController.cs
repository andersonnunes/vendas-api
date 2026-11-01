using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendasApi.Application.Vendas;
using VendasApi.Seguranca;

namespace VendasApi.Vendas;

/// <summary>Registro de venda: administrador e vendedor reservam o veículo para o comprador.</summary>
[ApiController]
[Route("vendas")]
[Authorize(Policy = PoliticasAutorizacao.OperacaoComercial)]
public sealed class VendasController(EfetuarVendaCasoDeUso efetuarVenda) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<VendaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Efetuar([FromBody] EfetuarVendaRequest request, CancellationToken cancellationToken)
    {
        var dto = await efetuarVenda.ExecutarAsync(
            new EntradaEfetuarVenda(request.VeiculoId, request.Cpf, request.DataVenda),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, VendaResponse.De(dto));
    }
}

public sealed record VendaResponse(
    Guid Id,
    Guid VeiculoId,
    string CpfMascarado,
    DateOnly DataVenda,
    string Status,
    string CodigoPagamento,
    DateTimeOffset CriadoEm)
{
    public static VendaResponse De(VendaDto venda) => new(
        venda.Id,
        venda.VeiculoId,
        venda.CpfMascarado,
        venda.DataVenda,
        venda.Status,
        venda.CodigoPagamento,
        venda.CriadoEm);
}
