using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaceOfDeathToClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlaceOfDeath",
                table: "Claims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 15, 14, 24, 22, 724, DateTimeKind.Utc).AddTicks(1003));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 15, 14, 24, 23, 13, DateTimeKind.Utc).AddTicks(8809), "$2a$11$eUnUBzQvW2nXzpi6EK95qedfk65Svx6tnfdABlU9dAOcwJT2Nty.e" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaceOfDeath",
                table: "Claims");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 15, 10, 59, 40, 195, DateTimeKind.Utc).AddTicks(5555));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 15, 10, 59, 40, 623, DateTimeKind.Utc).AddTicks(957), "$2a$11$GM5Ei9Q3s6SCXe2/Hn5.GuK5zmdQ8Ygp6SWfevjUgmJClvm/xrEx2" });
        }
    }
}
