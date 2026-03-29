using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanLinksToChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LinkedPlanId",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedPlanName",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedPlanUrl",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MessageType",
                table: "ChatMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 22, 10, 53, 34, 242, DateTimeKind.Utc).AddTicks(4472));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 22, 10, 53, 34, 408, DateTimeKind.Utc).AddTicks(2105), "$2a$11$avUyVsx9rkkT9xV32uZBde5aQdo8K.fa1iQiVlUqkvHRUxkobzVfe" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkedPlanId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "LinkedPlanName",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "LinkedPlanUrl",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "ChatMessages");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 22, 10, 38, 37, 642, DateTimeKind.Utc).AddTicks(6428));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 22, 10, 38, 37, 900, DateTimeKind.Utc).AddTicks(3418), "$2a$11$XqAMpILnT5lF1Rw/0/ANb.DwuuiFUUUBNi3GTR4CO/lZSPsB9Sloe" });
        }
    }
}
