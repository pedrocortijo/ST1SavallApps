using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20261001173000_AddMovimientosEntregaObra")]
public partial class AddMovimientosEntregaObra : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            IF OBJECT_ID(N'MovimientosEntregaObra', N'U') IS NULL
            BEGIN
                CREATE TABLE MovimientosEntregaObra (
                    IdMovimientoEntregaObra INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientosEntregaObra PRIMARY KEY,
                    ObraCodigo CHAR(5) NOT NULL, Fecha DATETIME2 NOT NULL, Tipo NVARCHAR(20) NOT NULL,
                    Debe DECIMAL(18,2) NOT NULL CONSTRAINT DF_MovimientosEntregaObra_Debe DEFAULT 0,
                    Haber DECIMAL(18,2) NOT NULL CONSTRAINT DF_MovimientosEntregaObra_Haber DEFAULT 0,
                    Concepto NVARCHAR(250) NOT NULL, EmpresaAlbaran NVARCHAR(2) NULL, NumeroAlbaran NVARCHAR(12) NULL,
                    SerieAlbaran NVARCHAR(2) NULL, Activo BIT NOT NULL CONSTRAINT DF_MovimientosEntregaObra_Activo DEFAULT 1, Anulado BIT NOT NULL CONSTRAINT DF_MovimientosEntregaObra_Anulado DEFAULT 0,
                    CreadoEn DATETIME2 NOT NULL, CreadoPor NVARCHAR(256) NULL, ModificadoEn DATETIME2 NULL, ModificadoPor NVARCHAR(256) NULL
                );
                CREATE INDEX IX_MovimientosEntregaObra_Obra_Fecha ON MovimientosEntregaObra(ObraCodigo, Fecha, IdMovimientoEntregaObra);
                CREATE INDEX IX_MovimientosEntregaObra_Albaran ON MovimientosEntregaObra(EmpresaAlbaran, NumeroAlbaran, SerieAlbaran);
            END;
            IF COL_LENGTH('MovimientosEntregaObra', 'Activo') IS NULL
                ALTER TABLE MovimientosEntregaObra ADD Activo BIT NOT NULL CONSTRAINT DF_MovimientosEntregaObra_Activo DEFAULT 1;");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("MovimientosEntregaObra");
}
