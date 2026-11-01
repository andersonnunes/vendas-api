using Microsoft.EntityFrameworkCore;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Infrastructure.Persistence;

public sealed class VendasDbContext(DbContextOptions<VendasDbContext> options) : DbContext(options)
{
    public DbSet<VeiculoCopia> VeiculosCopia => Set<VeiculoCopia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VendasDbContext).Assembly);
    }
}
