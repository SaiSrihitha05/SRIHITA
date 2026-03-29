using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReinstatementToPlanAndPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LapsedDate",
                table: "PolicyAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReinstatedDate",
                table: "PolicyAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReinstatementDays",
                table: "Plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ReinstatementPenaltyAmount",
                table: "Plans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 27, 6, 36, 45, 754, DateTimeKind.Utc).AddTicks(1435));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 27, 6, 36, 46, 218, DateTimeKind.Utc).AddTicks(6483), "$2a$11$P2dunAnHpOimZmQXViiGK.P.wbwq/5so8NjhbwkRFpJLDwUBhka2." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LapsedDate",
                table: "PolicyAssignments");

            migrationBuilder.DropColumn(
                name: "ReinstatedDate",
                table: "PolicyAssignments");

            migrationBuilder.DropColumn(
                name: "ReinstatementDays",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ReinstatementPenaltyAmount",
                table: "Plans");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 22, 18, 56, 54, 972, DateTimeKind.Utc).AddTicks(9671));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 22, 18, 56, 55, 437, DateTimeKind.Utc).AddTicks(3492), "$2a$11$kF6EJZQb3G8uHJBogsNN.eTgukkLZxZWGb1d52bTbcF/dbX/mzvJ2" });
        }
    }
}
