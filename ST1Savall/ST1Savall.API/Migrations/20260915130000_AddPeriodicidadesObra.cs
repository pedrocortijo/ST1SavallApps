using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST1Savall.API.Data;

#nullable disable

namespace ST1Savall.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260915130000_AddPeriodicidadesObra")]
public partial class AddPeriodicidadesObra : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PeriodicidadesObra",
            columns: table => new
            {
                IdPeriodicidadObra = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ObraCodigo = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                Activa = table.Column<bool>(type: "bit", nullable: false),
                Unidad = table.Column<int>(type: "int", nullable: false),
                Cantidad = table.Column<int>(type: "int", nullable: false),
                DiaSemana = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                HoraPrevista = table.Column<TimeSpan>(type: "time", nullable: false),
                TipoInicio = table.Column<int>(type: "int", nullable: false),
                FechaInicioManual = table.Column<DateTime>(type: "date", nullable: true),
                FechaUltimaRealizada = table.Column<DateTime>(type: "date", nullable: true),
                ProximaFecha = table.Column<DateTime>(type: "date", nullable: true),
                IdTipoTarea = table.Column<int>(type: "int", nullable: false),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_PeriodicidadesObra", x => x.IdPeriodicidadObra));
        migrationBuilder.CreateTable(
            name: "PeriodicidadesObraEjecuciones",
            columns: table => new
            {
                IdPeriodicidadObraEjecucion = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                IdPeriodicidadObra = table.Column<int>(type: "int", nullable: false),
                FechaPrevista = table.Column<DateTime>(type: "date", nullable: false),
                HoraPrevista = table.Column<TimeSpan>(type: "time", nullable: false),
                IdSolicitud = table.Column<int>(type: "int", nullable: true),
                Estado = table.Column<int>(type: "int", nullable: false),
                FechaRealizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PeriodicidadesObraEjecuciones", x => x.IdPeriodicidadObraEjecucion);
                table.ForeignKey("FK_PeriodicidadesObraEjecuciones_PeriodicidadesObra_IdPeriodicidadObra", x => x.IdPeriodicidadObra, "PeriodicidadesObra", "IdPeriodicidadObra", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.AddColumn<int>(name: "IdPeriodicidadObraEjecucion", table: "Solicitudes", type: "int", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_PeriodicidadesObra_ObraCodigo", table: "PeriodicidadesObra", column: "ObraCodigo", unique: true);
        migrationBuilder.CreateIndex(name: "IX_PeriodicidadesObraEjecuciones_IdPeriodicidadObra_FechaPrevista", table: "PeriodicidadesObraEjecuciones", columns: new[] { "IdPeriodicidadObra", "FechaPrevista" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_Solicitudes_IdPeriodicidadObraEjecucion", table: "Solicitudes", column: "IdPeriodicidadObraEjecucion", unique: true, filter: "[IdPeriodicidadObraEjecucion] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Solicitudes_IdPeriodicidadObraEjecucion", table: "Solicitudes");
        migrationBuilder.DropColumn(name: "IdPeriodicidadObraEjecucion", table: "Solicitudes");
        migrationBuilder.DropTable(name: "PeriodicidadesObraEjecuciones");
        migrationBuilder.DropTable(name: "PeriodicidadesObra");
    }
}