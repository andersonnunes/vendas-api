using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendasApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VeiculoCopiaInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "veiculos_copia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Marca = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Modelo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ano = table.Column<int>(type: "integer", nullable: false),
                    Cor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Preco = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Chassi = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Placa = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_veiculos_copia", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_veiculos_copia_chassi",
                table: "veiculos_copia",
                column: "Chassi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_veiculos_copia_placa",
                table: "veiculos_copia",
                column: "Placa",
                unique: true,
                filter: "\"Placa\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "veiculos_copia");
        }
    }
}
