using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

public partial class AddEmpresaAlbaranesToParametros : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('Parametros', 'EmpresaAlbaranes') IS NULL ALTER TABLE [Parametros] ADD [EmpresaAlbaranes] char(2) NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "EmpresaAlbaranes", table: "Parametros");
    }
}
