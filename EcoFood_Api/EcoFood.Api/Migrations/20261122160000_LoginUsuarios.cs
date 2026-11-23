using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoFood.Api.Migrations;

public partial class LoginUsuarios : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SenhaHash",
            table: "Usuarios",
            type: "varchar(255)",
            maxLength: 255,
            nullable: true);

        AlterarExclusaoDeUsuarios(migrationBuilder, ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        AlterarExclusaoDeUsuarios(migrationBuilder, ReferentialAction.Cascade);
        migrationBuilder.DropColumn(name: "SenhaHash", table: "Usuarios");
    }

    private static void AlterarExclusaoDeUsuarios(
        MigrationBuilder migrationBuilder, ReferentialAction onDelete)
    {
        migrationBuilder.DropForeignKey("FK_Doacoes_Usuarios_DoadorId", "Doacoes");
        migrationBuilder.AddForeignKey(
            "FK_Doacoes_Usuarios_DoadorId", "Doacoes", "DoadorId", "Usuarios", principalColumn: "Id",
            onDelete: onDelete);

        migrationBuilder.DropForeignKey("FK_Reservas_Usuarios_ReceptorId", "Reservas");
        migrationBuilder.AddForeignKey(
            "FK_Reservas_Usuarios_ReceptorId", "Reservas", "ReceptorId", "Usuarios", principalColumn: "Id",
            onDelete: onDelete);
    }
}
