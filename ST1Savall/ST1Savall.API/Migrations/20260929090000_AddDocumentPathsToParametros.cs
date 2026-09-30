using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260929090000_AddDocumentPathsToParametros")]
public partial class AddDocumentPathsToParametros : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PathDocumentosOVP",
            table: "Parametros",
            type: "varchar(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PathPdfFacturas",
            table: "Parametros",
            type: "varchar(255)",
            maxLength: 255,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PathDocumentosOVP", table: "Parametros");
        migrationBuilder.DropColumn(name: "PathPdfFacturas", table: "Parametros");
    }
}
