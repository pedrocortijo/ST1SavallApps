using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260813150000_AddPreciosEspecialesObra")]
public partial class AddPreciosEspecialesObra : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PreciosEspecialesObra",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ClienteSage = table.Column<string>(type: "char(8)", maxLength: 8, nullable: false),
                ObraSage = table.Column<string>(type: "char(5)", maxLength: 5, nullable: false),
                ArticuloSage = table.Column<string>(type: "char(20)", maxLength: 20, nullable: false),
                Precio = table.Column<decimal>(type: "decimal(15,6)", nullable: false),
                VigenteDesde = table.Column<DateTime>(type: "datetime2", nullable: true),
                VigenteHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                Observaciones = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_PreciosEspecialesObra", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_PreciosEspecialesObra_ClienteSage_ObraSage_ArticuloSage", table: "PreciosEspecialesObra", columns: new[] { "ClienteSage", "ObraSage", "ArticuloSage" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "PreciosEspecialesObra");
}
