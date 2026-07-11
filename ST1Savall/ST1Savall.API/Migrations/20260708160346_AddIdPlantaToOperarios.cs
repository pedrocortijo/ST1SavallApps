using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIdPlantaToOperarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdPlanta",
                table: "Operarios",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Operarios_IdPlanta",
                table: "Operarios",
                column: "IdPlanta");

            migrationBuilder.AddForeignKey(
                name: "FK_Operarios_Plantas_IdPlanta",
                table: "Operarios",
                column: "IdPlanta",
                principalTable: "Plantas",
                principalColumn: "IdPlanta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Operarios_Plantas_IdPlanta",
                table: "Operarios");

            migrationBuilder.DropIndex(
                name: "IX_Operarios_IdPlanta",
                table: "Operarios");

            migrationBuilder.DropColumn(
                name: "IdPlanta",
                table: "Operarios");
        }
    }
}
