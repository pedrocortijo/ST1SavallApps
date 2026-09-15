using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST1Savall.API.Data;

#nullable disable

namespace ST1Savall.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260904090000_AddHorarioComidaOperarios")]
public partial class AddHorarioComidaOperarios : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<TimeSpan>(name: "inicio_comida", table: "Operarios", type: "time", nullable: true);
        migrationBuilder.AddColumn<TimeSpan>(name: "fin_comida", table: "Operarios", type: "time", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "inicio_comida", table: "Operarios");
        migrationBuilder.DropColumn(name: "fin_comida", table: "Operarios");
    }
}