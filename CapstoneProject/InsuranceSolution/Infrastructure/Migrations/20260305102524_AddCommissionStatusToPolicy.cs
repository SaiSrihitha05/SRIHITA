using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionStatusToPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommissionStatus",
                table: "PolicyAssignments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 5, 10, 25, 23, 193, DateTimeKind.Utc).AddTicks(915), "$2a$11$JvtWl/8zsawuMC/hRmhya.yY.uMlrpSratbLxcKUBV9G3TWOzI1KC" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommissionStatus",
                table: "PolicyAssignments");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 3, 11, 32, 50, 315, DateTimeKind.Utc).AddTicks(1560), "$2a$11$N4oi0dpXYtn5B/s33QrSROH.uFXQp67F9Q6f1m00b9u.k5QnexDa2" });
        }
    }
}
