using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class SifreSifirlama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SifirlamaDenemeSayisi",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SifirlamaKodHash",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SifirlamaKodSonTarih",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SifreDegisimTarihi",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SonSifirlamaGonderim",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SifirlamaDenemeSayisi",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SifirlamaKodHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SifirlamaKodSonTarih",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SifreDegisimTarihi",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SonSifirlamaGonderim",
                table: "Users");
        }
    }
}
