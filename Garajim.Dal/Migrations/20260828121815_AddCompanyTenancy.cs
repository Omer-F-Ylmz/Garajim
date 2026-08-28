using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyTenancy : Migration
    {
        /// <inheritdoc />
        private static readonly string[] TenantTables =
        {
            "Users", "Vehicles", "MaintenanceRecords", "FuelRecords", "ExpenseRecords", "Reminders"
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PlanType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            foreach (var tablo in TenantTables)
            {
                migrationBuilder.AddColumn<int>(
                    name: "CompanyId",
                    table: tablo,
                    type: "int",
                    nullable: true);
            }

            migrationBuilder.Sql(@"
DECLARE @eslesme TABLE (UserId INT, CompanyId INT);

MERGE INTO Companies AS hedef
USING (
    SELECT Id AS UserId,
           CASE WHEN Email = 'demo@garajim.app' THEN N'Garajım Demo' ELSE LEFT(FullName, 150) END AS Ad
    FROM Users
) AS kaynak
ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT (Name, PlanType, CreatedAt) VALUES (kaynak.Ad, 1, SYSUTCDATETIME())
OUTPUT inserted.Id, kaynak.UserId INTO @eslesme (CompanyId, UserId);

UPDATE u SET u.CompanyId = e.CompanyId FROM Users u INNER JOIN @eslesme e ON e.UserId = u.Id;
UPDATE v SET v.CompanyId = u.CompanyId FROM Vehicles v INNER JOIN Users u ON u.Id = v.UserId;
UPDATE m SET m.CompanyId = v.CompanyId FROM MaintenanceRecords m INNER JOIN Vehicles v ON v.Id = m.VehicleId;
UPDATE f SET f.CompanyId = v.CompanyId FROM FuelRecords f INNER JOIN Vehicles v ON v.Id = f.VehicleId;
UPDATE x SET x.CompanyId = v.CompanyId FROM ExpenseRecords x INNER JOIN Vehicles v ON v.Id = x.VehicleId;
UPDATE r SET r.CompanyId = v.CompanyId FROM Reminders r INNER JOIN Vehicles v ON v.Id = r.VehicleId;
");

            foreach (var tablo in TenantTables)
            {
                migrationBuilder.AlterColumn<int>(
                    name: "CompanyId",
                    table: tablo,
                    type: "int",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "int",
                    oldNullable: true);
            }

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    MaintenanceRecordId = table.Column<int>(type: "int", nullable: true),
                    OriginalName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CompanyId",
                table: "Vehicles",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId",
                table: "Users",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_CompanyId",
                table: "Reminders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_CompanyId",
                table: "MaintenanceRecords",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelRecords_CompanyId",
                table: "FuelRecords",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRecords_CompanyId",
                table: "ExpenseRecords",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CompanyId",
                table: "Documents",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StoredName",
                table: "Documents",
                column: "StoredName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_CompanyId",
                table: "VehicleAssignments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_UserId_EndDate",
                table: "VehicleAssignments",
                columns: new[] { "UserId", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_VehicleId_EndDate",
                table: "VehicleAssignments",
                columns: new[] { "VehicleId", "EndDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseRecords_Companies_CompanyId",
                table: "ExpenseRecords",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FuelRecords_Companies_CompanyId",
                table: "FuelRecords",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceRecords_Companies_CompanyId",
                table: "MaintenanceRecords",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reminders_Companies_CompanyId",
                table: "Reminders",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Companies_CompanyId",
                table: "Users",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Companies_CompanyId",
                table: "Vehicles",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseRecords_Companies_CompanyId",
                table: "ExpenseRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_FuelRecords_Companies_CompanyId",
                table: "FuelRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceRecords_Companies_CompanyId",
                table: "MaintenanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Reminders_Companies_CompanyId",
                table: "Reminders");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Companies_CompanyId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Companies_CompanyId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "VehicleAssignments");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_CompanyId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Users_CompanyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Reminders_CompanyId",
                table: "Reminders");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRecords_CompanyId",
                table: "MaintenanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_FuelRecords_CompanyId",
                table: "FuelRecords");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseRecords_CompanyId",
                table: "ExpenseRecords");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "MaintenanceRecords");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "FuelRecords");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ExpenseRecords");
        }
    }
}
