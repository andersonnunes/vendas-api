using VendasApi.Application.Veiculos.Listagem;

namespace VendasApi.Tests.Aplicacao;

public class ListarVeiculosDisponiveisCasoDeUsoTests
{
    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    public async Task Executar_PaginaInvalida_Lanca(int pagina, int tamanho)
    {
        var casoDeUso = new ListarVeiculosDisponiveisCasoDeUso(new ConsultaFalsa());

        await Assert.ThrowsAsync<ParametrosPaginacaoInvalidosException>(() =>
            casoDeUso.ExecutarAsync(pagina, tamanho, CancellationToken.None));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Executar_TamanhoForaDoIntervalo_Lanca(int pagina, int tamanho)
    {
        var casoDeUso = new ListarVeiculosDisponiveisCasoDeUso(new ConsultaFalsa());

        await Assert.ThrowsAsync<ParametrosPaginacaoInvalidosException>(() =>
            casoDeUso.ExecutarAsync(pagina, tamanho, CancellationToken.None));
    }

    [Fact]
    public async Task Executar_ParametrosValidos_RepassaParaAConsulta()
    {
        var consulta = new ConsultaFalsa();
        var casoDeUso = new ListarVeiculosDisponiveisCasoDeUso(consulta);

        await casoDeUso.ExecutarAsync(3, 100, CancellationToken.None);

        Assert.Equal((3, 100), consulta.UltimaChamada);
    }

    private sealed class ConsultaFalsa : IListagemVeiculosConsulta
    {
        public (int Pagina, int Tamanho)? UltimaChamada { get; private set; }

        public Task<ResultadoPaginado<VeiculoListagemDto>> ListarDisponiveisAsync(int pagina, int tamanho, CancellationToken cancellationToken)
        {
            UltimaChamada = (pagina, tamanho);
            return Task.FromResult(new ResultadoPaginado<VeiculoListagemDto>([], pagina, tamanho, 0));
        }
    }
}
