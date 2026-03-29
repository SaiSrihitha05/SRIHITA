using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "ChatMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SessionInternalId",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    AgentId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 21, 18, 16, 51, 672, DateTimeKind.Utc).AddTicks(8538));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 21, 18, 16, 51, 995, DateTimeKind.Utc).AddTicks(7721), "$2a$11$a8x0lxrKQKmQfjOlW4e9sOekcJ7jiji94ju.BJcKK1x/K51DsoOCW" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionInternalId",
                table: "ChatMessages",
                column: "SessionInternalId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_ChatSessions_SessionInternalId",
                table: "ChatMessages",
                column: "SessionInternalId",
                principalTable: "ChatSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_ChatSessions_SessionInternalId",
                table: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_SessionInternalId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "SessionInternalId",
                table: "ChatMessages");

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
    }
}
