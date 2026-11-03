using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nlwc.ems.Data.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class RenameCouplesRetreat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "event_templates",
                keyColumn: "id",
                keyValue: new Guid("0192f5a0-0000-7000-8000-0000000000b3"),
                column: "name",
                value: "Couples Retreat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "event_templates",
                keyColumn: "id",
                keyValue: new Guid("0192f5a0-0000-7000-8000-0000000000b3"),
                column: "name",
                value: "Couples");
        }
    }
}
