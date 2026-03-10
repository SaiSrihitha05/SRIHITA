using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlanTypeToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing PlanType strings to their corresponding enum integers before altering column
            migrationBuilder.Sql("UPDATE Plans SET PlanType = '0' WHERE PlanType LIKE '%Term%'");
            migrationBuilder.Sql("UPDATE Plans SET PlanType = '1' WHERE PlanType LIKE '%Endowment%'");
            migrationBuilder.Sql("UPDATE Plans SET PlanType = '2' WHERE PlanType LIKE '%Saving%'");
            migrationBuilder.Sql("UPDATE Plans SET PlanType = '3' WHERE PlanType LIKE '%Whole%'");
            migrationBuilder.Sql("UPDATE Plans SET PlanType = '4' WHERE PlanType NOT IN ('0','1','2','3')");

            migrationBuilder.AlterColumn<int>(
                name: "PlanType",
                table: "Plans",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PlanType",
                table: "Plans",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
