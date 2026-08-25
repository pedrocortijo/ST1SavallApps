using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

public partial class AddUsuarioAlbaranesToParametros : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('Parametros', 'UsuarioAlbaranes') IS NULL ALTER TABLE [Parametros] ADD [UsuarioAlbaranes] char(25) NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "UsuarioAlbaranes", table: "Parametros");
    }
}
