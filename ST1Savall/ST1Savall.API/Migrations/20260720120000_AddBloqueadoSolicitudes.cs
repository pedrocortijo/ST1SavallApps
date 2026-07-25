using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260720120000_AddBloqueadoSolicitudes")]
public partial class AddBloqueadoSolicitudes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "Bloqueado",
            table: "Solicitudes",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Bloqueado",
            table: "Solicitudes");
    }
}
