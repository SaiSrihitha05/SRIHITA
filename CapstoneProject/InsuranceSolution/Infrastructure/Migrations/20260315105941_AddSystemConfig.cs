using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastClaimsOfficerIndex = table.Column<int>(type: "int", nullable: false),
                    LastAgentAssignmentIndex = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SystemConfigs",
                columns: new[] { "Id", "LastAgentAssignmentIndex", "LastClaimsOfficerIndex", "UpdatedAt" },
                values: new object[] { 1, -1, -1, new DateTime(2026, 3, 15, 10, 59, 40, 195, DateTimeKind.Utc).AddTicks(5555) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 15, 10, 59, 40, 623, DateTimeKind.Utc).AddTicks(957), "$2a$11$GM5Ei9Q3s6SCXe2/Hn5.GuK5zmdQ8Ygp6SWfevjUgmJClvm/xrEx2" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemConfigs");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 15, 8, 1, 14, 780, DateTimeKind.Utc).AddTicks(7764), "$2a$11$rv5DB6kalrw2yZVdBmmrEuIh0ohGtNyHbnCOAP3www2.zB6IUgPqK" });
        }
    }
}
