using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260725120000_AddAlbaranesToSolicitudes")]
public partial class AddAlbaranesToSolicitudes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AlbaranPlanta",
            table: "Solicitudes",
            type: "varchar(20)",
            unicode: false,
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AlbaranSerieSage",
            table: "Solicitudes",
            type: "varchar(2)",
            unicode: false,
            maxLength: 2,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AlbaranNumeroSage",
            table: "Solicitudes",
            type: "varchar(10)",
            unicode: false,
            maxLength: 10,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AlbaranPlanta", table: "Solicitudes");
        migrationBuilder.DropColumn(name: "AlbaranSerieSage", table: "Solicitudes");
        migrationBuilder.DropColumn(name: "AlbaranNumeroSage", table: "Solicitudes");
    }
}
