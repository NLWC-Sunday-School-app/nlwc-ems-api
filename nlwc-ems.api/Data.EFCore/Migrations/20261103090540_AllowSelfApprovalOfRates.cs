using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nlwc.ems.Data.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AllowSelfApprovalOfRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_rate_plan_versions_separation",
                table: "rate_plan_versions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_rate_plan_versions_separation",
                table: "rate_plan_versions",
                sql: "approved_by IS NULL OR approved_by <> drafted_by");
        }
    }
}
