using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using VendasApi.Application.Pagamentos;
using VendasApi.Tests.Infraestrutura;
using VendasApi.Tests.Seguranca;

namespace VendasApi.Tests.Pagamentos;

// Registra um notificador que conta as chamadas à veiculos-api. O registro de teste acontece depois
// do registro da aplicação, então o espião é o que o container resolve.
internal sealed class ApiComNotificadorEspiao(KeycloakFixture keycloak, PostgresFixture postgres)
    : ApiComKeycloak(keycloak, postgres)
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(servicos =>
        {
            servicos.AddSingleton<NotificadorEspiao>();
            servicos.AddSingleton<IVeiculosStatusNotificador>(provedor => provedor.GetRequiredService<NotificadorEspiao>());
        });
    }
}

internal sealed class NotificadorEspiao : IVeiculosStatusNotificador
{
    private int _chamadas;

    public int Chamadas => Volatile.Read(ref _chamadas);

    public Task NotificarVendidoAsync(Guid veiculoId, DateTimeOffset ocorridoEm, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _chamadas);
        return Task.CompletedTask;
    }
}
