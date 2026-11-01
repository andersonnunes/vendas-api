using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VendasApi.Domain.Veiculos;
using VendasApi.Infrastructure.Persistence;
using VendasApi.Tests.Infraestrutura;
using VendasApi.Tests.Seguranca;

namespace VendasApi.Tests.Listagem;

// O banco de teste é compartilhado entre classes: as asserções checam propriedades (ordem, exclusão, consistência
// entre páginas) e não contagens absolutas.
[Collection(AmbienteCollection.NomeColecao)]
public sealed class ListagemVeiculosDisponiveisControllerTests(KeycloakFixture keycloak, PostgresFixture postgres)
{
    private static readonly DateTimeOffset Agora = DateTimeOffset.UtcNow;

    [Fact]
    public async Task Listar_SemToken_Retorna200()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/veiculos");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Listar_NaoTraReservadosNemVendidos()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var reservado = await SemearAsync(fabrica, 0.07m, reservar: true);
        var vendido = await SemearAsync(fabrica, 0.07m, status: StatusVeiculoCopia.Vendido);

        var itens = await ItensAsync(fabrica, "tamanho=100");

        Assert.DoesNotContain(itens, i => i.GetProperty("id").GetGuid() == reservado);
        Assert.DoesNotContain(itens, i => i.GetProperty("id").GetGuid() == vendido);
    }

    [Fact]
    public async Task Listar_OrdenaPorPrecoCrescenteEDepoisPorId()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        await SemearAsync(fabrica, 0.06m);
        await SemearAsync(fabrica, 0.06m);
        await SemearAsync(fabrica, 0.05m);

        var itens = await ItensAsync(fabrica, "tamanho=100");

        var chaves = itens.Select(i => (Preco: i.GetProperty("preco").GetDecimal(), Id: i.GetProperty("id").GetGuid())).ToList();
        Assert.Equal(chaves.OrderBy(c => c.Preco).ThenBy(c => c.Id).ToList(), chaves);
    }

    [Fact]
    public async Task Listar_PaginasSaoConsistentesComAListagemCompleta()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var completa = (await ItensAsync(fabrica, "tamanho=4")).Select(i => i.GetProperty("id").GetGuid()).ToList();

        var primeira = (await ItensAsync(fabrica, "pagina=1&tamanho=2")).Select(i => i.GetProperty("id").GetGuid()).ToList();
        var segunda = (await ItensAsync(fabrica, "pagina=2&tamanho=2")).Select(i => i.GetProperty("id").GetGuid()).ToList();

        Assert.Equal(completa, primeira.Concat(segunda).ToList());
    }

    [Fact]
    public async Task Listar_RespostaNaoExpoeChassiNemPlaca()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        await SemearAsync(fabrica, 0.04m);

        var itens = await ItensAsync(fabrica, "tamanho=5");

        Assert.All(itens, i =>
        {
            Assert.False(i.TryGetProperty("chassi", out _));
            Assert.False(i.TryGetProperty("placa", out _));
        });
    }

    [Theory]
    [InlineData("pagina=0")]
    [InlineData("tamanho=0")]
    [InlineData("tamanho=101")]
    public async Task Listar_ParametroForaDoIntervalo_Retorna400(string query)
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.GetAsync($"/veiculos?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Listar_SnapshotVendidoTiraOVeiculoDaListagem()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var id = await SemearAsync(fabrica, 0.03m);
        var veiculos = await ClienteAsync(fabrica, "veiculos-api", "dev-veiculos-api-secret");

        var snapshot = await veiculos.PutAsJsonAsync($"/internal/veiculos/{id}", new
        {
            id,
            marca = "Toyota",
            modelo = "Corolla",
            ano = 2024,
            cor = "Prata",
            preco = 0.03m,
            chassi = await ChassiAsync(fabrica, id),
            placa = (string?)null,
            status = "vendido",
            atualizadoEm = Agora.AddMinutes(5),
        });
        var itens = await ItensAsync(fabrica, "tamanho=100");

        Assert.Equal(HttpStatusCode.OK, snapshot.StatusCode);
        Assert.DoesNotContain(itens, i => i.GetProperty("id").GetGuid() == id);
    }

    private async Task<HttpClient> ClienteAsync(ApiComKeycloak fabrica, string clientId, string clientSecret)
    {
        var token = await keycloak.ObterTokenServicoAsync(clientId, clientSecret);
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    private static async Task<List<JsonElement>> ItensAsync(ApiComKeycloak fabrica, string query)
    {
        var cliente = fabrica.CreateClient();
        var corpo = await cliente.GetFromJsonAsync<JsonElement>($"/veiculos?{query}");
        return corpo.GetProperty("itens").EnumerateArray().Select(e => e.Clone()).ToList();
    }

    private static async Task<Guid> SemearAsync(
        ApiComKeycloak fabrica,
        decimal preco,
        StatusVeiculoCopia status = StatusVeiculoCopia.Disponivel,
        bool reservar = false)
    {
        var id = Guid.NewGuid();
        var dados = new DadosSnapshot(id, "Toyota", "Corolla", 2024, "Prata", preco, NovoChassi(), null, status, Agora);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VendasDbContext>();
        var copia = VeiculoCopia.Criar(dados, Agora.Year);
        if (reservar)
            copia.Reservar();
        contexto.VeiculosCopia.Add(copia);
        await contexto.SaveChangesAsync();
        return id;
    }

    private static async Task<string> ChassiAsync(ApiComKeycloak fabrica, Guid id)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VendasDbContext>();
        return (await contexto.VeiculosCopia.AsNoTracking().FirstAsync(v => v.Id == id)).Chassi;
    }

    private static string NovoChassi() => Guid.NewGuid().ToString("N")[..17].ToUpperInvariant();
}
