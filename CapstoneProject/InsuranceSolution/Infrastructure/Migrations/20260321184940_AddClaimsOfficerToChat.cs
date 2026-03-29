using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimsOfficerToChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClaimsOfficerId",
                table: "ChatSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAgentAssigned",
                table: "ChatSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsClaimsOfficerAssigned",
                table: "ChatSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RelatedClaimId",
                table: "ChatSessions",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 21, 18, 49, 38, 819, DateTimeKind.Utc).AddTicks(3881));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 21, 18, 49, 39, 21, DateTimeKind.Utc).AddTicks(6169), "$2a$11$BxecjHOtNNXM29Qa08WuBOqzt6gqYuXxEbGmN8mLnAxcxAUf1dU1q" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_RelatedClaimId",
                table: "ChatSessions",
                column: "RelatedClaimId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_Claims_RelatedClaimId",
                table: "ChatSessions",
                column: "RelatedClaimId",
                principalTable: "Claims",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_Claims_RelatedClaimId",
                table: "ChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_RelatedClaimId",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "ClaimsOfficerId",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "IsAgentAssigned",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "IsClaimsOfficerAssigned",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "RelatedClaimId",
                table: "ChatSessions");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 21, 18, 16, 51, 672, DateTimeKind.Utc).AddTicks(8538));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 21, 18, 16, 51, 995, DateTimeKind.Utc).AddTicks(7721), "$2a$11$a8x0lxrKQKmQfjOlW4e9sOekcJ7jiji94ju.BJcKK1x/K51DsoOCW" });
        }
    }
}
