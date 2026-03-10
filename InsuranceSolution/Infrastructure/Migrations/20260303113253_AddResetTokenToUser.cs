using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResetTokenToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "ResetToken", "ResetTokenExpiry" },
                values: new object[] { new DateTime(2026, 3, 3, 11, 32, 50, 315, DateTimeKind.Utc).AddTicks(1560), "$2a$11$N4oi0dpXYtn5B/s33QrSROH.uFXQp67F9Q6f1m00b9u.k5QnexDa2", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 3, 9, 47, 5, 338, DateTimeKind.Utc).AddTicks(820), "$2a$11$Oqzx8LNtbrJzTS3436zUTetQ2gIm/N3T7xNKXfpGo.m2xpy2oZJ2a" });
        }
    }
}
