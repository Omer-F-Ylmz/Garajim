using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class DavetProgrami : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DavetEdenCompanyId",
                table: "Companies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DavetKodu",
                table: "Companies",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OdulGun",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_DavetKodu",
                table: "Companies",
                column: "DavetKodu",
                unique: true,
                filter: "[DavetKodu] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Companies_DavetKodu",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "DavetEdenCompanyId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "DavetKodu",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "OdulGun",
                table: "Companies");
        }
    }
}
