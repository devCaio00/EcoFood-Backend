using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoFood.Api.Migrations
{
    /// <inheritdoc />
    public partial class PermitirMultiplasReservasPorDoacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservas_DoacaoId",
                table: "Reservas");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_DoacaoId",
                table: "Reservas",
                column: "DoacaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservas_DoacaoId",
                table: "Reservas");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_DoacaoId",
                table: "Reservas",
                column: "DoacaoId",
                unique: true);
        }
    }
}
