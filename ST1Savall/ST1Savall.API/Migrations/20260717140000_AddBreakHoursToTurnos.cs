using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

public partial class AddBreakHoursToTurnos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<TimeSpan>(name: "HoraInicioBreak", table: "Turnos", type: "time", nullable: true);
        migrationBuilder.AddColumn<TimeSpan>(name: "HoraFinBreak", table: "Turnos", type: "time", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "HoraInicioBreak", table: "Turnos");
        migrationBuilder.DropColumn(name: "HoraFinBreak", table: "Turnos");
    }
}
