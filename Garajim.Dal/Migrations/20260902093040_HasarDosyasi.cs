using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class HasarDosyasi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasarGecmisi",
                table: "KarnePaylasimlari",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "HasarDosyalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    OlayTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tur = table.Column<int>(type: "int", nullable: false),
                    Konum = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OlayKm = table.Column<int>(type: "int", nullable: true),
                    TutanakTuru = table.Column<int>(type: "int", nullable: false),
                    KarsiTarafPlaka = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    KarsiTarafSigorta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    KarsiTarafPoliceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SigortaDosyaNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HasarBedeli = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    OlusturanUserId = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HasarDosyalari", x => x.Id);
                    table.CheckConstraint("CK_HasarDosyasi_Bedel", "[HasarBedeli] IS NULL OR [HasarBedeli] >= 0");
                    table.ForeignKey(
                        name: "FK_HasarDosyalari_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HasarDosyalari_Users_OlusturanUserId",
                        column: x => x.OlusturanUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HasarDosyalari_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HasarFotograflari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    HasarDosyasiId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    Etiket = table.Column<int>(type: "int", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HasarFotograflari", x => x.Id);
                    table.CheckConstraint("CK_HasarFoto_Sira", "[Sira] > 0");
                    table.ForeignKey(
                        name: "FK_HasarFotograflari_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HasarFotograflari_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HasarFotograflari_HasarDosyalari_HasarDosyasiId",
                        column: x => x.HasarDosyasiId,
                        principalTable: "HasarDosyalari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HasarDosyalari_CompanyId",
                table: "HasarDosyalari",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_HasarDosyalari_OlusturanUserId",
                table: "HasarDosyalari",
                column: "OlusturanUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HasarDosyalari_VehicleId_OlayTarihi",
                table: "HasarDosyalari",
                columns: new[] { "VehicleId", "OlayTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_HasarFotograflari_CompanyId",
                table: "HasarFotograflari",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_HasarFotograflari_DocumentId",
                table: "HasarFotograflari",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HasarFotograflari_HasarDosyasiId_Sira",
                table: "HasarFotograflari",
                columns: new[] { "HasarDosyasiId", "Sira" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HasarFotograflari");

            migrationBuilder.DropTable(
                name: "HasarDosyalari");

            migrationBuilder.DropColumn(
                name: "HasarGecmisi",
                table: "KarnePaylasimlari");
        }
    }
}
