using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260813150000_AddPreciosEspecialesCabeceraDetalles")]
public partial class AddPreciosEspecialesCabeceraDetalles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE [PreciosEspecialesCabecera] ([IdPrecioEspecialCabecera] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [ObraSage] char(5) NOT NULL, [VigenteDesde] datetime2 NULL, [VigenteHasta] datetime2 NULL, [Observaciones] nvarchar(250) NULL, CONSTRAINT [UQ_PreciosEspecialesCabecera] UNIQUE ([ObraSage]));
            CREATE TABLE [PreciosEspecialesDetalles] ([IdPrecioEspecialDetalle] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [IdPrecioEspecialCabecera] int NOT NULL, [ArticuloSage] char(20) NOT NULL, [Precio] decimal(15,6) NOT NULL, CONSTRAINT [FK_PreciosEspecialesDetalles_Cabecera] FOREIGN KEY ([IdPrecioEspecialCabecera]) REFERENCES [PreciosEspecialesCabecera]([IdPrecioEspecialCabecera]) ON DELETE CASCADE, CONSTRAINT [UQ_PreciosEspecialesDetalles] UNIQUE ([IdPrecioEspecialCabecera], [ArticuloSage]));
            IF OBJECT_ID(N'PreciosEspecialesObra', N'U') IS NOT NULL BEGIN
                INSERT INTO PreciosEspecialesCabecera (ObraSage, VigenteDesde, VigenteHasta, Observaciones) SELECT ObraSage, MAX(VigenteDesde), MAX(VigenteHasta), MAX(Observaciones) FROM PreciosEspecialesObra GROUP BY ObraSage;
                INSERT INTO PreciosEspecialesDetalles (IdPrecioEspecialCabecera, ArticuloSage, Precio) SELECT c.IdPrecioEspecialCabecera, o.ArticuloSage, o.Precio FROM PreciosEspecialesObra o INNER JOIN PreciosEspecialesCabecera c ON c.ObraSage=o.ObraSage;
            END;");
    }
    protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.DropTable("PreciosEspecialesDetalles"); migrationBuilder.DropTable("PreciosEspecialesCabecera"); }
}
