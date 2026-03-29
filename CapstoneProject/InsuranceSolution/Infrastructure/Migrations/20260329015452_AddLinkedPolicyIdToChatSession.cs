using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedPolicyIdToChatSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LinkedPolicyId",
                table: "ChatSessions",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 29, 1, 54, 50, 469, DateTimeKind.Utc).AddTicks(8869));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 29, 1, 54, 50, 835, DateTimeKind.Utc).AddTicks(1393), "$2a$11$b125uq/Ff6EXA6Z3NEnQ7ufTTugcivy66paKsW23JefESmHCasyYW" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkedPolicyId",
                table: "ChatSessions");

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
    }
}
