using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanModuleAndPlanEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CoverageIncreaseRate",
                table: "Plans",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "CoverageIncreasing",
                table: "Plans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CoverageUntilAge",
                table: "Plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasBonus",
                table: "Plans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasDeathBenefit",
                table: "Plans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasLoanFacility",
                table: "Plans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LoanEligibleAfterYears",
                table: "Plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "LoanInterestRate",
                table: "Plans",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxLoanPercentage",
                table: "Plans",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PolicyLoans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyAssignmentId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    LoanAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalInterestPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoanDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyLoans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyLoans_PolicyAssignments_PolicyAssignmentId",
                        column: x => x.PolicyAssignmentId,
                        principalTable: "PolicyAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PolicyLoans_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanRepayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyLoanId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrincipalPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RepaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanRepayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanRepayments_PolicyLoans_PolicyLoanId",
                        column: x => x.PolicyLoanId,
                        principalTable: "PolicyLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 8, 8, 15, 12, 715, DateTimeKind.Utc).AddTicks(3282), "$2a$11$cT0CLM88Cpg1m.nbn3frEOigBiJaN1y/tuPZG8yt5Bm5.3Tb654x." });

            migrationBuilder.CreateIndex(
                name: "IX_LoanRepayments_PolicyLoanId",
                table: "LoanRepayments",
                column: "PolicyLoanId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyLoans_CustomerId",
                table: "PolicyLoans",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyLoans_PolicyAssignmentId",
                table: "PolicyLoans",
                column: "PolicyAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanRepayments");

            migrationBuilder.DropTable(
                name: "PolicyLoans");

            migrationBuilder.DropColumn(
                name: "CoverageIncreaseRate",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "CoverageIncreasing",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "CoverageUntilAge",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "HasBonus",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "HasDeathBenefit",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "HasLoanFacility",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "LoanEligibleAfterYears",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "LoanInterestRate",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "MaxLoanPercentage",
                table: "Plans");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 5, 10, 25, 23, 193, DateTimeKind.Utc).AddTicks(915), "$2a$11$JvtWl/8zsawuMC/hRmhya.yY.uMlrpSratbLxcKUBV9G3TWOzI1KC" });
        }
    }
}
