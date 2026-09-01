using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class PlanAracLimiti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AracLimiti",
                table: "Companies",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AracLimiti",
                table: "Companies");
        }
    }
}
