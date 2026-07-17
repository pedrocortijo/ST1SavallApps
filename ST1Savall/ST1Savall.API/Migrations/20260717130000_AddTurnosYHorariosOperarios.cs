using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

public partial class AddTurnosYHorariosOperarios : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Turnos",
            columns: table => new
            {
                IdTurno = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                NombreTurno = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                HoraEntrada = table.Column<TimeSpan>(type: "time", nullable: false),
                HoraSalida = table.Column<TimeSpan>(type: "time", nullable: false),
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

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "HorariosOperarios");
        migrationBuilder.DropTable(name: "Turnos");
    }
}
