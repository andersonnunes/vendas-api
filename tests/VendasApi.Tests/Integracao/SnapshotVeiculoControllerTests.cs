using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VendasApi.Infrastructure.Persistence;
using VendasApi.Tests.Infraestrutura;
using VendasApi.Tests.Seguranca;

namespace VendasApi.Tests.Integracao;

[Collection(AmbienteCollection.NomeColecao)]
public sealed class SnapshotVeiculoControllerTests(KeycloakFixture keycloak, PostgresFixture postgres)
{
    private static readonly DateTimeOffset Antes = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Depois = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Aplicar_SemToken_Retorna401()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.PutAsJsonAsync($"/internal/veiculos/{Guid.NewGuid()}", Snapshot(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Aplicar_TokenDoProprioVendasApi_Retorna403()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteAsync(fabrica, "vendas-api", "dev-vendas-api-secret");
        var id = Guid.NewGuid();

        var resposta = await cliente.PutAsJsonAsync($"/internal/veiculos/{id}", Snapshot(id));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Aplicar_TokenDeVeiculosApi_CriaACopiaEEhIdempotente()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteAsync(fabrica, "veiculos-api", "dev-veiculos-api-secret");
        var id = Guid.NewGuid();

        var primeira = await cliente.PutAsJsonAsync($"/internal/veiculos/{id}", Snapshot(id, atualizadoEm: Antes));
        var repetida = await cliente.PutAsJsonAsync($"/internal/veiculos/{id}", Snapshot(id, atualizadoEm: Antes));

        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repetida.StatusCode);
        Assert.Equal(1, await ContarAsync(fabrica, id));
    }

    [Fact]
    public async Task Aplicar_SnapshotMaisAntigo_NaoAlteraACopia()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteAsync(fabrica, "veiculos-api", "dev-veiculos-api-secret");
        var id = Guid.NewGuid();
        await cliente.PutAsJsonAsync($"/internal/veiculos/{id}", Snapshot(id, atualizadoEm: Depois, modelo: "Novo"));

        var antigo = await cliente.PutAsJsonAsync($"/internal/veiculos/{id}", Snapshot(id, atualizadoEm: Antes, modelo: "Antigo"));

        Assert.Equal(HttpStatusCode.OK, antigo.StatusCode);
        Assert.Equal("Novo", await ModeloAsync(fabrica, id));
    }

    [Fact]
    public async Task Aplicar_ReservaEmAndamento_NaoEDesfeitaPorSnapshotDisponivel()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteAsync(fabrica, "veiculos-api", "dev-veiculos-api-secret");
        var id = Guid.NewGuid();
        await cliente.PutAsJsonAsync($"/internal/veiculos/{id}", Snapshot(id, atualizadoEm: Antes));
        await MarcarReservadoAsync(fabrica, id);

        await cliente.PutAsJsonAsync($"/internal/veiculos/{id}", Snapshot(id, atualizadoEm: Depois, preco: 120000));

        Assert.Equal("reservado", await StatusAsync(fabrica, id));
        Assert.Equal(120000m, await PrecoAsync(fabrica, id));
    }

    [Fact]
    public async Task Aplicar_IdDaRotaDiferenteDoCorpo_Retorna400()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteAsync(fabrica, "veiculos-api", "dev-veiculos-api-secret");

        var resposta = await cliente.PutAsJsonAsync($"/internal/veiculos/{Guid.NewGuid()}", Snapshot(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Aplicar_CampoObrigatorioAusente_Retorna400()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteAsync(fabrica, "veiculos-api", "dev-veiculos-api-secret");
        var id = Guid.NewGuid();

        var resposta = await cliente.PutAsJsonAsync($"/internal/veiculos/{id}", new { id, marca = "Toyota" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Aplicar_ChassiInvalido_Retorna400ComErroNoCampo()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteAsync(fabrica, "veiculos-api", "dev-veiculos-api-secret");
        var id = Guid.NewGuid();

        var resposta = await cliente.PutAsJsonAsync($"/internal/veiculos/{id}", Snapshot(id, chassi: "CURTO"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(corpo.GetProperty("errors").TryGetProperty("chassi", out _));
    }

    [Fact]
    public async Task Aplicar_StatusReservadoDoCatalogo_Retorna400()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteAsync(fabrica, "veiculos-api", "dev-veiculos-api-secret");
        var id = Guid.NewGuid();

        var resposta = await cliente.PutAsJsonAsync($"/internal/veiculos/{id}", Snapshot(id, status: "reservado"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Aplicar_ChassiDeOutroVeiculo_Retorna409()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteAsync(fabrica, "veiculos-api", "dev-veiculos-api-secret");
        var chassi = Guid.NewGuid().ToString("N")[..17].ToUpperInvariant();
        var primeiro = Guid.NewGuid();
        var segundo = Guid.NewGuid();
        await cliente.PutAsJsonAsync($"/internal/veiculos/{primeiro}", Snapshot(primeiro, chassi: chassi));

        var resposta = await cliente.PutAsJsonAsync($"/internal/veiculos/{segundo}", Snapshot(segundo, chassi: chassi));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("veiculo_duplicado", corpo.GetProperty("codigo").GetString());
        Assert.Equal("chassi", corpo.GetProperty("campo").GetString());
    }

    private static object Snapshot(
        Guid id,
        DateTimeOffset? atualizadoEm = null,
        string modelo = "Corolla",
        decimal preco = 139900m,
        string? chassi = null,
        string status = "disponivel") => new
        {
            id,
            marca = "Toyota",
            modelo,
            ano = 2024,
            cor = "Prata",
            preco,
            chassi = chassi ?? Guid.NewGuid().ToString("N")[..17].ToUpperInvariant(),
            placa = (string?)null,
            status,
            atualizadoEm = atualizadoEm ?? Antes,
        };

    private async Task<HttpClient> ClienteAsync(ApiComKeycloak fabrica, string clientId, string clientSecret)
    {
        var token = await keycloak.ObterTokenServicoAsync(clientId, clientSecret);
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    private static async Task<int> ContarAsync(ApiComKeycloak fabrica, Guid id)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VendasDbContext>();
        return await contexto.VeiculosCopia.CountAsync(v => v.Id == id);
    }

    private static async Task<string> ModeloAsync(ApiComKeycloak fabrica, Guid id)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VendasDbContext>();
        return (await contexto.VeiculosCopia.AsNoTracking().FirstAsync(v => v.Id == id)).Modelo;
    }

    private static async Task<decimal> PrecoAsync(ApiComKeycloak fabrica, Guid id)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VendasDbContext>();
        return (await contexto.VeiculosCopia.AsNoTracking().FirstAsync(v => v.Id == id)).Preco;
    }

    private static async Task<string> StatusAsync(ApiComKeycloak fabrica, Guid id)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VendasDbContext>();
        return (await contexto.VeiculosCopia.AsNoTracking().FirstAsync(v => v.Id == id)).Status.ToString().ToLowerInvariant();
    }

    private static async Task MarcarReservadoAsync(ApiComKeycloak fabrica, Guid id)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VendasDbContext>();
        await contexto.Database.ExecuteSqlInterpolatedAsync($"UPDATE veiculos_copia SET \"Status\" = 'Reservado' WHERE \"Id\" = {id}");
    }
}
