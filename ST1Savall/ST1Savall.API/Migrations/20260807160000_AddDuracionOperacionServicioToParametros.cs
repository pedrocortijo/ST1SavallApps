using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260807160000_AddDuracionOperacionServicioToParametros")]
public partial class AddDuracionOperacionServicioToParametros : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "DuracionOperacionServicioMinutos",
            table: "Parametros",
            type: "int",
            nullable: false,
            defaultValue: 30);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DuracionOperacionServicioMinutos",
            table: "Parametros");
    }
}
