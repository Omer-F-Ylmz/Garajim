using Garajim.Dal.Sorgular;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class UstaOzetiTekilIndeks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(YinelenenTemizligi.UstaOzetiTopla);
            migrationBuilder.Sql(YinelenenTemizligi.UstaOzetiKopyalariSil);

            migrationBuilder.CreateIndex(
                name: "UX_UstaCozumOzeti_DogalAnahtar",
                table: "UstaCozumOzetleri",
                columns: new[] { "Marka", "Model", "Motor", "BelirtiKategori", "ParcaTuru" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_UstaCozumOzeti_DogalAnahtar",
                table: "UstaCozumOzetleri");
        }
    }
}
