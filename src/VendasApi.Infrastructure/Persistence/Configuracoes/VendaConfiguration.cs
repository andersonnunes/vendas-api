using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VendasApi.Domain.Vendas;

namespace VendasApi.Infrastructure.Persistence.Configuracoes;

internal sealed class VendaConfiguration : IEntityTypeConfiguration<Venda>
{
    public void Configure(EntityTypeBuilder<Venda> builder)
    {
        builder.ToTable("vendas");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.VeiculoId).IsRequired();
        builder.Property(v => v.Cpf).HasMaxLength(11).IsRequired();
        builder.Property(v => v.DataVenda).IsRequired();
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.CodigoPagamento).HasMaxLength(64).IsRequired();
        builder.Property(v => v.CriadoEm).IsRequired();

        // Um veículo tem no máximo uma venda ativa; a trava de linha em veiculos_copia já impede a corrida.
        builder.HasOne<Domain.Veiculos.VeiculoCopia>()
            .WithMany()
            .HasForeignKey(v => v.VeiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => v.CodigoPagamento)
            .IsUnique()
            .HasDatabaseName("ux_vendas_codigo_pagamento");
    }
}
