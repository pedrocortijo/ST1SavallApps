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
        migrationBuilder.AddColumn<int>(
            name: "AvisoTiempoContenedor",
            table: "Parametros",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "AvisoTiempoServicio",
            table: "Parametros",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "PathImagenes",
            table: "Parametros",
            type: "varchar(255)",
            maxLength: 255,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AvisoTiempoContenedor", table: "Parametros");
        migrationBuilder.DropColumn(name: "AvisoTiempoServicio", table: "Parametros");
        migrationBuilder.DropColumn(name: "PathImagenes", table: "Parametros");
    }
}
