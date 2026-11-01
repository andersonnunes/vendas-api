using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using VendasApi.Application.Veiculos;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Erros;

internal sealed class ExcecoesDeNegocioHandler : IExceptionHandler
{
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
                    ["codigo"] = "veiculo_duplicado",
                    ["campo"] = duplicado.Campo,
                }),
            ConcorrenciaCopiaVeiculoException => Results.Problem(
                title: "Conflito de sincronização",
                detail: "A cópia do veículo foi alterada por outra operação. Tente novamente.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["codigo"] = "conflito_sincronizacao" }),
            _ => null
        };

        if (resultado is null)
            return false;

        await resultado.ExecuteAsync(httpContext);
        return true;
    }
}
