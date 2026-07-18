using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST1Savall.API.Data;

#nullable disable

namespace ST1Savall.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260717140000_AddBreakHoursToTurnos")]
public partial class AddBreakHoursToTurnos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('Turnos', 'HoraInicioBreak') IS NULL ALTER TABLE [Turnos] ADD [HoraInicioBreak] time NULL;");
        migrationBuilder.Sql("IF COL_LENGTH('Turnos', 'HoraFinBreak') IS NULL ALTER TABLE [Turnos] ADD [HoraFinBreak] time NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "HoraInicioBreak", table: "Turnos");
        migrationBuilder.DropColumn(name: "HoraFinBreak", table: "Turnos");
    }
}
