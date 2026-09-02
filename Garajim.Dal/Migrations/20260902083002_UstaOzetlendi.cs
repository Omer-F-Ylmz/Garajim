using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class UstaOzetlendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ozetlendi",
                table: "UstaMesajlari",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_UstaMesajlari_Ozetlendi",
                table: "UstaMesajlari",
                column: "Ozetlendi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UstaMesajlari_Ozetlendi",
                table: "UstaMesajlari");

            migrationBuilder.DropColumn(
                name: "Ozetlendi",
                table: "UstaMesajlari");
        }
    }
}
