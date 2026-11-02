using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nlwc.ems.Data.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddEditionMaxAge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_age",
                table: "event_editions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_age",
                table: "event_editions");
        }
    }
}
