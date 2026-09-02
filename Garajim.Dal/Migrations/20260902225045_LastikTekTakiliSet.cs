using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class LastikTekTakiliSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LastikSetleri_VehicleId",
                table: "LastikSetleri",
                column: "VehicleId",
                unique: true,
                filter: "[Takili] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LastikSetleri_VehicleId",
                table: "LastikSetleri");
        }
    }
}
