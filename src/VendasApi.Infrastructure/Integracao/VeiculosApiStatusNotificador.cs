using System.Net.Http.Json;
using VendasApi.Application.Pagamentos;

namespace VendasApi.Infrastructure.Integracao;

/// <summary>
/// Chama o endpoint interno de veiculos-api que recebe o status final (US1.4). O cliente nomeado já traz
/// o token de serviço e a política de retry; aqui só se traduz falha de transporte ou de resposta.
/// </summary>
public sealed class VeiculosApiStatusNotificador(IHttpClientFactory fabrica) : IVeiculosStatusNotificador
{
    public const string NomeCliente = "veiculos-api";

    public async Task NotificarVendidoAsync(Guid veiculoId, DateTimeOffset ocorridoEm, CancellationToken cancellationToken)
    {
        var cliente = fabrica.CreateClient(NomeCliente);
        try
        {
            var resposta = await cliente.PutAsJsonAsync(
                $"/internal/veiculos/{veiculoId}/status",
                new { status = "vendido", ocorridoEm },
                cancellationToken);

            if (!resposta.IsSuccessStatusCode)
                throw new NotificacaoVeiculosFalhouException(veiculoId);
        }
        catch (HttpRequestException excecao)
        {
            throw new NotificacaoVeiculosFalhouException(veiculoId, excecao);
        }
        catch (TaskCanceledException excecao) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout do cliente (não o cancelamento da requisição): é falha de notificação, não de processamento.
            throw new NotificacaoVeiculosFalhouException(veiculoId, excecao);
        }
    }
}
