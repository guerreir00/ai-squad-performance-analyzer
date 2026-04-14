using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SquadIA.Migrations
{
    /// <inheritdoc />
    public partial class InitialHistorico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoricosAnalise",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomeSquad = table.Column<string>(type: "TEXT", nullable: false),
                    LeadTimeMedio = table.Column<int>(type: "INTEGER", nullable: false),
                    Throughput = table.Column<int>(type: "INTEGER", nullable: false),
                    Bugs = table.Column<int>(type: "INTEGER", nullable: false),
                    Bloqueios = table.Column<int>(type: "INTEGER", nullable: false),
                    Diagnostico = table.Column<string>(type: "TEXT", nullable: false),
                    Prioridade = table.Column<string>(type: "TEXT", nullable: false),
                    ResumoExecutivo = table.Column<string>(type: "TEXT", nullable: false),
                    ScoreSaude = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricosAnalise", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricosAnalise");
        }
    }
}
