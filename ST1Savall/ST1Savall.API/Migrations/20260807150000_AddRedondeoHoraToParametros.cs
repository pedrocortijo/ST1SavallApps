using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260807150000_AddRedondeoHoraToParametros")]
public partial class AddRedondeoHoraToParametros : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "RedondeoHora",
            table: "Parametros",
            type: "int",
            nullable: false,
            defaultValue: 5);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RedondeoHora",
            table: "Parametros");
    }
}
