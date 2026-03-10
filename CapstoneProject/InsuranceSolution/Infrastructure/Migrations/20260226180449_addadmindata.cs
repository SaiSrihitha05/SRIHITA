using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addadmindata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "Name", "PasswordHash", "Phone", "Role" },
                values: new object[] { 1, new DateTime(2026, 2, 26, 18, 4, 48, 214, DateTimeKind.Utc).AddTicks(9596), "admin@insurance.com", true, "System Admin", "$2a$11$DKqHDQM7o4lkF8jTKbVp/eQPJjFhSMqpDbiv65sHHDkiR8d1RBNu.", "9999999999", "Admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
