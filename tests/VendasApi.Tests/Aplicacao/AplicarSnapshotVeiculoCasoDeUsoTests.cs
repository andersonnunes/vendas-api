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
        Assert.Equal(1, repositorio.SalvarChamadas);
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
        Assert.Equal(2, repositorio.SalvarChamadas);
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

        Assert.Equal(0, repositorio.ObterChamadas);
    }

    private sealed class RepositorioEmMemoria : IVeiculoCopiaRepositorio
    {
        public List<VeiculoCopia> Copias { get; } = [];
        public int ConflitosParaSimular { get; set; }
        public int SalvarChamadas { get; private set; }
        public int ObterChamadas { get; private set; }

        public Task<VeiculoCopia?> ObterParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken)
        {
            ObterChamadas++;
            return Task.FromResult(Copias.FirstOrDefault(c => c.Id == id));
        }

        public Task AdicionarAsync(VeiculoCopia copia, CancellationToken cancellationToken)
        {
            Copias.Add(copia);
            return Task.CompletedTask;
        }

        public Task SalvarAsync(CancellationToken cancellationToken)
        {
            SalvarChamadas++;
            if (ConflitosParaSimular > 0)
            {
                ConflitosParaSimular--;
                Copias.Clear();
                throw new ConcorrenciaCopiaVeiculoException();
            }
            return Task.CompletedTask;
        }
    }
}
