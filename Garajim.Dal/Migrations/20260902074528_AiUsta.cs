using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AiUsta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Motor",
                table: "Vehicles",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Vites",
                table: "Vehicles",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UstaCozumOzetleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Marka = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Motor = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    BelirtiKategori = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ParcaTuru = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Sayi = table.Column<int>(type: "int", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UstaCozumOzetleri", x => x.Id);
                    table.CheckConstraint("CK_UstaCozumOzeti_Sayi", "[Sayi] > 0");
                });

            migrationBuilder.CreateTable(
                name: "UstaOnaylari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MetinSurumu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    KabulTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UstaOnaylari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UstaOnaylari_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UstaOnaylari_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UstaSohbetleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UstaSohbetleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UstaSohbetleri_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UstaSohbetleri_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UstaSohbetleri_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UstaMesajlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    SohbetId = table.Column<int>(type: "int", nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    Metin = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    YapiliYanit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KirmiziCizgi = table.Column<bool>(type: "bit", nullable: false),
                    TokenGiris = table.Column<int>(type: "int", nullable: false),
                    TokenCikis = table.Column<int>(type: "int", nullable: false),
                    SureMs = table.Column<int>(type: "int", nullable: false),
                    GeriBildirim = table.Column<int>(type: "int", nullable: false),
                    CozumBakimId = table.Column<int>(type: "int", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UstaMesajlari", x => x.Id);
                    table.CheckConstraint("CK_UstaMesaj_Token", "[TokenGiris] >= 0 AND [TokenCikis] >= 0 AND [SureMs] >= 0");
                    table.ForeignKey(
                        name: "FK_UstaMesajlari_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UstaMesajlari_UstaSohbetleri_SohbetId",
                        column: x => x.SohbetId,
                        principalTable: "UstaSohbetleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UstaCozumOzetleri_Marka_Model_BelirtiKategori_ParcaTuru",
                table: "UstaCozumOzetleri",
                columns: new[] { "Marka", "Model", "BelirtiKategori", "ParcaTuru" });

            migrationBuilder.CreateIndex(
                name: "IX_UstaMesajlari_CompanyId",
                table: "UstaMesajlari",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UstaMesajlari_SohbetId_Id",
                table: "UstaMesajlari",
                columns: new[] { "SohbetId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_UstaOnaylari_CompanyId",
                table: "UstaOnaylari",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UstaOnaylari_UserId_MetinSurumu",
                table: "UstaOnaylari",
                columns: new[] { "UserId", "MetinSurumu" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UstaSohbetleri_CompanyId",
                table: "UstaSohbetleri",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UstaSohbetleri_UserId",
                table: "UstaSohbetleri",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UstaSohbetleri_VehicleId_OlusturmaTarihi",
                table: "UstaSohbetleri",
                columns: new[] { "VehicleId", "OlusturmaTarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UstaCozumOzetleri");

            migrationBuilder.DropTable(
                name: "UstaMesajlari");

            migrationBuilder.DropTable(
                name: "UstaOnaylari");

            migrationBuilder.DropTable(
                name: "UstaSohbetleri");

            migrationBuilder.DropColumn(
                name: "Motor",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Vites",
                table: "Vehicles");
        }
    }
}
