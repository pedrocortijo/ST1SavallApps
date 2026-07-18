using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST1Savall.API.Data;

#nullable disable

namespace ST1Savall.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260718140000_AddDescansoAOperarios")]
public partial class AddDescansoAOperarios : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<TimeSpan>(name: "inicio_descanso", table: "Operarios", type: "time", nullable: true);
        migrationBuilder.AddColumn<TimeSpan>(name: "fin_descanso", table: "Operarios", type: "time", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "inicio_descanso", table: "Operarios");
        migrationBuilder.DropColumn(name: "fin_descanso", table: "Operarios");
    }
}
