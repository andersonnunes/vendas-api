using Microsoft.EntityFrameworkCore;
using Npgsql;
using VendasApi.Application.Veiculos;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Infrastructure.Persistence;

internal sealed class EfVeiculoCopiaRepositorio(VendasDbContext contexto) : IVeiculoCopiaRepositorio
{
    private const string SqlStateViolacaoUnicidade = "23505";
    private const string NomeChavePrimaria = "PK_veiculos_copia";

    public async Task AplicarAsync(Guid id, Func<VeiculoCopia?, VeiculoCopia> aplicar, CancellationToken cancellationToken)
    {
        // Sem commit explícito, o dispose da transação faz rollback: nada fica pela metade.
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Trava a linha: envios simultâneos do mesmo veículo passam um de cada vez, e a leitura abaixo vê o último commit.
            await contexto.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM veiculos_copia WHERE \"Id\" = {id} FOR UPDATE", cancellationToken);

            var existente = await contexto.VeiculosCopia.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
            var resultado = aplicar(existente);
            if (existente is null)
                contexto.VeiculosCopia.Add(resultado);

            await contexto.SaveChangesAsync(cancellationToken);
            await transacao.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (excecao.InnerException is PostgresException { SqlState: SqlStateViolacaoUnicidade } postgres)
        {
            contexto.ChangeTracker.Clear();
            if (postgres.ConstraintName == NomeChavePrimaria)
                throw new ConcorrenciaCopiaVeiculoException();

            var campo = postgres.ConstraintName?.Contains("placa", StringComparison.OrdinalIgnoreCase) == true
                ? "placa"
                : "chassi";
            throw new SnapshotVeiculoDuplicadoException(campo);
        }
    }
}
