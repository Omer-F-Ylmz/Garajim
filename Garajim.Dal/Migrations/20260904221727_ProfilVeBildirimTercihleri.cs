using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class ProfilVeBildirimTercihleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BildirimEvrak",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "BildirimHatirlatma",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "EpostaDenemeSayisi",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EpostaKodHash",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EpostaKodSonTarih",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SonEpostaGonderim",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YeniEposta",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BildirimEvrak",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BildirimHatirlatma",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EpostaDenemeSayisi",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EpostaKodHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EpostaKodSonTarih",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SonEpostaGonderim",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "YeniEposta",
                table: "Users");
        }
    }
}
