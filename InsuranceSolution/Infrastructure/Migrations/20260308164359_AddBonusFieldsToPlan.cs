using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBonusFieldsToPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BonusRate",
                table: "Plans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TerminalBonusRate",
                table: "Plans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 8, 16, 43, 58, 277, DateTimeKind.Utc).AddTicks(7781), "$2a$11$jAm8IK5QtRCvZy5xtHi7r.AEYa4jd2akWMnxR8VoAEINOKShgtsCS" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BonusRate",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "TerminalBonusRate",
                table: "Plans");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 8, 11, 58, 18, 747, DateTimeKind.Utc).AddTicks(1466), "$2a$11$ab/r0OoYIuX3IxnXuBezgebw2eD8RX2QsiyHKzR2ZWLLl.B300qHa" });
        }
    }
}
