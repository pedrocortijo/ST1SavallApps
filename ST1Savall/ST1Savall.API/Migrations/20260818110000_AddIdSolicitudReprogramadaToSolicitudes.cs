using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260818110000_AddIdSolicitudReprogramadaToSolicitudes")]
public partial class AddIdSolicitudReprogramadaToSolicitudes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "IdSolicitudReprogramada",
            table: "Solicitudes",
            type: "int",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IdSolicitudReprogramada",
            table: "Solicitudes");
    }
}
