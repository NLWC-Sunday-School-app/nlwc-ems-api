using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nlwc.ems.Data.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class FamilyEmergencyContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_emergency_contacts_people_person_id", table: "emergency_contacts");
            migrationBuilder.DropIndex(
                name: "ix_emergency_contacts_person_id",
                table: "emergency_contacts");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_contacts_person_id",
                table: "emergency_contacts",
                column: "person_id");
            migrationBuilder.AddForeignKey(name: "fk_emergency_contacts_people_person_id", table: "emergency_contacts", column: "person_id",
                principalTable: "people", principalColumn: "id", onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_emergency_contacts_people_person_id", table: "emergency_contacts");
            migrationBuilder.DropIndex(
                name: "ix_emergency_contacts_person_id",
                table: "emergency_contacts");

            migrationBuilder.CreateIndex(
                name: "ix_emergency_contacts_person_id",
                table: "emergency_contacts",
                column: "person_id",
                unique: true);
            migrationBuilder.AddForeignKey(name: "fk_emergency_contacts_people_person_id", table: "emergency_contacts", column: "person_id",
                principalTable: "people", principalColumn: "id", onDelete: ReferentialAction.Cascade);
        }
    }
}
