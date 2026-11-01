using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using VendasApi.Application.Veiculos;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Infrastructure.Persistence;

internal sealed class EfVeiculoCopiaRepositorio(VendasDbContext contexto) : IVeiculoCopiaRepositorio, IAsyncDisposable
{
    private const string SqlStateViolacaoUnicidade = "23505";
    private const string NomeChavePrimaria = "PK_veiculos_copia";

    private IDbContextTransaction? _transacao;

    public async Task<VeiculoCopia?> ObterParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken)
    {
        _transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);

        // Trava a linha: envios simultâneos do mesmo veículo passam um de cada vez, e a leitura abaixo vê o último commit.
        await contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM veiculos_copia WHERE \"Id\" = {id} FOR UPDATE", cancellationToken);

        return await contexto.VeiculosCopia.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public Task AdicionarAsync(VeiculoCopia copia, CancellationToken cancellationToken)
    {
        contexto.VeiculosCopia.Add(copia);
        return Task.CompletedTask;
    }

    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
            if (_transacao is not null)
                await _transacao.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (excecao.InnerException is PostgresException { SqlState: SqlStateViolacaoUnicidade } postgres)
        {
            await DescartarAsync();
            if (postgres.ConstraintName == NomeChavePrimaria)
                throw new ConcorrenciaCopiaVeiculoException();

            var campo = postgres.ConstraintName?.Contains("placa", StringComparison.OrdinalIgnoreCase) == true
                ? "placa"
                : "chassi";
            throw new SnapshotVeiculoDuplicadoException(campo);
        }
        finally
        {
            await LiberarTransacaoAsync();
        }
    }

    public async ValueTask DisposeAsync() => await LiberarTransacaoAsync();

    private async Task DescartarAsync()
    {
        contexto.ChangeTracker.Clear();
        if (_transacao is not null)
            await _transacao.RollbackAsync();
    }

    private async Task LiberarTransacaoAsync()
    {
        if (_transacao is null)
            return;

        await _transacao.DisposeAsync();
        _transacao = null;
    }
}
