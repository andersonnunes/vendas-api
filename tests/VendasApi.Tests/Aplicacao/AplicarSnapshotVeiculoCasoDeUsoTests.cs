using Microsoft.Extensions.Time.Testing;
using VendasApi.Application.Veiculos;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Tests.Aplicacao;

public class AplicarSnapshotVeiculoCasoDeUsoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Id = Guid.Parse("0196f0a4-0000-7000-8000-000000000001");

    private static DadosSnapshot Dados(decimal preco = 139900m, DateTimeOffset? atualizadoEm = null) => new(
        Id, "Toyota", "Corolla", 2024, "Prata", preco, "9BWZZZ377VT004251", "BRA2E19",
        StatusVeiculoCopia.Disponivel, atualizadoEm ?? Agora);

    [Fact]
    public async Task ExecutarAsync_VeiculoNovo_CriaACopia()
    {
        var repositorio = new RepositorioEmMemoria();
        var casoDeUso = new AplicarSnapshotVeiculoCasoDeUso(repositorio, new FakeTimeProvider(Agora));

        await casoDeUso.ExecutarAsync(Dados(), CancellationToken.None);

        var copia = Assert.Single(repositorio.Copias);
        Assert.Equal(Id, copia.Id);
        Assert.Equal(1, repositorio.AplicarChamadas);
    }

    [Fact]
    public async Task ExecutarAsync_VeiculoExistente_AtualizaACopia()
    {
        var repositorio = new RepositorioEmMemoria();
        var casoDeUso = new AplicarSnapshotVeiculoCasoDeUso(repositorio, new FakeTimeProvider(Agora));
        await casoDeUso.ExecutarAsync(Dados(), CancellationToken.None);

        await casoDeUso.ExecutarAsync(Dados(120000m, Agora.AddDays(1)), CancellationToken.None);

        Assert.Equal(120000m, Assert.Single(repositorio.Copias).Preco);
    }

    [Fact]
    public async Task ExecutarAsync_ConflitoNaCriacao_TentaDeNovoEAplica()
    {
        var repositorio = new RepositorioEmMemoria { ConflitosParaSimular = 1 };
        var casoDeUso = new AplicarSnapshotVeiculoCasoDeUso(repositorio, new FakeTimeProvider(Agora));

        await casoDeUso.ExecutarAsync(Dados(), CancellationToken.None);

        Assert.Single(repositorio.Copias);
        Assert.Equal(2, repositorio.AplicarChamadas);
    }

    [Fact]
    public async Task ExecutarAsync_ConflitoPersistente_PropagaAoChamador()
    {
        var repositorio = new RepositorioEmMemoria { ConflitosParaSimular = 5 };
        var casoDeUso = new AplicarSnapshotVeiculoCasoDeUso(repositorio, new FakeTimeProvider(Agora));

        await Assert.ThrowsAsync<ConcorrenciaCopiaVeiculoException>(() =>
            casoDeUso.ExecutarAsync(Dados(), CancellationToken.None));
    }

    [Fact]
    public async Task ExecutarAsync_SnapshotInvalido_NaoAbreUnidadeDeTrabalho()
    {
        var repositorio = new RepositorioEmMemoria();
        var casoDeUso = new AplicarSnapshotVeiculoCasoDeUso(repositorio, new FakeTimeProvider(Agora));

        await Assert.ThrowsAsync<SnapshotInvalidoException>(() =>
            casoDeUso.ExecutarAsync(Dados() with { Chassi = "CURTO" }, CancellationToken.None));

        Assert.Equal(0, repositorio.AplicarChamadas);
    }

    private sealed class RepositorioEmMemoria : IVeiculoCopiaRepositorio
    {
        public List<VeiculoCopia> Copias { get; } = [];
        public int ConflitosParaSimular { get; set; }
        public int AplicarChamadas { get; private set; }

        public Task AplicarAsync(Guid id, Func<VeiculoCopia?, VeiculoCopia> aplicar, CancellationToken cancellationToken)
        {
            AplicarChamadas++;
            var existente = Copias.FirstOrDefault(c => c.Id == id);
            var resultado = aplicar(existente);

            if (ConflitosParaSimular > 0)
            {
                ConflitosParaSimular--;
                throw new ConcorrenciaCopiaVeiculoException();
            }

            if (existente is null)
                Copias.Add(resultado);
            return Task.CompletedTask;
        }
    }
}
