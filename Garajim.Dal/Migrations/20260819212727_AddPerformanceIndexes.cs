using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garajim.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reminders_VehicleId",
                table: "Reminders");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRecords_VehicleId",
                table: "MaintenanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_FuelRecords_VehicleId",
                table: "FuelRecords");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseRecords_VehicleId",
                table: "ExpenseRecords");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_VehicleId_IsCompleted_DueDate",
                table: "Reminders",
                columns: new[] { "VehicleId", "IsCompleted", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_VehicleId_Date",
                table: "MaintenanceRecords",
                columns: new[] { "VehicleId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_FuelRecords_VehicleId_Date",
                table: "FuelRecords",
                columns: new[] { "VehicleId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRecords_VehicleId_Date",
                table: "ExpenseRecords",
                columns: new[] { "VehicleId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reminders_VehicleId_IsCompleted_DueDate",
                table: "Reminders");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRecords_VehicleId_Date",
                table: "MaintenanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_FuelRecords_VehicleId_Date",
                table: "FuelRecords");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseRecords_VehicleId_Date",
                table: "ExpenseRecords");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_VehicleId",
                table: "Reminders",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_VehicleId",
                table: "MaintenanceRecords",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelRecords_VehicleId",
                table: "FuelRecords",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRecords_VehicleId",
                table: "ExpenseRecords",
                column: "VehicleId");
        }
    }
}
