using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class LastikSetleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LastikSetleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Mevsim = table.Column<int>(type: "int", nullable: false),
                    Marka = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Ebat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisDerinligiMm = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true),
                    TakilmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TakilmaKm = table.Column<int>(type: "int", nullable: false),
                    SokulmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SokulmeKm = table.Column<int>(type: "int", nullable: true),
                    ToplamKm = table.Column<int>(type: "int", nullable: false),
                    Takili = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LastikSetleri", x => x.Id);
                    table.CheckConstraint("CK_LastikSeti_Mesafe", "([SokulmeKm] IS NULL AND [Takili] = 1 AND [ToplamKm] = 0) OR ([SokulmeKm] IS NOT NULL AND [Takili] = 0 AND [SokulmeKm] >= [TakilmaKm] AND [ToplamKm] = [SokulmeKm] - [TakilmaKm])");
                    table.ForeignKey(
                        name: "FK_LastikSetleri_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LastikSetleri_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LastikSetleri_CompanyId",
                table: "LastikSetleri",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_LastikSetleri_VehicleId_Takili",
                table: "LastikSetleri",
                columns: new[] { "VehicleId", "Takili" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LastikSetleri");
        }
    }
}
