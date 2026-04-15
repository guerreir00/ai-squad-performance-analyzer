using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SquadIA.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricoIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_HistoricosAnalise_CriadoEm",
                table: "HistoricosAnalise",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosAnalise_NomeSquad",
                table: "HistoricosAnalise",
                column: "NomeSquad");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HistoricosAnalise_CriadoEm",
                table: "HistoricosAnalise");

            migrationBuilder.DropIndex(
                name: "IX_HistoricosAnalise_NomeSquad",
                table: "HistoricosAnalise");
        }
    }
}
