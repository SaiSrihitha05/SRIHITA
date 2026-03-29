using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimResubmissionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Claims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResubmissionCount",
                table: "Claims",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResubmissionDeadline",
                table: "Claims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 22, 16, 54, 44, 91, DateTimeKind.Utc).AddTicks(3207));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 22, 16, 54, 44, 535, DateTimeKind.Utc).AddTicks(207), "$2a$11$hrLmHZpjuYBul6V3N40.puVvx6BXlZTWLsnCszpNVYj5FtcYKbSp6" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "ResubmissionCount",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "ResubmissionDeadline",
                table: "Claims");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 22, 14, 22, 9, 866, DateTimeKind.Utc).AddTicks(529));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 22, 14, 22, 10, 153, DateTimeKind.Utc).AddTicks(2830), "$2a$11$Buu/vn8jV7tv9ViMEM4qeeMC1ezJkCXCTdXMSvXEotaYJcf7W0ejO" });
        }
    }
}
