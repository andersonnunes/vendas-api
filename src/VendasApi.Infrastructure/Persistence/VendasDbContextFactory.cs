using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VendasApi.Infrastructure.Persistence;

/// <summary>Usado só pelo `dotnet ef` em tempo de design; a aplicação usa a connection string de configuração.</summary>
internal sealed class VendasDbContextFactory : IDesignTimeDbContextFactory<VendasDbContext>
{
    public VendasDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<VendasDbContext>()
            .UseNpgsql("Host=localhost;Port=5434;Database=vendas;Username=vendas")
            .Options);
}
