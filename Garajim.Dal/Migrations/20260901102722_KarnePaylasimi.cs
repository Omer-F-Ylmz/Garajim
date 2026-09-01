using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class KarnePaylasimi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KarnePaylasimlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BakimGecmisi = table.Column<bool>(type: "bit", nullable: false),
                    ParcaHafizasi = table.Column<bool>(type: "bit", nullable: false),
                    YakitOzeti = table.Column<bool>(type: "bit", nullable: false),
                    Belgeler = table.Column<bool>(type: "bit", nullable: false),
                    PlakaGoster = table.Column<bool>(type: "bit", nullable: false),
                    TutarGoster = table.Column<bool>(type: "bit", nullable: false),
                    SonKullanma = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    GoruntulenmeSayisi = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KarnePaylasimlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KarnePaylasimlari_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KarnePaylasimlari_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KarnePaylasimlari_CompanyId",
                table: "KarnePaylasimlari",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_KarnePaylasimlari_TokenHash",
                table: "KarnePaylasimlari",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KarnePaylasimlari_VehicleId_Aktif",
                table: "KarnePaylasimlari",
                columns: new[] { "VehicleId", "Aktif" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KarnePaylasimlari");
        }
    }
}
