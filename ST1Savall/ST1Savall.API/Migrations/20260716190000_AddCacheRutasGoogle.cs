using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST1Savall.API.Data;

#nullable disable

namespace ST1Savall.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260716190000_AddCacheRutasGoogle")]
public partial class AddCacheRutasGoogle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RutasCache",
            columns: table => new
            {
                IdRutaCache = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ClaveRuta = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                LatitudOrigen = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                LongitudOrigen = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                LatitudDestino = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                LongitudDestino = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                ModoViaje = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                PreferenciaRuta = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                DistanciaMetros = table.Column<int>(type: "int", nullable: false),
                DuracionSegundos = table.Column<int>(type: "int", nullable: false),
                FechaCalculoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaExpiracionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UltimoUsoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                NumeroUsos = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_RutasCache", x => x.IdRutaCache));

        migrationBuilder.CreateIndex(
            name: "IX_RutasCache_ClaveRuta",
            table: "RutasCache",
            column: "ClaveRuta",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RutasCache_FechaExpiracionUtc",
            table: "RutasCache",
            column: "FechaExpiracionUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RutasCache");
    }
}
