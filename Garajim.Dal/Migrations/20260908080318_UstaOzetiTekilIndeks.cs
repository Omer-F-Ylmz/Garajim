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
            migrationBuilder.CreateIndex(
                name: "UX_UstaCozumOzeti_DogalAnahtar",
                table: "UstaCozumOzetleri",
                columns: new[] { "Marka", "Model", "Motor", "BelirtiKategori", "ParcaTuru" },
                unique: true,
                filter: "[Motor] IS NOT NULL");
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
