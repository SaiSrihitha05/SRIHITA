using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionRateToPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CommissionRate",
                table: "Plans",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 27, 9, 23, 0, 699, DateTimeKind.Utc).AddTicks(2177), "$2a$11$O2gNe8dcTW.iPAEQ/FBhNuoC8Ho8QYisHBZAd2w1CAq74WEAEKJ1u" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommissionRate",
                table: "Plans");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 26, 18, 4, 48, 214, DateTimeKind.Utc).AddTicks(9596), "$2a$11$DKqHDQM7o4lkF8jTKbVp/eQPJjFhSMqpDbiv65sHHDkiR8d1RBNu." });
        }
    }
}
