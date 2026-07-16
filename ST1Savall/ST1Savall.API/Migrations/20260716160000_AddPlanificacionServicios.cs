using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST1Savall.API.Data;

#nullable disable

namespace ST1Savall.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260716160000_AddPlanificacionServicios")]
public partial class AddPlanificacionServicios : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "EstadoLaboral", table: "Operarios", type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Activo");
        migrationBuilder.AddColumn<string>(name: "MotivoInactividad", table: "Operarios", type: "nvarchar(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "InactivoDesde", table: "Operarios", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "InactivoHasta", table: "Operarios", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<TimeSpan>(name: "HoraInicioJornada", table: "Operarios", type: "time", nullable: true, defaultValue: new TimeSpan(8, 0, 0));
        migrationBuilder.AddColumn<TimeSpan>(name: "HoraFinJornada", table: "Operarios", type: "time", nullable: true, defaultValue: new TimeSpan(17, 0, 0));
        migrationBuilder.AddColumn<int>(name: "MinutosMaximosDiarios", table: "Operarios", type: "int", nullable: false, defaultValue: 480);
        migrationBuilder.AddColumn<int>(name: "MinutosMaximosSemanales", table: "Operarios", type: "int", nullable: false, defaultValue: 2400);
        migrationBuilder.AddColumn<bool>(name: "TrabajaSabados", table: "Operarios", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "TrabajaDomingos", table: "Operarios", type: "bit", nullable: false, defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(name: "FechaHoraInicioPlanificada", table: "Solicitudes", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "FechaHoraFinPlanificada", table: "Solicitudes", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<int>(name: "DuracionPlanificadaMinutos", table: "Solicitudes", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "DuracionViajeMinutos", table: "Solicitudes", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "DuracionOperacionMinutos", table: "Solicitudes", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "IdPlantaOrigen", table: "Solicitudes", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "IdPlantaDescarga", table: "Solicitudes", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "IdPlantaRegreso", table: "Solicitudes", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "DistanciaOrigenObraMetros", table: "Solicitudes", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "DistanciaObraDescargaMetros", table: "Solicitudes", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "DistanciaDescargaRegresoMetros", table: "Solicitudes", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "MinutosOrigenObra", table: "Solicitudes", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "MinutosObraDescarga", table: "Solicitudes", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "MinutosDescargaRegreso", table: "Solicitudes", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "DistanciaTotalMetros", table: "Solicitudes", type: "int", nullable: true);

        foreach (var name in new[] { "LatitudOrigen", "LongitudOrigen", "LatitudObra", "LongitudObra", "LatitudDescarga", "LongitudDescarga", "LatitudRegreso", "LongitudRegreso" })
            migrationBuilder.AddColumn<decimal>(name: name, table: "Solicitudes", type: "decimal(9,6)", nullable: true);

        migrationBuilder.AddColumn<bool>(name: "DuracionModificadaManualmente", table: "Solicitudes", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTime>(name: "FechaCalculoRuta", table: "Solicitudes", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ProveedorCalculoRuta", table: "Solicitudes", type: "nvarchar(30)", maxLength: 30, nullable: true);

        migrationBuilder.CreateIndex(name: "IX_Solicitudes_Conductor_Inicio_Fin", table: "Solicitudes", columns: new[] { "IdConductor", "FechaHoraInicioPlanificada", "FechaHoraFinPlanificada" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Solicitudes_Conductor_Inicio_Fin", table: "Solicitudes");
        foreach (var name in new[] { "EstadoLaboral", "MotivoInactividad", "InactivoDesde", "InactivoHasta", "HoraInicioJornada", "HoraFinJornada", "MinutosMaximosDiarios", "MinutosMaximosSemanales", "TrabajaSabados", "TrabajaDomingos" })
            migrationBuilder.DropColumn(name: name, table: "Operarios");
        foreach (var name in new[] { "FechaHoraInicioPlanificada", "FechaHoraFinPlanificada", "DuracionPlanificadaMinutos", "DuracionViajeMinutos", "DuracionOperacionMinutos", "IdPlantaOrigen", "IdPlantaDescarga", "IdPlantaRegreso", "DistanciaOrigenObraMetros", "DistanciaObraDescargaMetros", "DistanciaDescargaRegresoMetros", "MinutosOrigenObra", "MinutosObraDescarga", "MinutosDescargaRegreso", "DistanciaTotalMetros", "LatitudOrigen", "LongitudOrigen", "LatitudObra", "LongitudObra", "LatitudDescarga", "LongitudDescarga", "LatitudRegreso", "LongitudRegreso", "DuracionModificadaManualmente", "FechaCalculoRuta", "ProveedorCalculoRuta" })
            migrationBuilder.DropColumn(name: name, table: "Solicitudes");
    }
}
