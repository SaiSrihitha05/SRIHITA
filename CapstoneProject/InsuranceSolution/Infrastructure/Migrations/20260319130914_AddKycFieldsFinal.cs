using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKycFieldsFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Claims_PolicyMembers_PolicyMemberId",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_Claims_PolicyMemberId",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "NomineeContact",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "PolicyMemberId",
                table: "Claims");

            migrationBuilder.RenameColumn(
                name: "NomineeName",
                table: "Claims",
                newName: "IssuedAuthority");

            migrationBuilder.AddColumn<string>(
                name: "ExtractedIdNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdProofDocumentPath",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdProofNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdProofType",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsKycVerified",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KycVerificationStatus",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "KycVerifiedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedIdNumber",
                table: "PolicyMembers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedName",
                table: "PolicyMembers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdProofDocumentPath",
                table: "PolicyMembers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdProofNumber",
                table: "PolicyMembers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdProofType",
                table: "PolicyMembers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsKycVerified",
                table: "PolicyMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KycVerificationStatus",
                table: "PolicyMembers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "KycVerifiedAt",
                table: "PolicyMembers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationDate",
                table: "Claims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VerifiedByOfficer",
                table: "Claims",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 13, 9, 11, 985, DateTimeKind.Utc).AddTicks(8860));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExtractedIdNumber", "ExtractedName", "IdProofDocumentPath", "IdProofNumber", "IdProofType", "IsKycVerified", "KycVerificationStatus", "KycVerifiedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 19, 13, 9, 12, 467, DateTimeKind.Utc).AddTicks(6960), null, null, null, null, null, false, "Pending", null, "$2a$11$fzHcByMIcKSsSisZMnsQc.t0QeAjl85c2JPj2xM6hOvWbfE16V3QO" });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ClaimForMemberId",
                table: "Claims",
                column: "ClaimForMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_Claims_PolicyMembers_ClaimForMemberId",
                table: "Claims",
                column: "ClaimForMemberId",
                principalTable: "PolicyMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Claims_PolicyMembers_ClaimForMemberId",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_Claims_ClaimForMemberId",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "ExtractedIdNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExtractedName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IdProofDocumentPath",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IdProofNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IdProofType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsKycVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KycVerificationStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KycVerifiedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExtractedIdNumber",
                table: "PolicyMembers");

            migrationBuilder.DropColumn(
                name: "ExtractedName",
                table: "PolicyMembers");

            migrationBuilder.DropColumn(
                name: "IdProofDocumentPath",
                table: "PolicyMembers");

            migrationBuilder.DropColumn(
                name: "IdProofNumber",
                table: "PolicyMembers");

            migrationBuilder.DropColumn(
                name: "IdProofType",
                table: "PolicyMembers");

            migrationBuilder.DropColumn(
                name: "IsKycVerified",
                table: "PolicyMembers");

            migrationBuilder.DropColumn(
                name: "KycVerificationStatus",
                table: "PolicyMembers");

            migrationBuilder.DropColumn(
                name: "KycVerifiedAt",
                table: "PolicyMembers");

            migrationBuilder.DropColumn(
                name: "VerificationDate",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "VerifiedByOfficer",
                table: "Claims");

            migrationBuilder.RenameColumn(
                name: "IssuedAuthority",
                table: "Claims",
                newName: "NomineeName");

            migrationBuilder.AddColumn<string>(
                name: "NomineeContact",
                table: "Claims",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PolicyMemberId",
                table: "Claims",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 18, 11, 18, 6, 13, DateTimeKind.Utc).AddTicks(2861));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 18, 11, 18, 6, 291, DateTimeKind.Utc).AddTicks(4552), "$2a$11$tE/xMmQAnwOm0zrgAR2kp.mYwm5Hgw8ErNqy9p1MVEk0EOQBeDBUq" });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_PolicyMemberId",
                table: "Claims",
                column: "PolicyMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_Claims_PolicyMembers_PolicyMemberId",
                table: "Claims",
                column: "PolicyMemberId",
                principalTable: "PolicyMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
