using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nlwc.ems.Data.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCampDecisionsAndDuplicateDismissals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "camp_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    date_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    modified_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_camp_decisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_camp_decisions_people_decided_by",
                        column: x => x.decided_by,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_camp_decisions_registrations_registration_id",
                        column: x => x.registration_id,
                        principalTable: "registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "duplicate_dismissals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_a_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_b_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    dismissed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    date_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    modified_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_duplicate_dismissals", x => x.id);
                    table.CheckConstraint("ck_duplicate_dismissals_ordered", "person_a_id < person_b_id");
                    table.ForeignKey(
                        name: "fk_duplicate_dismissals_people_dismissed_by",
                        column: x => x.dismissed_by,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_duplicate_dismissals_people_person_a_id",
                        column: x => x.person_a_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_duplicate_dismissals_people_person_b_id",
                        column: x => x.person_b_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_camp_decisions_decided_by",
                table: "camp_decisions",
                column: "decided_by");

            migrationBuilder.CreateIndex(
                name: "ix_camp_decisions_registration_id_date_created",
                table: "camp_decisions",
                columns: new[] { "registration_id", "date_created" });

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_dismissals_dismissed_by",
                table: "duplicate_dismissals",
                column: "dismissed_by");

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_dismissals_person_a_id_person_b_id",
                table: "duplicate_dismissals",
                columns: new[] { "person_a_id", "person_b_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_duplicate_dismissals_person_b_id",
                table: "duplicate_dismissals",
                column: "person_b_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "camp_decisions");

            migrationBuilder.DropTable(
                name: "duplicate_dismissals");
        }
    }
}
