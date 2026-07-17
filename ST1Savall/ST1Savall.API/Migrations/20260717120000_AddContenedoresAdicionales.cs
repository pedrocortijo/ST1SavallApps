using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

public partial class AddContenedoresAdicionales : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CodigoAmbosEntrega",
            table: "Solicitudes",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CodigoAmbosRecogida",
            table: "Solicitudes",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<bool>(name: "Recoger1", table: "Tareas", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "Recoger2", table: "Tareas", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "Entrega1", table: "Tareas", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "Entrega2", table: "Tareas", type: "bit", nullable: false, defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CodigoAmbosEntrega", table: "Solicitudes");
        migrationBuilder.DropColumn(name: "CodigoAmbosRecogida", table: "Solicitudes");
        migrationBuilder.DropColumn(name: "Recoger1", table: "Tareas");
        migrationBuilder.DropColumn(name: "Recoger2", table: "Tareas");
        migrationBuilder.DropColumn(name: "Entrega1", table: "Tareas");
        migrationBuilder.DropColumn(name: "Entrega2", table: "Tareas");
    }
}
