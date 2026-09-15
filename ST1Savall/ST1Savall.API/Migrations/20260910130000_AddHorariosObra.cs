using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace ST1Savall.API.Migrations;
public partial class AddHorariosObra : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(name: "HorariosObra", columns: table => new { CODIGO = table.Column<string>(type: "char(5)", nullable: false), HORAINICIO = table.Column<TimeSpan>(type: "time", nullable: false), HORAFIN = table.Column<TimeSpan>(type: "time", nullable: false) }, constraints: table => table.PrimaryKey("PK_HorariosObra", x => x.CODIGO));
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "HorariosObra");
}