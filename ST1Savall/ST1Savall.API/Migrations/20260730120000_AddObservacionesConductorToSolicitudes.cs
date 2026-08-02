using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260730120000_AddObservacionesConductorToSolicitudes")]
public partial class AddObservacionesConductorToSolicitudes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ObservacionesConductor",
            table: "Solicitudes",
            type: "varchar(max)",
            unicode: false,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ObservacionesConductor",
            table: "Solicitudes");
    }
}
