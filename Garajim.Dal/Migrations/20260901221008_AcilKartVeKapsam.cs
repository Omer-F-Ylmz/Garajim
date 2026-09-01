using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AcilKartVeKapsam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcilKisiAd",
                table: "Vehicles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcilKisiTelefon",
                table: "Vehicles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcilNot",
                table: "Vehicles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcilKart",
                table: "KarnePaylasimlari",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcilKisiAd",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "AcilKisiTelefon",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "AcilNot",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "AcilKart",
                table: "KarnePaylasimlari");
        }
    }
}
