using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST1Savall.API.Data;

#nullable disable

namespace ST1Savall.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260915090000_AddTarifaAplicadaToSolicitudes")]
public partial class AddTarifaAplicadaToSolicitudes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(
            name: "TarifaAplicada",
            table: "Solicitudes",
            type: "char(2)",
            maxLength: 2,
            nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "TarifaAplicada", table: "Solicitudes");
}
