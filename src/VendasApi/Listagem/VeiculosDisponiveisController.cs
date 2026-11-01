using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendasApi.Application.Veiculos.Listagem;

namespace VendasApi.Listagem;

/// <summary>Listagem pública de veículos à venda. Anônima: o visitante não precisa de token.</summary>
[ApiController]
[Route("veiculos")]
[AllowAnonymous]
public sealed class VeiculosDisponiveisController(ListarVeiculosDisponiveisCasoDeUso listar) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ListagemVeiculosResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = ValidacaoPaginacao.TamanhoPadrao,
        CancellationToken cancellationToken = default)
    {
        var resultado = await listar.ExecutarAsync(pagina, tamanho, cancellationToken);

        return Ok(new ListagemVeiculosResponse(
            resultado.Itens.Select(VeiculoListagemResponse.De).ToList(),
            resultado.Pagina,
            resultado.Tamanho,
            resultado.Total));
    }
}

public sealed record ListagemVeiculosResponse(
    IReadOnlyList<VeiculoListagemResponse> Itens,
    int Pagina,
    int Tamanho,
    int Total);

public sealed record VeiculoListagemResponse(Guid Id, string Marca, string Modelo, int Ano, string Cor, decimal Preco)
{
    public static VeiculoListagemResponse De(VeiculoListagemDto veiculo) =>
        new(veiculo.Id, veiculo.Marca, veiculo.Modelo, veiculo.Ano, veiculo.Cor, veiculo.Preco);
}
