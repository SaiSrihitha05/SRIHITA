using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsChatClosedToChatSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsChatClosed",
                table: "ChatSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsChatClosed",
                table: "ChatSessions");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 21, 18, 49, 38, 819, DateTimeKind.Utc).AddTicks(3881));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 21, 18, 49, 39, 21, DateTimeKind.Utc).AddTicks(6169), "$2a$11$BxecjHOtNNXM29Qa08WuBOqzt6gqYuXxEbGmN8mLnAxcxAUf1dU1q" });
        }
    }
}
