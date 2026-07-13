using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST1Savall.API.Migrations
{
    /// <inheritdoc />
    public partial class AddParametros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Parametros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Empresa = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReceiverEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SmtpServer = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    SmtpUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SmtpPassword = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SslSmtpType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parametros", x => x.Id);
                });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Parametros");
        }
    }
}
