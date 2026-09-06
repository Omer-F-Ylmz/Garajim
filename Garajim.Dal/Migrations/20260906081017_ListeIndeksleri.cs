using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class ListeIndeksleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EvrakKayitlari_VehicleId_BitisTarihi",
                table: "EvrakKayitlari",
                columns: new[] { "VehicleId", "BitisTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_MaintenanceRecordId",
                table: "Documents",
                column: "MaintenanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_VehicleId_CreatedAt",
                table: "Documents",
                columns: new[] { "VehicleId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EvrakKayitlari_VehicleId_BitisTarihi",
                table: "EvrakKayitlari");

            migrationBuilder.DropIndex(
                name: "IX_Documents_MaintenanceRecordId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_VehicleId_CreatedAt",
                table: "Documents");
        }
    }
}
