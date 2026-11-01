using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VendasApi.Domain.Veiculos;
using VendasApi.Domain.Vendas;
using VendasApi.Infrastructure.Persistence;
using VendasApi.Tests.Infraestrutura;
using VendasApi.Tests.Seguranca;

namespace VendasApi.Tests.Pagamentos;

// Cada teste cria a própria venda com código único: o banco de teste é compartilhado entre classes.
[Collection(AmbienteCollection.NomeColecao)]
public sealed class WebhookPagamentosControllerTests(KeycloakFixture keycloak, PostgresFixture postgres)
{
    private const string SegredoTeste = "segredo-de-teste";
    private static readonly DateTimeOffset Agora = DateTimeOffset.UtcNow;

    [Fact]
    public async Task Receber_SemAssinatura_Retorna401()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsync("/webhooks/pagamentos", Conteudo(Corpo(NovoCodigo(), "aprovado")));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Receber_AssinaturaErrada_Retorna401()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = fabrica.CreateClient();
        var corpo = Corpo(NovoCodigo(), "aprovado");

        var resposta = await Enviar(cliente, corpo, "assinatura-falsa");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Receber_AprovadoAssinado_ConcluiAVendaEMarcaOVeiculo()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var codigo = NovoCodigo();
        var veiculo = await SemearVendaReservadaAsync(fabrica, codigo);
        var cliente = fabrica.CreateClient();

        var resposta = await Enviar(cliente, Corpo(codigo, "aprovado"), Assinar(Corpo(codigo, "aprovado")));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.True((await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("alterou").GetBoolean());
        Assert.Equal(StatusVenda.Concluida, await StatusVendaAsync(fabrica, codigo));
        Assert.Equal(StatusVeiculoCopia.Vendido, await StatusVeiculoAsync(fabrica, veiculo));
    }

    [Fact]
    public async Task Receber_CanceladoAssinado_CancelaELiberaOVeiculo()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var codigo = NovoCodigo();
        var veiculo = await SemearVendaReservadaAsync(fabrica, codigo);
        var cliente = fabrica.CreateClient();

        var resposta = await Enviar(cliente, Corpo(codigo, "cancelado"), Assinar(Corpo(codigo, "cancelado")));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(StatusVenda.Cancelada, await StatusVendaAsync(fabrica, codigo));
        Assert.Equal(StatusVeiculoCopia.Disponivel, await StatusVeiculoAsync(fabrica, veiculo));
    }

    [Fact]
    public async Task Receber_MesmoResultadoDeNovo_EIdempotente()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var codigo = NovoCodigo();
        await SemearVendaReservadaAsync(fabrica, codigo);
        var cliente = fabrica.CreateClient();
        var corpo = Corpo(codigo, "aprovado");
        await Enviar(cliente, corpo, Assinar(corpo));

        var repetido = await Enviar(cliente, corpo, Assinar(corpo));

        Assert.Equal(HttpStatusCode.OK, repetido.StatusCode);
        Assert.False((await repetido.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("alterou").GetBoolean());
    }

    [Fact]
    public async Task Receber_CancelamentoDepoisDeConcluida_Retorna409()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var codigo = NovoCodigo();
        await SemearVendaReservadaAsync(fabrica, codigo);
        var cliente = fabrica.CreateClient();
        await Enviar(cliente, Corpo(codigo, "aprovado"), Assinar(Corpo(codigo, "aprovado")));
        var cancelamento = Corpo(codigo, "cancelado");

        var resposta = await Enviar(cliente, cancelamento, Assinar(cancelamento));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("transicao_invalida", (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Receber_CodigoDesconhecido_Retorna404()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = fabrica.CreateClient();
        var corpo = Corpo(NovoCodigo(), "aprovado");

        var resposta = await Enviar(cliente, corpo, Assinar(corpo));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Receber_ResultadoInvalido_Retorna400()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = fabrica.CreateClient();
        var corpo = Corpo(NovoCodigo(), "talvez");

        var resposta = await Enviar(cliente, corpo, Assinar(corpo));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Receber_ConclusaoComVeiculosApiIndisponivel_AindaRetorna200()
    {
        // O segredo de teste aponta para veiculos-api inalcançável: a notificação falha, a conclusão permanece.
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var codigo = NovoCodigo();
        await SemearVendaReservadaAsync(fabrica, codigo);
        var cliente = fabrica.CreateClient();
        var corpo = Corpo(codigo, "aprovado");

        var resposta = await Enviar(cliente, corpo, Assinar(corpo));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(StatusVenda.Concluida, await StatusVendaAsync(fabrica, codigo));
    }

    private static string NovoCodigo() => Guid.NewGuid().ToString("N");

    private static string Corpo(string codigo, string resultado) =>
        JsonSerializer.Serialize(new { codigoPagamento = codigo, resultado });

    private static string Assinar(string corpo)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(SegredoTeste), Encoding.UTF8.GetBytes(corpo));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static StringContent Conteudo(string corpo) => new(corpo, Encoding.UTF8, "application/json");

    private static Task<HttpResponseMessage> Enviar(HttpClient cliente, string corpo, string assinatura)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/webhooks/pagamentos") { Content = Conteudo(corpo) };
        requisicao.Headers.Add("X-Assinatura", assinatura);
        return cliente.SendAsync(requisicao);
    }

    private static async Task<Guid> SemearVendaReservadaAsync(ApiComKeycloak fabrica, string codigo)
    {
        var veiculoId = Guid.NewGuid();
        var dados = new DadosSnapshot(veiculoId, "Toyota", "Corolla", 2024, "Prata", 100000m, Guid.NewGuid().ToString("N")[..17].ToUpperInvariant(), null, StatusVeiculoCopia.Disponivel, Agora);
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VendasDbContext>();
        var copia = VeiculoCopia.Criar(dados, Agora.Year);
        copia.Reservar();
        var hoje = DateOnly.FromDateTime(Agora.UtcDateTime);
        contexto.VeiculosCopia.Add(copia);
        contexto.Vendas.Add(Venda.Criar(Guid.NewGuid(), veiculoId, "52998224725", hoje, hoje, codigo, Agora));
        await contexto.SaveChangesAsync();
        return veiculoId;
    }

    private static async Task<StatusVenda> StatusVendaAsync(ApiComKeycloak fabrica, string codigo)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VendasDbContext>();
        return (await contexto.Vendas.AsNoTracking().FirstAsync(v => v.CodigoPagamento == codigo)).Status;
    }

    private static async Task<StatusVeiculoCopia> StatusVeiculoAsync(ApiComKeycloak fabrica, Guid id)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VendasDbContext>();
        return (await contexto.VeiculosCopia.AsNoTracking().FirstAsync(v => v.Id == id)).Status;
    }
}
