using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations
{
    /// <inheritdoc />
    public partial class SyncApplicationModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF COL_LENGTH('Contenedores', 'IdPlanta') IS NULL ALTER TABLE [Contenedores] ADD [IdPlanta] int NOT NULL CONSTRAINT [DF_Contenedores_IdPlanta] DEFAULT 0;");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Contenedores_IdPlanta' AND object_id = OBJECT_ID('Contenedores')) CREATE INDEX [IX_Contenedores_IdPlanta] ON [Contenedores] ([IdPlanta]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contenedores_Plantas_IdPlanta') ALTER TABLE [Contenedores] ADD CONSTRAINT [FK_Contenedores_Plantas_IdPlanta] FOREIGN KEY ([IdPlanta]) REFERENCES [Plantas] ([IdPlanta]) ON DELETE CASCADE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contenedores_Plantas_IdPlanta",
                table: "Contenedores");

            migrationBuilder.DropIndex(
                name: "IX_Contenedores_IdPlanta",
                table: "Contenedores");

            migrationBuilder.DropColumn(
                name: "IdPlanta",
                table: "Contenedores");
        }
    }
}
