using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendasApi.Application.Veiculos.Listagem;
using VendasApi.Seguranca;

namespace VendasApi.Listagem;

/// <summary>Listagem administrativa de veículos vendidos. Restrita ao papel administrador.</summary>
[ApiController]
[Route("veiculos/vendidos")]
[Authorize(Policy = PoliticasAutorizacao.Administrador)]
public sealed class VeiculosVendidosController(ListarVeiculosVendidosCasoDeUso listar) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ListagemResponse<VeiculoVendidoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = ValidacaoPaginacao.TamanhoPadrao,
        CancellationToken cancellationToken = default)
    {
        var resultado = await listar.ExecutarAsync(pagina, tamanho, cancellationToken);
        return Ok(ListagemResponse<VeiculoVendidoResponse>.De(resultado, VeiculoVendidoResponse.De));
    }
}

public sealed record VeiculoVendidoResponse(
    Guid Id,
    string Marca,
    string Modelo,
    int Ano,
    string Cor,
    decimal Preco,
    string Chassi,
    string? Placa)
{
    public static VeiculoVendidoResponse De(VeiculoVendidoDto veiculo) =>
        new(veiculo.Id, veiculo.Marca, veiculo.Modelo, veiculo.Ano, veiculo.Cor, veiculo.Preco, veiculo.Chassi, veiculo.Placa);
}
