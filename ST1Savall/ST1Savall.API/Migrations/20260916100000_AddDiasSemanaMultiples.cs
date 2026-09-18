using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace ST1Savall.API.Migrations;

public partial class AddDiasSemanaMultiples : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var dia in new[] { "L", "M", "X", "J", "V", "S", "D" })
            migrationBuilder.AddColumn<int>(name: dia, table: "PeriodicidadesObra", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.Sql("UPDATE PeriodicidadesObra SET L = CASE WHEN DiaSemana = 1 THEN 1 ELSE 0 END, M = CASE WHEN DiaSemana = 2 THEN 1 ELSE 0 END, X = CASE WHEN DiaSemana = 3 THEN 1 ELSE 0 END, J = CASE WHEN DiaSemana = 4 THEN 1 ELSE 0 END, V = CASE WHEN DiaSemana = 5 THEN 1 ELSE 0 END, S = CASE WHEN DiaSemana = 6 THEN 1 ELSE 0 END, D = CASE WHEN DiaSemana = 7 THEN 1 ELSE 0 END;");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var dia in new[] { "L", "M", "X", "J", "V", "S", "D" }) migrationBuilder.DropColumn(name: dia, table: "PeriodicidadesObra");
    }
}