using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRelatedPolicyToChatSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RelatedPolicyId",
                table: "ChatSessions",
                type: "int",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_RelatedPolicyId",
                table: "ChatSessions",
                column: "RelatedPolicyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_PolicyAssignments_RelatedPolicyId",
                table: "ChatSessions",
                column: "RelatedPolicyId",
                principalTable: "PolicyAssignments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_PolicyAssignments_RelatedPolicyId",
                table: "ChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_RelatedPolicyId",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "RelatedPolicyId",
                table: "ChatSessions");

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
    }
}
