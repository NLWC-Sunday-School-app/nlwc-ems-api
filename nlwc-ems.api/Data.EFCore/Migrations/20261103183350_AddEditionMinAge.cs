using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nlwc.ems.Data.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddEditionMinAge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "min_age",
                table: "event_editions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "min_age",
                table: "event_editions");
        }
    }
}
