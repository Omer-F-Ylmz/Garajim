using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AracDeger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BeyanDegeri",
                table: "KarnePaylasimlari",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AracDegerleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deger = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Kaynak = table.Column<int>(type: "int", nullable: false),
                    Not = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AracDegerleri", x => x.Id);
                    table.CheckConstraint("CK_AracDeger_Deger", "[Deger] > 0");
                    table.ForeignKey(
                        name: "FK_AracDegerleri_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AracDegerleri_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AracDegerleri_CompanyId",
                table: "AracDegerleri",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AracDegerleri_VehicleId_Tarih",
                table: "AracDegerleri",
                columns: new[] { "VehicleId", "Tarih" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AracDegerleri");

            migrationBuilder.DropColumn(
                name: "BeyanDegeri",
                table: "KarnePaylasimlari");
        }
    }
}
