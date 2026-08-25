using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

public partial class AddCreaAlbaranToTareas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('Tareas', 'CreaAlbaran') IS NULL ALTER TABLE [Tareas] ADD [CreaAlbaran] bit NOT NULL CONSTRAINT [DF_Tareas_CreaAlbaran] DEFAULT (0);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CreaAlbaran", table: "Tareas");
    }
}
