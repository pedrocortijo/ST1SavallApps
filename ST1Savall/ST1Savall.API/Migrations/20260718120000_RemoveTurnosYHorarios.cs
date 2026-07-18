using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST1Savall.API.Data;

#nullable disable

namespace ST1Savall.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260718120000_RemoveTurnosYHorarios")]
public partial class RemoveTurnosYHorarios : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF OBJECT_ID(N'HorariosOperarios', N'U') IS NOT NULL DROP TABLE [HorariosOperarios];");
        migrationBuilder.Sql("IF OBJECT_ID(N'Turnos', N'U') IS NOT NULL DROP TABLE [Turnos];");

        foreach (var column in new[]
        {
            "HoraInicioJornada", "HoraFinJornada", "MinutosMaximosDiarios",
            "MinutosMaximosSemanales", "TrabajaSabados", "TrabajaDomingos"
        })
        {
            migrationBuilder.Sql($"""
                IF COL_LENGTH(N'Operarios', N'{column}') IS NOT NULL
                BEGIN
                    DECLARE @constraintName sysname;
                    SELECT @constraintName = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                    WHERE dc.parent_object_id = OBJECT_ID(N'Operarios') AND c.name = N'{column}';
                    IF @constraintName IS NOT NULL EXEC(N'ALTER TABLE [Operarios] DROP CONSTRAINT [' + @constraintName + N']');
                    ALTER TABLE [Operarios] DROP COLUMN [{column}];
                END
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<TimeSpan>(name: "HoraInicioJornada", table: "Operarios", type: "time", nullable: true);
        migrationBuilder.AddColumn<TimeSpan>(name: "HoraFinJornada", table: "Operarios", type: "time", nullable: true);
        migrationBuilder.AddColumn<int>(name: "MinutosMaximosDiarios", table: "Operarios", type: "int", nullable: false, defaultValue: 480);
        migrationBuilder.AddColumn<int>(name: "MinutosMaximosSemanales", table: "Operarios", type: "int", nullable: false, defaultValue: 2400);
        migrationBuilder.AddColumn<bool>(name: "TrabajaSabados", table: "Operarios", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "TrabajaDomingos", table: "Operarios", type: "bit", nullable: false, defaultValue: false);

        migrationBuilder.CreateTable(
            name: "Turnos",
            columns: table => new
            {
                IdTurno = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                NombreTurno = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                HoraEntrada = table.Column<TimeSpan>(type: "time", nullable: false),
                HoraSalida = table.Column<TimeSpan>(type: "time", nullable: false),
                HoraInicioBreak = table.Column<TimeSpan>(type: "time", nullable: true),
                HoraFinBreak = table.Column<TimeSpan>(type: "time", nullable: true),
                TiempoAlmuerzoMinutos = table.Column<int>(type: "int", nullable: false),
                ToleranciaEntradaMinutos = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Turnos", x => x.IdTurno));

        migrationBuilder.CreateTable(
            name: "HorariosOperarios",
            columns: table => new
            {
                IdAsignacion = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                IdOperario = table.Column<int>(type: "int", nullable: false),
                IdTurno = table.Column<int>(type: "int", nullable: false),
                DiaSemana = table.Column<int>(type: "int", nullable: false),
                FechaInicioVigencia = table.Column<DateOnly>(type: "date", nullable: false),
                FechaFinVigencia = table.Column<DateOnly>(type: "date", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HorariosOperarios", x => x.IdAsignacion);
                table.ForeignKey("FK_HorariosOperarios_Operarios_IdOperario", x => x.IdOperario, "Operarios", "IdOperario", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_HorariosOperarios_Turnos_IdTurno", x => x.IdTurno, "Turnos", "IdTurno", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_HorariosOperarios_IdOperario", table: "HorariosOperarios", column: "IdOperario");
        migrationBuilder.CreateIndex(name: "IX_HorariosOperarios_IdTurno", table: "HorariosOperarios", column: "IdTurno");
    }
}
