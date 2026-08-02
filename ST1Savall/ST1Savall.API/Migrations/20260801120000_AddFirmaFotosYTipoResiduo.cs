using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260801120000_AddFirmaFotosYTipoResiduo")]
public partial class AddFirmaFotosYTipoResiduo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('Solicitudes', 'TipoResiduo') IS NULL ALTER TABLE [Solicitudes] ADD [TipoResiduo] varchar(150) NULL;");
        migrationBuilder.Sql(@"
IF OBJECT_ID('SolicitudFotos', 'U') IS NULL
BEGIN
    CREATE TABLE [SolicitudFotos] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [IdSolicitud] int NOT NULL,
        [RutaArchivo] varchar(255) NOT NULL,
        [NombreArchivo] varchar(150) NULL,
        [FechaCreacion] datetime2 NOT NULL
    );
    CREATE INDEX [IX_SolicitudFotos_IdSolicitud_FechaCreacion] ON [SolicitudFotos] ([IdSolicitud], [FechaCreacion]);
END");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SolicitudFotos");
        migrationBuilder.DropColumn(name: "TipoResiduo", table: "Solicitudes");
    }
}
