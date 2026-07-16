using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST1Savall.API.Data;

#nullable disable

namespace ST1Savall.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260716120000_AddFiltrarToEstadosSolicitud")]
public partial class AddFiltrarToEstadosSolicitud : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "Filtrar",
            table: "EstadosSolicitud",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Filtrar",
            table: "EstadosSolicitud");
    }
}
