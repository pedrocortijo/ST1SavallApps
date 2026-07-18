using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST1Savall.API.Data;

#nullable disable

namespace ST1Savall.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260713062000_AddAvisosYPathImagenesToParametros")]
public partial class AddAvisosYPathImagenesToParametros : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('Parametros', 'AvisoTiempoContenedor') IS NULL ALTER TABLE [Parametros] ADD [AvisoTiempoContenedor] int NOT NULL CONSTRAINT [DF_Parametros_AvisoTiempoContenedor] DEFAULT 0;");
        migrationBuilder.Sql("IF COL_LENGTH('Parametros', 'AvisoTiempoServicio') IS NULL ALTER TABLE [Parametros] ADD [AvisoTiempoServicio] int NOT NULL CONSTRAINT [DF_Parametros_AvisoTiempoServicio] DEFAULT 0;");
        migrationBuilder.Sql("IF COL_LENGTH('Parametros', 'PathImagenes') IS NULL ALTER TABLE [Parametros] ADD [PathImagenes] varchar(255) NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AvisoTiempoContenedor", table: "Parametros");
        migrationBuilder.DropColumn(name: "AvisoTiempoServicio", table: "Parametros");
        migrationBuilder.DropColumn(name: "PathImagenes", table: "Parametros");
    }
}
