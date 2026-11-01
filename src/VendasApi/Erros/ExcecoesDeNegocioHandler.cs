using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using VendasApi.Application.Veiculos;
using VendasApi.Application.Veiculos.Listagem;
using VendasApi.Application.Pagamentos;
using VendasApi.Application.Vendas;
using VendasApi.Domain;
using VendasApi.Domain.Veiculos;
using VendasApi.Domain.Vendas;

namespace VendasApi.Erros;

internal sealed class ExcecoesDeNegocioHandler : IExceptionHandler
{
    private const string ChaveCodigo = "codigo";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        IResult? resultado = exception switch
        {
            SnapshotInvalidoException invalido => Results.ValidationProblem(
                invalido.Erros.ToDictionary(par => par.Key, par => par.Value)),
            SnapshotVeiculoDuplicadoException duplicado => Results.Problem(
                title: "Veículo já cadastrado",
                detail: $"Já existe um veículo com este {duplicado.Campo}.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?>
                {
                    [ChaveCodigo] = "veiculo_duplicado",
                    ["campo"] = duplicado.Campo,
                }),
            ParametrosPaginacaoInvalidosException paginacao => Results.ValidationProblem(
                new Dictionary<string, string[]> { ["paginacao"] = [paginacao.Message] }),
            VendaInvalidaException venda => Results.ValidationProblem(
                venda.Erros.ToDictionary(par => par.Key, par => par.Value)),
            VeiculoNaoEncontradoException => Results.Problem(
                title: "Veículo não encontrado",
                detail: "O veículo informado não existe na cópia local.",
                statusCode: StatusCodes.Status404NotFound),
            VeiculoIndisponivelParaVendaException => Results.Problem(
                title: "Veículo indisponível",
                detail: "Só um veículo disponível pode ser vendido.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { [ChaveCodigo] = "veiculo_indisponivel" }),
            TransicaoInvalidaException transicao => Results.Problem(
                title: "Transição não permitida",
                detail: transicao.Message,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { [ChaveCodigo] = "transicao_invalida" }),
            VendaPagamentoNaoEncontradaException => Results.Problem(
                title: "Venda não encontrada",
                detail: "Nenhuma venda corresponde ao código de pagamento informado.",
                statusCode: StatusCodes.Status404NotFound),
            ConcorrenciaCopiaVeiculoException => Results.Problem(
                title: "Conflito de sincronização",
                detail: "A cópia do veículo foi alterada por outra operação. Tente novamente.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { [ChaveCodigo] = "conflito_sincronizacao" }),
            _ => null
        };

        if (resultado is null)
            return false;

        await resultado.ExecuteAsync(httpContext);
        return true;
    }
}
