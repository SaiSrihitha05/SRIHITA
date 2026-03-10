using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallmentsPaidToPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstallmentsPaid",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 28, 20, 2, 26, 482, DateTimeKind.Utc).AddTicks(9554), "$2a$11$PZzNhehgX1b1W.mYfkxVg.TvTmGqQ.T5CIOF2EX8wXA6ZyzZ0n2by" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstallmentsPaid",
                table: "Payments");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 28, 12, 12, 27, 518, DateTimeKind.Utc).AddTicks(7790), "$2a$11$jVdE/r5F8WQDASWABNooOumoOsBwU0Pdhdw47O.gbIuUmg4WJws8e" });
        }
    }
}
