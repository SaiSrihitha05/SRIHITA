using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficerRemarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OfficerRemarks",
                table: "Claims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 18, 11, 18, 6, 13, DateTimeKind.Utc).AddTicks(2861));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 18, 11, 18, 6, 291, DateTimeKind.Utc).AddTicks(4552), "$2a$11$tE/xMmQAnwOm0zrgAR2kp.mYwm5Hgw8ErNqy9p1MVEk0EOQBeDBUq" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfficerRemarks",
                table: "Claims");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 18, 5, 15, 29, 149, DateTimeKind.Utc).AddTicks(7320));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 18, 5, 15, 29, 510, DateTimeKind.Utc).AddTicks(321), "$2a$11$KLT4jiHX9L/xLjpOZupiZupnTd5qTsmfYiZZeQDWAvdWNyV74bQfy" });
        }
    }
}
