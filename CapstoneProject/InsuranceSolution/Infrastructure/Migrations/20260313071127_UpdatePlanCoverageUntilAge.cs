using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlanCoverageUntilAge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverageUntilAge",
                table: "Plans");

            migrationBuilder.AddColumn<bool>(
                name: "IsCoverageUntilAge",
                table: "Plans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 13, 7, 11, 24, 963, DateTimeKind.Utc).AddTicks(168), "$2a$11$fnS6V/F1IDkGhNw4J2M6puegCO8qzR.uAjr6YumEukDELill54Fqi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCoverageUntilAge",
                table: "Plans");

            migrationBuilder.AddColumn<int>(
                name: "CoverageUntilAge",
                table: "Plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 8, 16, 43, 58, 277, DateTimeKind.Utc).AddTicks(7781), "$2a$11$jAm8IK5QtRCvZy5xtHi7r.AEYa4jd2akWMnxR8VoAEINOKShgtsCS" });
        }
    }
}
