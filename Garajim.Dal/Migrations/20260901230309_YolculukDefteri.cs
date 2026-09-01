using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class YolculukDefteri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YolculukKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaslangicKm = table.Column<int>(type: "int", nullable: false),
                    BitisKm = table.Column<int>(type: "int", nullable: false),
                    MesafeKm = table.Column<int>(type: "int", nullable: false),
                    Amac = table.Column<int>(type: "int", nullable: false),
                    Nereden = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Nereye = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Not = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YolculukKayitlari", x => x.Id);
                    table.CheckConstraint("CK_YolculukKaydi_Mesafe", "[BitisKm] > [BaslangicKm] AND [MesafeKm] = [BitisKm] - [BaslangicKm]");
                    table.ForeignKey(
                        name: "FK_YolculukKayitlari_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YolculukKayitlari_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YolculukKayitlari_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YolculukKayitlari_CompanyId",
                table: "YolculukKayitlari",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_YolculukKayitlari_UserId",
                table: "YolculukKayitlari",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_YolculukKayitlari_VehicleId_Tarih",
                table: "YolculukKayitlari",
                columns: new[] { "VehicleId", "Tarih" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YolculukKayitlari");
        }
    }
}
