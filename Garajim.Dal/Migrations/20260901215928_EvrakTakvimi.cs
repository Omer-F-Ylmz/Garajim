using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class EvrakTakvimi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IlkTescilTarihi",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KullanimTuru",
                table: "Vehicles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EvrakKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    EvrakTuru = table.Column<int>(type: "int", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Saglayici = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PoliceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Not = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    LastNotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvrakKayitlari", x => x.Id);
                    table.CheckConstraint("CK_EvrakKaydi_TekSahip", "([VehicleId] IS NOT NULL AND [UserId] IS NULL) OR ([VehicleId] IS NULL AND [UserId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_EvrakKayitlari_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvrakKayitlari_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvrakKayitlari_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvrakKayitlari_CompanyId_BitisTarihi",
                table: "EvrakKayitlari",
                columns: new[] { "CompanyId", "BitisTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_EvrakKayitlari_UserId_Aktif",
                table: "EvrakKayitlari",
                columns: new[] { "UserId", "Aktif" });

            migrationBuilder.CreateIndex(
                name: "IX_EvrakKayitlari_VehicleId_Aktif",
                table: "EvrakKayitlari",
                columns: new[] { "VehicleId", "Aktif" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvrakKayitlari");

            migrationBuilder.DropColumn(
                name: "IlkTescilTarihi",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "KullanimTuru",
                table: "Vehicles");
        }
    }
}
