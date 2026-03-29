using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSenderType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFromUser",
                table: "ChatMessages");

            migrationBuilder.AddColumn<int>(
                name: "SenderType",
                table: "ChatMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 21, 17, 39, 29, 226, DateTimeKind.Utc).AddTicks(6546));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 21, 17, 39, 29, 633, DateTimeKind.Utc).AddTicks(5119), "$2a$11$XF5U/EolMK4YwQSJNqlLX./ZYd9MbySVnVj21eyq4heKNAKQJY.Du" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SenderType",
                table: "ChatMessages");

            migrationBuilder.AddColumn<bool>(
                name: "IsFromUser",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 21, 16, 47, 49, 620, DateTimeKind.Utc).AddTicks(778));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 21, 16, 47, 50, 38, DateTimeKind.Utc).AddTicks(9975), "$2a$11$tr7vsSQm1Su3g8gWXgu9duoZPX9wky4UoB0Nnxs724We9XbjUPHM." });
        }
    }
}
