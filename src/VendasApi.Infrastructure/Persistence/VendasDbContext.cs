using Microsoft.EntityFrameworkCore;
using VendasApi.Domain.Veiculos;
using VendasApi.Domain.Vendas;

namespace VendasApi.Infrastructure.Persistence;

public sealed class VendasDbContext(DbContextOptions<VendasDbContext> options) : DbContext(options)
{
    public DbSet<VeiculoCopia> VeiculosCopia => Set<VeiculoCopia>();

    public DbSet<Venda> Vendas => Set<Venda>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VendasDbContext).Assembly);
    }
}
