using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VendasApi.Domain.Veiculos;
using VendasApi.Infrastructure.Persistence;
using VendasApi.Tests.Infraestrutura;
using VendasApi.Tests.Seguranca;

namespace VendasApi.Tests.Listagem;

// O banco de teste é compartilhado entre classes: as asserções checam propriedades (filtro, ordem,
// consistência entre páginas) e não contagens absolutas.
[Collection(AmbienteCollection.NomeColecao)]
public sealed class ListagemVeiculosVendidosControllerTests(KeycloakFixture keycloak, PostgresFixture postgres)
{
    private static readonly DateTimeOffset Agora = DateTimeOffset.UtcNow;

    [Fact]
    public async Task Listar_SemToken_Retorna401()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/veiculos/vendidos");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Listar_TokenDeVendedor_Retorna403()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteAsync(fabrica, "vendedor-dev", "dev-vendedor");

        var resposta = await cliente.GetAsync("/veiculos/vendidos");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Listar_TokenDeAdministrador_RetornaSoVendidos()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var vendido = await SemearAsync(fabrica, 0.08m, StatusVeiculoCopia.Vendido);
        var disponivel = await SemearAsync(fabrica, 0.08m, StatusVeiculoCopia.Disponivel);
        var reservado = await SemearAsync(fabrica, 0.08m, StatusVeiculoCopia.Disponivel, reservar: true);
        var cliente = await ClienteAsync(fabrica, "admin-dev", "dev-admin");

        var itens = await ItensAsync(cliente, "tamanho=100");

        var ids = itens.Select(i => i.GetProperty("id").GetGuid()).ToList();
        Assert.Contains(vendido, ids);
        Assert.DoesNotContain(disponivel, ids);
        Assert.DoesNotContain(reservado, ids);
    }

    [Fact]
    public async Task Listar_OrdenaPorPrecoCrescenteEDepoisPorId()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        await SemearAsync(fabrica, 0.09m, StatusVeiculoCopia.Vendido);
        await SemearAsync(fabrica, 0.09m, StatusVeiculoCopia.Vendido);
        await SemearAsync(fabrica, 0.02m, StatusVeiculoCopia.Vendido);
        var cliente = await ClienteAsync(fabrica, "admin-dev", "dev-admin");

        var itens = await ItensAsync(cliente, "tamanho=100");

        var chaves = itens.Select(i => (Preco: i.GetProperty("preco").GetDecimal(), Id: i.GetProperty("id").GetGuid())).ToList();
        Assert.Equal(chaves.OrderBy(c => c.Preco).ThenBy(c => c.Id).ToList(), chaves);
    }

    [Fact]
    public async Task Listar_RespostaTrazChassiEPlaca()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var placa = Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();
        var id = await SemearAsync(fabrica, 0.01m, StatusVeiculoCopia.Vendido, placa: placa);
        var cliente = await ClienteAsync(fabrica, "admin-dev", "dev-admin");

        var itens = await ItensAsync(cliente, "tamanho=100");

        var item = itens.Single(i => i.GetProperty("id").GetGuid() == id);
        Assert.False(string.IsNullOrEmpty(item.GetProperty("chassi").GetString()));
        Assert.Equal(placa, item.GetProperty("placa").GetString());
    }

    [Fact]
    public async Task Listar_PaginasSaoConsistentesComAListagemCompleta()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteAsync(fabrica, "admin-dev", "dev-admin");
        var completa = (await ItensAsync(cliente, "tamanho=4")).Select(i => i.GetProperty("id").GetGuid()).ToList();

        var primeira = (await ItensAsync(cliente, "pagina=1&tamanho=2")).Select(i => i.GetProperty("id").GetGuid()).ToList();
        var segunda = (await ItensAsync(cliente, "pagina=2&tamanho=2")).Select(i => i.GetProperty("id").GetGuid()).ToList();

        Assert.Equal(completa, primeira.Concat(segunda).ToList());
    }

    [Theory]
    [InlineData("pagina=0")]
    [InlineData("tamanho=0")]
    [InlineData("tamanho=101")]
    public async Task Listar_ParametroForaDoIntervalo_Retorna400(string query)
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteAsync(fabrica, "admin-dev", "dev-admin");

        var resposta = await cliente.GetAsync($"/veiculos/vendidos?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    private async Task<HttpClient> ClienteAsync(ApiComKeycloak fabrica, string usuario, string senha)
    {
        var token = await keycloak.ObterTokenUsuarioAsync(usuario, senha);
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    private static async Task<List<JsonElement>> ItensAsync(HttpClient cliente, string query)
    {
        var corpo = await cliente.GetFromJsonAsync<JsonElement>($"/veiculos/vendidos?{query}");
        return corpo.GetProperty("itens").EnumerateArray().Select(e => e.Clone()).ToList();
    }

    private static async Task<Guid> SemearAsync(
        ApiComKeycloak fabrica,
        decimal preco,
        StatusVeiculoCopia status,
        bool reservar = false,
        string? placa = null)
    {
        var id = Guid.NewGuid();
        var dados = new DadosSnapshot(id, "Toyota", "Corolla", 2024, "Prata", preco, NovoChassi(), placa, status, Agora);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VendasDbContext>();
        var copia = VeiculoCopia.Criar(dados, Agora.Year);
        if (reservar)
            copia.Reservar();
        contexto.VeiculosCopia.Add(copia);
        await contexto.SaveChangesAsync();
        return id;
    }

    private static string NovoChassi() => Guid.NewGuid().ToString("N")[..17].ToUpperInvariant();
}
