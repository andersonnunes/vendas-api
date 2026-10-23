using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendasApi.Seguranca;

namespace VendasApi.Tests.Seguranca;

[ApiController]
[Route("testes")]
public sealed class TestesSegurancaController : ControllerBase
{
    [HttpGet("operacao")]
    [Authorize(Policy = PoliticasAutorizacao.OperacaoComercial)]
    public IActionResult Operacao() => Ok();

    [HttpGet("interno")]
    [Authorize(Policy = PoliticasAutorizacao.ServicoVeiculosApi)]
    public IActionResult Interno() => Ok();
}
