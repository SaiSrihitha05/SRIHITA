using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftPolicyStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 3, 9, 47, 5, 338, DateTimeKind.Utc).AddTicks(820), "$2a$11$Oqzx8LNtbrJzTS3436zUTetQ2gIm/N3T7xNKXfpGo.m2xpy2oZJ2a" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 2, 9, 8, 51, 669, DateTimeKind.Utc).AddTicks(3229), "$2a$11$/KBgUtjGlHtpDi5.PhuYOONDWt0PLLDV2oVmHl6bw/KzSmo67D7gO" });
        }
    }
}
