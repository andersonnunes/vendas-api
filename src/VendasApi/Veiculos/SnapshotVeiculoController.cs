using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendasApi.Application.Veiculos;
using VendasApi.Domain.Veiculos;
using VendasApi.Seguranca;

namespace VendasApi.Veiculos;

/// <summary>Endpoint interno: só veiculos-api (papel servico, azp = veiculos-api) pode chamar.</summary>
[ApiController]
[Route("internal/veiculos/{id:guid}")]
[Authorize(Policy = PoliticasAutorizacao.ServicoVeiculosApi)]
public sealed class SnapshotVeiculoController(AplicarSnapshotVeiculoCasoDeUso aplicarSnapshot) : ControllerBase
{
    private static readonly Dictionary<string, StatusVeiculoCopia> StatusAceitos = new()
    {
        ["disponivel"] = StatusVeiculoCopia.Disponivel,
        ["vendido"] = StatusVeiculoCopia.Vendido,
    };

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Aplicar(Guid id, [FromBody] SnapshotVeiculoRequest request, CancellationToken cancellationToken)
    {
        if (request.Id != id)
        {
            ModelState.AddModelError(nameof(request.Id), "O id do corpo deve ser igual ao da rota.");
            return ValidationProblem(ModelState);
        }
        if (!StatusAceitos.TryGetValue(request.Status, out var status))
        {
            ModelState.AddModelError(nameof(request.Status), "Status deve ser 'disponivel' ou 'vendido'.");
            return ValidationProblem(ModelState);
        }

        var dados = new DadosSnapshot(
            request.Id,
            request.Marca,
            request.Modelo,
            request.Ano,
            request.Cor,
            request.Preco,
            request.Chassi,
            request.Placa,
            status,
            request.AtualizadoEm);

        await aplicarSnapshot.ExecutarAsync(dados, cancellationToken);
        return Ok();
    }
}
