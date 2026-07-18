using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260718150000_AddNotificacionInicioVisualizada")]
public partial class AddNotificacionInicioVisualizada : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "NotificacionInicioVisualizada",
            table: "Solicitudes",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "NotificacionInicioVisualizada",
            table: "Solicitudes");
    }
}
