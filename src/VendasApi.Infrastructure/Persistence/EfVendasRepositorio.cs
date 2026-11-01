using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Pagamentos;
using VendasApi.Application.Vendas;
using VendasApi.Domain.Veiculos;
using VendasApi.Domain.Vendas;

namespace VendasApi.Infrastructure.Persistence;

internal sealed class EfVendasRepositorio(VendasDbContext contexto) : IVendasRepositorio, IProcessamentoPagamentoRepositorio
{
    public async Task EfetuarVendaAsync(Guid veiculoId, Func<VeiculoCopia?, Venda> efetuar, CancellationToken cancellationToken)
    {
        // Sem commit explícito, o dispose da transação faz rollback: reserva e venda andam juntas ou não andam.
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);

        // Trava a linha: dois pedidos para o mesmo veículo passam um de cada vez; o segundo vê a reserva do primeiro.
        await contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM veiculos_copia WHERE \"Id\" = {veiculoId} FOR UPDATE", cancellationToken);

        var copia = await contexto.VeiculosCopia.FirstOrDefaultAsync(v => v.Id == veiculoId, cancellationToken);
        var venda = efetuar(copia);
        contexto.Vendas.Add(venda);

        await contexto.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);
    }

    public async Task<ProcessamentoPagamento> ProcessarAsync(
        string codigoPagamento,
        Func<Venda?, VeiculoCopia?, ProcessamentoPagamento> aplicar,
        CancellationToken cancellationToken)
    {
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);

        // Ordem fixa de travas (venda, depois veículo) para dois processos nunca se esperarem mutuamente.
        await contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM vendas WHERE \"CodigoPagamento\" = {codigoPagamento} FOR UPDATE", cancellationToken);
        var venda = await contexto.Vendas.FirstOrDefaultAsync(v => v.CodigoPagamento == codigoPagamento, cancellationToken);

        VeiculoCopia? copia = null;
        if (venda is not null)
        {
            await contexto.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM veiculos_copia WHERE \"Id\" = {venda.VeiculoId} FOR UPDATE", cancellationToken);
            copia = await contexto.VeiculosCopia.FirstOrDefaultAsync(v => v.Id == venda.VeiculoId, cancellationToken);
        }

        var processamento = aplicar(venda, copia);

        await contexto.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);
        return processamento;
    }
}
