using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandUserIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "DateOfBirth", "Gender", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 13, 12, 27, 36, 60, DateTimeKind.Utc).AddTicks(6324), null, null, "$2a$11$ayA8Ozp1in.uGqZ0OD8E9eYXW0sOxRxmW3MJJsVaQNzvAWgbyTDJq" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 13, 9, 55, 36, 391, DateTimeKind.Utc).AddTicks(7964), "$2a$11$l.7GbgbWBLoMCTBrDM5pNOAT1720CfIWaAowUJ9q3EC0dZ.L0krt6" });
        }
    }
}
