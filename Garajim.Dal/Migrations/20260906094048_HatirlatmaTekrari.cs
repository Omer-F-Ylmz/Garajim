using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class HatirlatmaTekrari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TekrarAy",
                table: "Reminders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TekrarKm",
                table: "Reminders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TekrardanUretenId",
                table: "Reminders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_TekrardanUretenId",
                table: "Reminders",
                column: "TekrardanUretenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reminders_TekrardanUretenId",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "TekrarAy",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "TekrarKm",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "TekrardanUretenId",
                table: "Reminders");
        }
    }
}
