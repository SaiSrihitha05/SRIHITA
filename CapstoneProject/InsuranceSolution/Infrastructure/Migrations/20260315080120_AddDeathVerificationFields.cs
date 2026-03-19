using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeathVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CauseOfDeath",
                table: "Claims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfDeath",
                table: "Claims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 15, 8, 1, 14, 780, DateTimeKind.Utc).AddTicks(7764), "$2a$11$rv5DB6kalrw2yZVdBmmrEuIh0ohGtNyHbnCOAP3www2.zB6IUgPqK" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CauseOfDeath",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "DateOfDeath",
                table: "Claims");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 14, 20, 38, 4, 986, DateTimeKind.Utc).AddTicks(8774), "$2a$11$O.4VHjWCc2MTIxShythvtekyORgKsNkV1ecCE3CCrAQnM0OYCxHLC" });
        }
    }
}
