using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressAndRenewalPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "PolicyAssignments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
            */

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 28, 18, 1, 42, 894, DateTimeKind.Utc).AddTicks(2303));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 28, 18, 1, 43, 192, DateTimeKind.Utc).AddTicks(4297), "$2a$11$OA7e6el4oYPmAxsJEv2HNurkdpYx/AjDOxyzZGRus6vk7vIGwpyze" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "PolicyAssignments");

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
    }
}
