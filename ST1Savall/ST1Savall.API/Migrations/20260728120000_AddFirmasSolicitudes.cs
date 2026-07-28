using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260728120000_AddFirmasSolicitudes")]
public partial class AddFirmasSolicitudes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('Parametros', 'PathFirmas') IS NULL ALTER TABLE [Parametros] ADD [PathFirmas] varchar(255) NULL;");
        migrationBuilder.Sql("IF COL_LENGTH('Solicitudes', 'FirmaNombre') IS NULL ALTER TABLE [Solicitudes] ADD [FirmaNombre] varchar(50) NULL;");
        migrationBuilder.Sql("IF COL_LENGTH('Solicitudes', 'FirmaDni') IS NULL ALTER TABLE [Solicitudes] ADD [FirmaDni] varchar(25) NULL;");
        migrationBuilder.Sql("IF COL_LENGTH('Solicitudes', 'FirmaPath') IS NULL ALTER TABLE [Solicitudes] ADD [FirmaPath] varchar(255) NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PathFirmas", table: "Parametros");
        migrationBuilder.DropColumn(name: "FirmaNombre", table: "Solicitudes");
        migrationBuilder.DropColumn(name: "FirmaDni", table: "Solicitudes");
        migrationBuilder.DropColumn(name: "FirmaPath", table: "Solicitudes");
    }
}
