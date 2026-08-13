using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations;

[Migration("20260813140000_AddParametrosAlbaranes")]
public partial class AddParametrosAlbaranes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('Parametros', 'SerieAlbaranes') IS NULL ALTER TABLE [Parametros] ADD [SerieAlbaranes] char(2) NOT NULL CONSTRAINT [DF_Parametros_SerieAlbaranes] DEFAULT '';");
        migrationBuilder.Sql("IF COL_LENGTH('Parametros', 'AlmacenAlbaranes') IS NULL ALTER TABLE [Parametros] ADD [AlmacenAlbaranes] char(3) NOT NULL CONSTRAINT [DF_Parametros_AlmacenAlbaranes] DEFAULT '';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SerieAlbaranes", table: "Parametros");
        migrationBuilder.DropColumn(name: "AlmacenAlbaranes", table: "Parametros");
    }
}
