using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class FisTaslagi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceiptDrafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    YukleyenUserId = table.Column<int>(type: "int", nullable: false),
                    DosyaYolu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrijinalAd = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    IcerikTipi = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BoyutBayt = table.Column<long>(type: "bigint", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToplamTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    KdvTutari = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Litre = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    BirimFiyat = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    Plaka = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Km = table.Column<int>(type: "int", nullable: true),
                    TahminiTur = table.Column<int>(type: "int", nullable: false),
                    GuvenSkoru = table.Column<double>(type: "float", nullable: false),
                    DuzeltilenAlanlar = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptDrafts_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptDrafts_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptDrafts_CompanyId",
                table: "ReceiptDrafts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptDrafts_CompanyId_Durum",
                table: "ReceiptDrafts",
                columns: new[] { "CompanyId", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptDrafts_OlusturmaTarihi",
                table: "ReceiptDrafts",
                column: "OlusturmaTarihi");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptDrafts_VehicleId",
                table: "ReceiptDrafts",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptDrafts");
        }
    }
}
