using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRemarksToPolicyAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "PolicyAssignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 13, 9, 55, 36, 391, DateTimeKind.Utc).AddTicks(7964), "$2a$11$l.7GbgbWBLoMCTBrDM5pNOAT1720CfIWaAowUJ9q3EC0dZ.L0krt6" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "PolicyAssignments");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 13, 7, 46, 55, 402, DateTimeKind.Utc).AddTicks(5062), "$2a$11$rWu0qNotEQ9okVZnR6OlLebFV29PTAn1ehb/wSe6fKmGHZ62KOgqW" });
        }
    }
}
