using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficerToChatSenderType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 22, 10, 38, 37, 642, DateTimeKind.Utc).AddTicks(6428));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 22, 10, 38, 37, 900, DateTimeKind.Utc).AddTicks(3418), "$2a$11$XqAMpILnT5lF1Rw/0/ANb.DwuuiFUUUBNi3GTR4CO/lZSPsB9Sloe" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 22, 9, 11, 8, 631, DateTimeKind.Utc).AddTicks(7471));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 22, 9, 11, 9, 26, DateTimeKind.Utc).AddTicks(3979), "$2a$11$12Kb.5b/oc5wpTzGtcyZzeFPQVsh7G3PH2RKdUIrM7i.9cjoS3Q96" });
        }
    }
}
