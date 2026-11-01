using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendasApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Vendas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vendas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VeiculoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    DataVenda = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CodigoPagamento = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendas_veiculos_copia_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "veiculos_copia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vendas_VeiculoId",
                table: "vendas",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "ux_vendas_codigo_pagamento",
                table: "vendas",
                column: "CodigoPagamento",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vendas");
        }
    }
}
