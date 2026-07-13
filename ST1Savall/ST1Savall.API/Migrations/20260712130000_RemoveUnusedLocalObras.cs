using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST1Savall.API.Data;

#nullable disable

namespace ST1Savall.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260712130000_RemoveUnusedLocalObras")]
public partial class RemoveUnusedLocalObras : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Obras");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Obras",
            columns: table => new
            {
                IdObra = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Anyo = table.Column<int>(type: "int", nullable: true),
                Cliente = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                CodigoPostal = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                CodigoPostalCliente = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                Contador = table.Column<int>(type: "int", nullable: true),
                Descripcion = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                DireccionCliente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                EmailCliente = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Encargado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Finalizada = table.Column<bool>(type: "bit", nullable: true),
                IdEmpresa = table.Column<int>(type: "int", nullable: true),
                Movil = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                Nima = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                NombreCliente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Poblacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                PoblacionCliente = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Provincia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ResponsableCliente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                TelefonoCliente = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                TelefonoContactoCliente = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                Ubicacion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Visible = table.Column<bool>(type: "bit", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Obras", x => x.IdObra));
    }
}
