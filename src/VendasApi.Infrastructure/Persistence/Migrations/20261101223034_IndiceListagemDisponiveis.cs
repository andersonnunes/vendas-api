using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendasApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndiceListagemDisponiveis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_veiculos_copia_status_preco_id",
                table: "veiculos_copia",
                columns: new[] { "Status", "Preco", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_veiculos_copia_status_preco_id",
                table: "veiculos_copia");
        }
    }
}
