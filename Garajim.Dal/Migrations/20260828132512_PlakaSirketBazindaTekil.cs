using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class PlakaSirketBazindaTekil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM [Vehicles] GROUP BY [CompanyId], [Plate] HAVING COUNT(*) > 1)
BEGIN
    DECLARE @cakisan NVARCHAR(2000);
    SELECT @cakisan = STUFF((
        SELECT TOP 20 N', ' + CAST(c.[CompanyId] AS NVARCHAR(20)) + N'/' + c.[Plate]
        FROM (
            SELECT [CompanyId], [Plate]
            FROM [Vehicles]
            GROUP BY [CompanyId], [Plate]
            HAVING COUNT(*) > 1
        ) AS c
        ORDER BY c.[CompanyId], c.[Plate]
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(2000)'), 1, 2, N'');

    DECLARE @mesaj NVARCHAR(4000) = N'Plaka tekilleştirmesi durduruldu: aynı şirkette birden fazla araç aynı plakaya sahip. Çakışan şirket/plaka çiftleri: '
        + ISNULL(@cakisan, N'(listelenemedi)')
        + N'. Bu kayıtlar elle düzeltilmeden geçiş uygulanamaz.';

    THROW 51000, @mesaj, 1;
END
");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_UserId_Plate",
                table: "Vehicles");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CompanyId_Plate",
                table: "Vehicles",
                columns: new[] { "CompanyId", "Plate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_UserId",
                table: "Vehicles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_CompanyId_Plate",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_UserId",
                table: "Vehicles");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_UserId_Plate",
                table: "Vehicles",
                columns: new[] { "UserId", "Plate" },
                unique: true);
        }
    }
}
