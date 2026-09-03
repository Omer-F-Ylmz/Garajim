using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class HesapSilme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SilmeDenemeSayisi",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SilmeKodHash",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SilmeKodSonTarih",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SonSilmeGonderim",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SilinmePlanlanan",
                table: "Companies",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SilmeDenemeSayisi",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SilmeKodHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SilmeKodSonTarih",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SonSilmeGonderim",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SilinmePlanlanan",
                table: "Companies");
        }
    }
}
