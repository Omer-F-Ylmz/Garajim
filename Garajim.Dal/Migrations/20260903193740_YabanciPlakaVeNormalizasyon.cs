using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class YabanciPlakaVeNormalizasyon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "YabanciPlaka",
                table: "Vehicles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
UPDATE v
SET v.Plate = k.Yeni
FROM Vehicles v
CROSS APPLY (SELECT UPPER(REPLACE(REPLACE(v.Plate, ' ', ''), '-', '')) AS Yeni) k
WHERE v.Plate COLLATE Latin1_General_BIN2 <> k.Yeni COLLATE Latin1_General_BIN2
  AND NOT EXISTS (
        SELECT 1 FROM Vehicles d
        WHERE d.CompanyId = v.CompanyId AND d.Id <> v.Id AND d.Plate = k.Yeni);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YabanciPlaka",
                table: "Vehicles");
        }
    }
}
