using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTermYearsToPolicyAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TermYears",
                table: "PolicyAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 28, 6, 59, 19, 347, DateTimeKind.Utc).AddTicks(9685), "$2a$11$ISUB0arMs5WcXOhycnHgaOLignixGq7HLJpe9FpnEfM7G8cis7Sgm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TermYears",
                table: "PolicyAssignments");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 27, 10, 20, 12, 720, DateTimeKind.Utc).AddTicks(9002), "$2a$11$sXzkGUCsvfdTEwWLROVdguwkRD4PC3u2JdZhwMqMGJA1.RathPOZm" });
        }
    }
}
