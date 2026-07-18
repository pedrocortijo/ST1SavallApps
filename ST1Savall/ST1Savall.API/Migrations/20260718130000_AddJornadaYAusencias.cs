using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST1Savall.API.Data;

#nullable disable

namespace ST1Savall.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260718130000_AddJornadaYAusencias")]
public partial class AddJornadaYAusencias : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<TimeSpan>(name: "inicio_jornada", table: "Operarios", type: "time", nullable: false, defaultValue: new TimeSpan(8, 0, 0));
        migrationBuilder.AddColumn<TimeSpan>(name: "fin_jornada", table: "Operarios", type: "time", nullable: false, defaultValue: new TimeSpan(17, 0, 0));

        migrationBuilder.CreateTable(
            name: "Ausencias",
            columns: table => new
            {
                IdAusencia = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                IdConductor = table.Column<int>(type: "int", nullable: false),
                FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                FechaFin = table.Column<DateOnly>(type: "date", nullable: false),
                Tipo = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Ausencias", x => x.IdAusencia);
                table.ForeignKey("FK_Ausencias_Operarios_IdConductor", x => x.IdConductor, "Operarios", "IdOperario", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_Ausencias_IdConductor_FechaInicio", table: "Ausencias", columns: new[] { "IdConductor", "FechaInicio" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Ausencias");
        migrationBuilder.DropColumn(name: "inicio_jornada", table: "Operarios");
        migrationBuilder.DropColumn(name: "fin_jornada", table: "Operarios");
    }
}
