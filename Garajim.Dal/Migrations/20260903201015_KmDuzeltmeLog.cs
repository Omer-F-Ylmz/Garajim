using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class KmDuzeltmeLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KmDuzeltmeLoglari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EskiKm = table.Column<int>(type: "int", nullable: false),
                    YeniKm = table.Column<int>(type: "int", nullable: false),
                    Neden = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KmDuzeltmeLoglari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KmDuzeltmeLoglari_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KmDuzeltmeLoglari_CompanyId",
                table: "KmDuzeltmeLoglari",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_KmDuzeltmeLoglari_VehicleId_Tarih",
                table: "KmDuzeltmeLoglari",
                columns: new[] { "VehicleId", "Tarih" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KmDuzeltmeLoglari");
        }
    }
}
