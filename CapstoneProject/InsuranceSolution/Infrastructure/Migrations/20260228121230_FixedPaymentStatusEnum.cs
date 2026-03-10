using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixedPaymentStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 28, 12, 12, 27, 518, DateTimeKind.Utc).AddTicks(7790), "$2a$11$jVdE/r5F8WQDASWABNooOumoOsBwU0Pdhdw47O.gbIuUmg4WJws8e" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 28, 6, 59, 19, 347, DateTimeKind.Utc).AddTicks(9685), "$2a$11$ISUB0arMs5WcXOhycnHgaOLignixGq7HLJpe9FpnEfM7G8cis7Sgm" });
        }
    }
}
