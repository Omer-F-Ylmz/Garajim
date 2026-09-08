using Garajim.Dal.Sorgular;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class HasarFotoSiraTekil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(YinelenenTemizligi.HasarFotoSiraGeciciOlustur);
            migrationBuilder.Sql(YinelenenTemizligi.HasarFotoSiraGeciciDoldur);
            migrationBuilder.Sql(YinelenenTemizligi.HasarFotoSiraUygula);
            migrationBuilder.Sql(YinelenenTemizligi.HasarFotoSiraGeciciSil);

            migrationBuilder.CreateIndex(
                name: "UX_HasarFoto_DosyaSira",
                table: "HasarFotograflari",
                columns: new[] { "HasarDosyasiId", "Sira" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_HasarFoto_DosyaSira",
                table: "HasarFotograflari");
        }
    }
}
