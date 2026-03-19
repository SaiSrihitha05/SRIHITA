using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberStatusToPolicyMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PolicyMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClaimForMemberId",
                table: "Claims",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 14, 20, 38, 4, 986, DateTimeKind.Utc).AddTicks(8774), "$2a$11$O.4VHjWCc2MTIxShythvtekyORgKsNkV1ecCE3CCrAQnM0OYCxHLC" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "PolicyMembers");

            migrationBuilder.DropColumn(
                name: "ClaimForMemberId",
                table: "Claims");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 13, 12, 27, 36, 60, DateTimeKind.Utc).AddTicks(6324), "$2a$11$ayA8Ozp1in.uGqZ0OD8E9eYXW0sOxRxmW3MJJsVaQNzvAWgbyTDJq" });
        }
    }
}
