using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNomineeLimitsToPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxNominees",
                table: "Plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinNominees",
                table: "Plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 2, 9, 8, 51, 669, DateTimeKind.Utc).AddTicks(3229), "$2a$11$/KBgUtjGlHtpDi5.PhuYOONDWt0PLLDV2oVmHl6bw/KzSmo67D7gO" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxNominees",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "MinNominees",
                table: "Plans");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 28, 21, 8, 59, 646, DateTimeKind.Utc).AddTicks(4669), "$2a$11$lHjVyuyp6AjYj8JAB33uneYCJCo3IDhjVBbHbUFMsEr0t74I.u2Yq" });
        }
    }
}
