using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AiTokenVeFisTokenlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TokenCikis",
                table: "ReceiptDrafts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TokenGiris",
                table: "ReceiptDrafts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AiTokenSayaclari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Yil = table.Column<int>(type: "int", nullable: false),
                    Ay = table.Column<int>(type: "int", nullable: false),
                    TokenGiris = table.Column<long>(type: "bigint", nullable: false),
                    TokenCikis = table.Column<long>(type: "bigint", nullable: false),
                    BildirimGonderildi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiTokenSayaclari", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiTokenSayaclari_Yil_Ay",
                table: "AiTokenSayaclari",
                columns: new[] { "Yil", "Ay" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiTokenSayaclari");

            migrationBuilder.DropColumn(
                name: "TokenCikis",
                table: "ReceiptDrafts");

            migrationBuilder.DropColumn(
                name: "TokenGiris",
                table: "ReceiptDrafts");
        }
    }
}
