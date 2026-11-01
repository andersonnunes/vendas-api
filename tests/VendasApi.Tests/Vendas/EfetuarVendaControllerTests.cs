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

namespace VendasApi.Tests.Vendas;

// O banco de teste é compartilhado entre classes: os veículos de cada teste são criados com id próprio.
[Collection(AmbienteCollection.NomeColecao)]
public sealed class EfetuarVendaControllerTests(KeycloakFixture keycloak, PostgresFixture postgres)
{
    private const string CpfValido = "52998224725";
    private static readonly DateTimeOffset Agora = DateTimeOffset.UtcNow;

    [Fact]
    public async Task Efetuar_SemToken_Retorna401()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/vendas", Corpo(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Efetuar_VendedorEVeiculoDisponivel_Retorna201ComReservaEVendaCriada()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var veiculo = await SemearAsync(fabrica);
        var cliente = await ClienteAsync(fabrica, "vendedor-dev", "dev-vendedor");

        var resposta = await cliente.PostAsJsonAsync("/vendas", Corpo(veiculo));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("***.982.247-**", corpo.GetProperty("cpfMascarado").GetString());
        Assert.Equal("reservada", corpo.GetProperty("status").GetString());
        Assert.False(string.IsNullOrEmpty(corpo.GetProperty("codigoPagamento").GetString()));
        Assert.Equal("Reservado", await StatusDoVeiculoAsync(fabrica, veiculo));
    }

    [Fact]
    public async Task Efetuar_AdministradorTambemPodeVender()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var veiculo = await SemearAsync(fabrica);
        var cliente = await ClienteAsync(fabrica, "admin-dev", "dev-admin");

        var resposta = await cliente.PostAsJsonAsync("/vendas", Corpo(veiculo));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Fact]
    public async Task Efetuar_VeiculoInexistente_Retorna404()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteAsync(fabrica, "vendedor-dev", "dev-vendedor");

        var resposta = await cliente.PostAsJsonAsync("/vendas", Corpo(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Efetuar_VeiculoJaReservado_Retorna409ComCodigo()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var veiculo = await SemearAsync(fabrica);
        var cliente = await ClienteAsync(fabrica, "vendedor-dev", "dev-vendedor");
        await cliente.PostAsJsonAsync("/vendas", Corpo(veiculo));

        var segunda = await cliente.PostAsJsonAsync("/vendas", Corpo(veiculo));

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
        var corpo = await segunda.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("veiculo_indisponivel", corpo.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Efetuar_CpfInvalido_Retorna400NoCampoCpf()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var veiculo = await SemearAsync(fabrica);
        var cliente = await ClienteAsync(fabrica, "vendedor-dev", "dev-vendedor");

        var resposta = await cliente.PostAsJsonAsync("/vendas", new { veiculoId = veiculo, cpf = "11111111111", dataVenda = DateOnly.FromDateTime(DateTime.UtcNow) });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(corpo.GetProperty("errors").TryGetProperty("cpf", out _));
        Assert.Equal("Disponivel", await StatusDoVeiculoAsync(fabrica, veiculo));
    }

    [Fact]
    public async Task Efetuar_DoisPedidosSimultaneosParaOMesmoVeiculo_SoUmEhAceito()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var veiculo = await SemearAsync(fabrica);
        var cliente = await ClienteAsync(fabrica, "vendedor-dev", "dev-vendedor");

        var respostas = await Task.WhenAll(
            cliente.PostAsJsonAsync("/vendas", Corpo(veiculo)),
            cliente.PostAsJsonAsync("/vendas", Corpo(veiculo)));

        var codigos = respostas.Select(r => r.StatusCode).OrderBy(c => c).ToList();
        Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }, codigos);
    }

    [Fact]
    public async Task Efetuar_VeiculoReservado_SaiDaListagemPublica()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var veiculo = await SemearAsync(fabrica);
        var cliente = await ClienteAsync(fabrica, "vendedor-dev", "dev-vendedor");
        await cliente.PostAsJsonAsync("/vendas", Corpo(veiculo));

        var publica = await fabrica.CreateClient().GetFromJsonAsync<JsonElement>("/veiculos?tamanho=100");

        var ids = publica.GetProperty("itens").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).ToList();
        Assert.DoesNotContain(veiculo, ids);
    }

    private static object Corpo(Guid veiculoId) => new
    {
        veiculoId,
        cpf = CpfValido,
        dataVenda = DateOnly.FromDateTime(DateTime.UtcNow),
    };

    private async Task<HttpClient> ClienteAsync(ApiComKeycloak fabrica, string usuario, string senha)
    {
        var token = await keycloak.ObterTokenUsuarioAsync(usuario, senha);
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    private static async Task<Guid> SemearAsync(ApiComKeycloak fabrica)
    {
        var id = Guid.NewGuid();
        var dados = new DadosSnapshot(id, "Toyota", "Corolla", 2024, "Prata", 100000m, Guid.NewGuid().ToString("N")[..17].ToUpperInvariant(), null, StatusVeiculoCopia.Disponivel, Agora);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VendasDbContext>();
        contexto.VeiculosCopia.Add(VeiculoCopia.Criar(dados, Agora.Year));
        await contexto.SaveChangesAsync();
        return id;
    }

    private static async Task<string> StatusDoVeiculoAsync(ApiComKeycloak fabrica, Guid id)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VendasDbContext>();
        return (await contexto.VeiculosCopia.AsNoTracking().FirstAsync(v => v.Id == id)).Status.ToString();
    }
}
