using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class EmailDogrulama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DogrulamaDenemeSayisi",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DogrulamaKodHash",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DogrulamaKodSonTarih",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailDogrulandi",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SonKodGonderim",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE Users SET EmailDogrulandi = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DogrulamaDenemeSayisi",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DogrulamaKodHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DogrulamaKodSonTarih",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailDogrulandi",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SonKodGonderim",
                table: "Users");
        }
    }
}
