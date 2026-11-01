using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Infrastructure.Persistence.Configuracoes;

internal sealed class VeiculoCopiaConfiguration : IEntityTypeConfiguration<VeiculoCopia>
{
    public void Configure(EntityTypeBuilder<VeiculoCopia> builder)
    {
        builder.ToTable("veiculos_copia");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.Marca).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Modelo).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Ano).IsRequired();
        builder.Property(v => v.Cor).HasMaxLength(30).IsRequired();
        builder.Property(v => v.Preco).HasPrecision(12, 2).IsRequired();
        builder.Property(v => v.Chassi).HasMaxLength(17).IsRequired();
        builder.Property(v => v.Placa).HasMaxLength(7);
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.AtualizadoEm).IsRequired();

        builder.HasIndex(v => v.Chassi)
            .IsUnique()
            .HasDatabaseName("ux_veiculos_copia_chassi");

        builder.HasIndex(v => v.Placa)
            .IsUnique()
            .HasFilter("\"Placa\" IS NOT NULL")
            .HasDatabaseName("ux_veiculos_copia_placa");

        // Atende o filtro por status e a ordenação por preço e id da listagem pública (US2.2).
        builder.HasIndex(v => new { v.Status, v.Preco, v.Id })
            .HasDatabaseName("ix_veiculos_copia_status_preco_id");
    }
}
