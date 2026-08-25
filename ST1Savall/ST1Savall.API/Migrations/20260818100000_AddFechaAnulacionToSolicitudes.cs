using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260818100000_AddFechaAnulacionToSolicitudes")]
public partial class AddFechaAnulacionToSolicitudes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "FechaAnulacion",
            table: "Solicitudes",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "FechaAnulacion",
            table: "Solicitudes");
    }
}
