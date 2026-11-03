using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace nlwc.ems.Data.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "access_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    actor_account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    resource = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    subject_person_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    edition_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_logs", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    seq = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    occurred_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    actor_account_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    action = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entity_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    record_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    display_name = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    edition_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    detail = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    prev_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_events", x => x.seq);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    given_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    family_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    middle_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    marital_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    country = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    city = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_people", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "policy_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    document_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    version = table.Column<int>(type: "int", nullable: false),
                    body = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_policy_versions", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    key = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "venues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    address = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venues", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "event_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    organization_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    config = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_templates_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "account_claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    person_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    token_hash = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    invited_email = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    state = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_account_claims_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    person_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    email = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_hash = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    google_sub = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    failed_logins = table.Column<int>(type: "int", nullable: false),
                    locked_until = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.id);
                    table.CheckConstraint("ck_accounts_credential", "password_hash IS NOT NULL OR google_sub IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_accounts_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "authorized_collectors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    child_person_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    full_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    relationship = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone_encrypted = table.Column<byte[]>(type: "longblob", nullable: false),
                    valid_days = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    removed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authorized_collectors", x => x.id);
                    table.ForeignKey(
                        name: "fk_authorized_collectors_people_child_person_id",
                        column: x => x.child_person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "duplicate_dismissals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    person_a_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    person_b_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dismissed_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "emergency_contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    person_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    full_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    relationship = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone_encrypted = table.Column<byte[]>(type: "longblob", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_emergency_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_emergency_contacts_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guardian_authorizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    guardian_person_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    dependant_person_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    declared_relationship = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    live_key = table.Column<int>(type: "int", nullable: true, computedColumnSql: "IF(`revoked_at` IS NULL, 1, NULL)", stored: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guardian_authorizations", x => x.id);
                    table.CheckConstraint("ck_guardian_authorizations_distinct", "guardian_person_id <> dependant_person_id");
                    table.ForeignKey(
                        name: "fk_guardian_authorizations_people_dependant_person_id",
                        column: x => x.dependant_person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_guardian_authorizations_people_guardian_person_id",
                        column: x => x.guardian_person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "person_relationships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    from_person_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    to_person_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    kind = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_person_relationships", x => x.id);
                    table.CheckConstraint("ck_person_relationships_distinct", "from_person_id <> to_person_id");
                    table.ForeignKey(
                        name: "fk_person_relationships_people_from_person_id",
                        column: x => x.from_person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_person_relationships_people_to_person_id",
                        column: x => x.to_person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "verified_contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    person_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    verified_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_verified_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_verified_contacts_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    role_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    resource = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    level = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "event_editions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    template_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    venue_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    slug = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    timezone = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false),
                    age_reference_date = table.Column<DateOnly>(type: "date", nullable: false),
                    max_age = table.Column<int>(type: "int", nullable: true),
                    camp_min_age = table.Column<int>(type: "int", nullable: true),
                    camp_max_age = table.Column<int>(type: "int", nullable: true),
                    camp_capacity = table.Column<int>(type: "int", nullable: true),
                    pickup_window_start = table.Column<TimeOnly>(type: "time(6)", nullable: true),
                    pickup_window_end = table.Column<TimeOnly>(type: "time(6)", nullable: true),
                    visibility = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    registration_window = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    live_state = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_editions", x => x.id);
                    table.CheckConstraint("ck_event_editions_camp_ages", "camp_min_age IS NULL OR camp_max_age IS NULL OR camp_min_age <= camp_max_age");
                    table.CheckConstraint("ck_event_editions_dates", "ends_on >= starts_on");
                    table.ForeignKey(
                        name: "fk_event_editions_event_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "event_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_editions_venues_venue_id",
                        column: x => x.venue_id,
                        principalTable: "venues",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "auth_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    token_hash = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    surface = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    last_seen_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    idle_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    absolute_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    mfa_verified_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_auth_sessions_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_devices_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "email_verifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    token_hash = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    used_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_verifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_verifications_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mfa_factors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    secret_encrypted = table.Column<byte[]>(type: "longblob", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mfa_factors", x => x.id);
                    table.ForeignKey(
                        name: "fk_mfa_factors_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "edition_capabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    edition_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    capability = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_edition_capabilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_edition_capabilities_event_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "event_editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "edition_contents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    edition_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    published_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_edition_contents", x => x.id);
                    table.ForeignKey(
                        name: "fk_edition_contents_event_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "event_editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "event_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    edition_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_days", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_days_event_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "event_editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rate_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    edition_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rate_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_rate_plans_event_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "event_editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reference_counters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    edition_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    last_value = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reference_counters", x => x.id);
                    table.ForeignKey(
                        name: "fk_reference_counters_event_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "event_editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    reference = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    edition_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    person_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    state = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    participation_mode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    age_on_reference_date = table.Column<int>(type: "int", nullable: true),
                    age_band = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    draft = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    active_key = table.Column<int>(type: "int", nullable: true, computedColumnSql: "IF(`state` IN ('Cancelled', 'Rejected', 'Expired'), NULL, 1)", stored: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registrations", x => x.id);
                    table.ForeignKey(
                        name: "fk_registrations_event_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "event_editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_registrations_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "role_grants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    account_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    role_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    edition_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    granted_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_grants", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_grants_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_grants_accounts_granted_by",
                        column: x => x.granted_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_grants_event_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "event_editions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_role_grants_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rate_plan_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    plan_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    version = table.Column<int>(type: "int", nullable: false),
                    state = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drafted_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    approved_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    published_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    published_key = table.Column<int>(type: "int", nullable: true, computedColumnSql: "IF(`state` = 'Published', 1, NULL)", stored: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rate_plan_versions", x => x.id);
                    table.CheckConstraint("ck_rate_plan_versions_approved", "state = 'Draft' OR approved_by IS NOT NULL");
                    table.CheckConstraint("ck_rate_plan_versions_separation", "approved_by IS NULL OR approved_by <> drafted_by");
                    table.ForeignKey(
                        name: "fk_rate_plan_versions_accounts_approved_by",
                        column: x => x.approved_by,
                        principalTable: "accounts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_rate_plan_versions_accounts_drafted_by",
                        column: x => x.drafted_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rate_plan_versions_rate_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "rate_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "camp_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    decision = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    decided_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "consent_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    person_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    kind = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    accepted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    document_id = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    document_version = table.Column<int>(type: "int", nullable: false),
                    answered_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consent_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_consent_records_people_answered_by",
                        column: x => x.answered_by,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_consent_records_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_consent_records_registrations_registration_id",
                        column: x => x.registration_id,
                        principalTable: "registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "health_disclosures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    data_encrypted = table.Column<byte[]>(type: "longblob", nullable: false),
                    has_flags = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_disclosures", x => x.id);
                    table.ForeignKey(
                        name: "fk_health_disclosures_registrations_registration_id",
                        column: x => x.registration_id,
                        principalTable: "registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    invited_by = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    token_hash = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_invitations_people_invited_by",
                        column: x => x.invited_by,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_invitations_registrations_registration_id",
                        column: x => x.registration_id,
                        principalTable: "registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "registration_actors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    person_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_actors", x => x.id);
                    table.ForeignKey(
                        name: "fk_registration_actors_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_registration_actors_registrations_registration_id",
                        column: x => x.registration_id,
                        principalTable: "registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "registration_status_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    from_state = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    to_state = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    actor_person_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_status_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_registration_status_histories_registrations_registration_id",
                        column: x => x.registration_id,
                        principalTable: "registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rate_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    version_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    age_min = table.Column<int>(type: "int", nullable: false),
                    age_max = table.Column<int>(type: "int", nullable: true),
                    participation_mode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    accommodation_category = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    amount_kobo = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_by_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rate_rules", x => x.id);
                    table.CheckConstraint("ck_rate_rules_ages", "age_min >= 0 AND (age_max IS NULL OR age_max >= age_min)");
                    table.CheckConstraint("ck_rate_rules_kind", "(kind = 'Amount' AND amount_kobo IS NOT NULL AND amount_kobo > 0) OR (kind = 'Free' AND amount_kobo IS NULL)");
                    table.ForeignKey(
                        name: "fk_rate_rules_rate_plan_versions_version_id",
                        column: x => x.version_id,
                        principalTable: "rate_plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "organizations",
                columns: new[] { "id", "created_by", "created_by_ip", "date_created", "date_modified", "modified_by", "modified_by_ip", "name", "status" },
                values: new object[] { new Guid("0192f5a0-0000-7000-8000-000000000001"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "NLWC", "Active" });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_by", "created_by_ip", "date_created", "date_modified", "key", "modified_by", "modified_by_ip", "name", "status" },
                values: new object[,]
                {
                    { new Guid("0192f5a0-0000-7000-8000-000000000101"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "super_admin", null, null, "Super Admin", "Active" },
                    { new Guid("0192f5a0-0000-7000-8000-000000000102"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "event_admin", null, null, "Event Admin", "Active" },
                    { new Guid("0192f5a0-0000-7000-8000-000000000103"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "finance_officer", null, null, "Finance Officer", "Active" },
                    { new Guid("0192f5a0-0000-7000-8000-000000000104"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "checkin_officer", null, null, "Child Check-in Officer", "Active" },
                    { new Guid("0192f5a0-0000-7000-8000-000000000105"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "child_safety_officer", null, null, "Child Safety Officer", "Active" },
                    { new Guid("0192f5a0-0000-7000-8000-000000000106"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "accommodation_manager", null, null, "Accommodation Manager", "Active" },
                    { new Guid("0192f5a0-0000-7000-8000-000000000107"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "medical_welfare", null, null, "Medical / Welfare", "Active" },
                    { new Guid("0192f5a0-0000-7000-8000-000000000108"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "volunteer_usher", null, null, "Volunteer / Usher", "Active" },
                    { new Guid("0192f5a0-0000-7000-8000-000000000109"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "registration_officer", null, null, "Registration Officer", "Active" },
                    { new Guid("0192f5a0-0000-7000-8000-000000000110"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "camp_coordinator", null, null, "Camp Coordinator", "Active" },
                    { new Guid("0192f5a0-0000-7000-8000-000000000111"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "communications_officer", null, null, "Communications Officer", "Active" },
                    { new Guid("0192f5a0-0000-7000-8000-000000000112"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "auditor", null, null, "Auditor", "Active" }
                });

            migrationBuilder.InsertData(
                table: "event_templates",
                columns: new[] { "id", "code", "config", "created_by", "created_by_ip", "date_created", "date_modified", "modified_by", "modified_by_ip", "name", "organization_id", "status" },
                values: new object[,]
                {
                    { new Guid("0192f5a0-0000-7000-8000-0000000000b1"), "BB", "{}", null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Bible Bootcamp", new Guid("0192f5a0-0000-7000-8000-000000000001"), "Active" },
                    { new Guid("0192f5a0-0000-7000-8000-0000000000b2"), "SS", "{}", null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Singlz Summit", new Guid("0192f5a0-0000-7000-8000-000000000001"), "Active" },
                    { new Guid("0192f5a0-0000-7000-8000-0000000000b3"), "CP", "{}", null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Couples", new Guid("0192f5a0-0000-7000-8000-000000000001"), "Active" }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "id", "created_by", "created_by_ip", "date_created", "date_modified", "level", "modified_by", "modified_by_ip", "resource", "role_id", "status" },
                values: new object[,]
                {
                    { new Guid("0143331a-c01e-2bad-eee4-6279b3a76eb6"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "money", new Guid("0192f5a0-0000-7000-8000-000000000112"), "Active" },
                    { new Guid("03af612b-69fc-67b9-bc37-9dc065ecedd6"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "edit", null, null, "config", new Guid("0192f5a0-0000-7000-8000-000000000102"), "Active" },
                    { new Guid("05520f4b-ba34-e944-6113-21a6c760f9bd"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "scan", null, null, "custody", new Guid("0192f5a0-0000-7000-8000-000000000108"), "Active" },
                    { new Guid("0e0d3071-71f7-b7fe-32b4-afb25f64413e"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "edit", null, null, "people", new Guid("0192f5a0-0000-7000-8000-000000000109"), "Active" },
                    { new Guid("0fbd1f7d-709a-095d-8095-f5a9a063310a"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "edit", null, null, "communications", new Guid("0192f5a0-0000-7000-8000-000000000111"), "Active" },
                    { new Guid("13002b98-5ccb-6e6c-600b-d355aa41e361"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "registrations", new Guid("0192f5a0-0000-7000-8000-000000000103"), "Active" },
                    { new Guid("14ba89f1-40ce-df27-a481-3f5c573d9479"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "audit", new Guid("0192f5a0-0000-7000-8000-000000000103"), "Active" },
                    { new Guid("1e37b419-4a50-d3bc-1fb2-1e4d7426ac88"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "flags", null, null, "health", new Guid("0192f5a0-0000-7000-8000-000000000105"), "Active" },
                    { new Guid("1fccb418-c024-1652-9963-29d048c5a7da"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "registrations", new Guid("0192f5a0-0000-7000-8000-000000000107"), "Active" },
                    { new Guid("256b9a28-d5c5-788a-7ac1-b19802b1a933"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "full", null, null, "health", new Guid("0192f5a0-0000-7000-8000-000000000107"), "Active" },
                    { new Guid("284af6be-b37f-376e-bdf6-dc268d8bd08b"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "audit", new Guid("0192f5a0-0000-7000-8000-000000000101"), "Active" },
                    { new Guid("29c5148c-83c5-6b70-f3b8-1175d8e21714"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "custody", new Guid("0192f5a0-0000-7000-8000-000000000101"), "Active" },
                    { new Guid("410ea06f-dfda-4a02-0a5d-9a03a3104e81"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "custody", new Guid("0192f5a0-0000-7000-8000-000000000105"), "Active" },
                    { new Guid("47dece1e-8e5f-f394-d691-c9725b7c79d5"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "edit", null, null, "people", new Guid("0192f5a0-0000-7000-8000-000000000102"), "Active" },
                    { new Guid("4ae88772-49f2-93dd-400d-4a847c2134bb"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "edit", null, null, "config", new Guid("0192f5a0-0000-7000-8000-000000000101"), "Active" },
                    { new Guid("54f8a129-df85-5e94-3b9d-58493c41fd0b"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "registrations", new Guid("0192f5a0-0000-7000-8000-000000000111"), "Active" },
                    { new Guid("6321fc24-cf72-e319-7ecd-2dd95f8ea1c7"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "act", null, null, "custody", new Guid("0192f5a0-0000-7000-8000-000000000110"), "Active" },
                    { new Guid("634697df-6339-c8a7-976e-b1fc333a964a"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "custody", new Guid("0192f5a0-0000-7000-8000-000000000102"), "Active" },
                    { new Guid("6ed92f0a-9335-b01f-4752-7e458574a70d"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "config", new Guid("0192f5a0-0000-7000-8000-000000000106"), "Active" },
                    { new Guid("70e697c3-c730-3e5c-b4aa-39c810298268"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "edit", null, null, "communications", new Guid("0192f5a0-0000-7000-8000-000000000102"), "Active" },
                    { new Guid("75d75ad6-04f7-8154-91b6-e37163494804"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "registrations", new Guid("0192f5a0-0000-7000-8000-000000000110"), "Active" },
                    { new Guid("7c88e691-48e3-ff18-0e32-463e0b718b89"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "draft", null, null, "pricing", new Guid("0192f5a0-0000-7000-8000-000000000102"), "Active" },
                    { new Guid("8c8e088c-bd14-5a11-15c7-d06928d6665f"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "registrations", new Guid("0192f5a0-0000-7000-8000-000000000105"), "Active" },
                    { new Guid("8de72685-8563-396c-da00-07a9b3e192ea"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "edit", null, null, "money", new Guid("0192f5a0-0000-7000-8000-000000000103"), "Active" },
                    { new Guid("98f4cf62-28c9-4c30-6eaf-8a47b3a4c31e"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "registrations", new Guid("0192f5a0-0000-7000-8000-000000000112"), "Active" },
                    { new Guid("b21e3699-1331-2aa7-155b-d5746811cefb"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "act", null, null, "custody", new Guid("0192f5a0-0000-7000-8000-000000000104"), "Active" },
                    { new Guid("b9b336cc-76da-a122-c729-cfa58c7d6064"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "edit", null, null, "staff", new Guid("0192f5a0-0000-7000-8000-000000000101"), "Active" },
                    { new Guid("bf1287bb-52ae-44a9-7aa6-c62b2d21d12d"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "flags", null, null, "health", new Guid("0192f5a0-0000-7000-8000-000000000110"), "Active" },
                    { new Guid("c159effd-34d2-884e-1e6e-2643d46a9138"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "edit", null, null, "registrations", new Guid("0192f5a0-0000-7000-8000-000000000102"), "Active" },
                    { new Guid("c1e8f79b-b001-51fd-fcd0-9cd8d63a5526"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "edit", null, null, "registrations", new Guid("0192f5a0-0000-7000-8000-000000000109"), "Active" },
                    { new Guid("c425ff71-fbf4-181d-661b-7ca58fa061ed"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "edit", null, null, "people", new Guid("0192f5a0-0000-7000-8000-000000000101"), "Active" },
                    { new Guid("c6c6f1f4-973b-cdfa-d91d-9a0095fca995"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "edit", null, null, "registrations", new Guid("0192f5a0-0000-7000-8000-000000000101"), "Active" },
                    { new Guid("d0c7e7e4-47a9-4576-8545-ee3251cad6b5"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "flags", null, null, "health", new Guid("0192f5a0-0000-7000-8000-000000000104"), "Active" },
                    { new Guid("e008c84a-f40b-dc1c-2398-e49004996e57"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "approve", null, null, "pricing", new Guid("0192f5a0-0000-7000-8000-000000000103"), "Active" },
                    { new Guid("f150fb55-0f5b-6d1d-ad93-9ac4b3dc5443"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "flags", null, null, "health", new Guid("0192f5a0-0000-7000-8000-000000000109"), "Active" },
                    { new Guid("f16436d7-c60a-b732-bad0-5155dbefce05"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "flags", null, null, "health", new Guid("0192f5a0-0000-7000-8000-000000000101"), "Active" },
                    { new Guid("f9539b07-a5fb-630c-b1c0-13d699b262da"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "audit", new Guid("0192f5a0-0000-7000-8000-000000000112"), "Active" },
                    { new Guid("fc2492b4-3a15-f27b-01db-1768c5d38871"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "view", null, null, "registrations", new Guid("0192f5a0-0000-7000-8000-000000000106"), "Active" },
                    { new Guid("fd698888-dc80-59d5-3625-b67caf21f642"), null, null, new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), "flags", null, null, "health", new Guid("0192f5a0-0000-7000-8000-000000000102"), "Active" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_logs_actor_account_id_date_created",
                table: "access_logs",
                columns: new[] { "actor_account_id", "date_created" });

            migrationBuilder.CreateIndex(
                name: "ix_account_claims_person_id",
                table: "account_claims",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_claims_token_hash",
                table: "account_claims",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounts_email",
                table: "accounts",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounts_google_sub",
                table: "accounts",
                column: "google_sub",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounts_person_id",
                table: "accounts",
                column: "person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_action",
                table: "audit_events",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_id",
                table: "audit_events",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_auth_sessions_account_id",
                table: "auth_sessions",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_auth_sessions_token_hash",
                table: "auth_sessions",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_authorized_collectors_child_person_id",
                table: "authorized_collectors",
                column: "child_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_camp_decisions_decided_by",
                table: "camp_decisions",
                column: "decided_by");

            migrationBuilder.CreateIndex(
                name: "ix_camp_decisions_registration_id_date_created",
                table: "camp_decisions",
                columns: new[] { "registration_id", "date_created" });

            migrationBuilder.CreateIndex(
                name: "ix_consent_records_answered_by",
                table: "consent_records",
                column: "answered_by");

            migrationBuilder.CreateIndex(
                name: "ix_consent_records_person_id",
                table: "consent_records",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_records_registration_id_kind_date_created",
                table: "consent_records",
                columns: new[] { "registration_id", "kind", "date_created" });

            migrationBuilder.CreateIndex(
                name: "ix_devices_account_id",
                table: "devices",
                column: "account_id");

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

            migrationBuilder.CreateIndex(
                name: "ix_edition_capabilities_edition_id_capability",
                table: "edition_capabilities",
                columns: new[] { "edition_id", "capability" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_edition_contents_edition_id_key",
                table: "edition_contents",
                columns: new[] { "edition_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_verifications_account_id",
                table: "email_verifications",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_verifications_token_hash",
                table: "email_verifications",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_emergency_contacts_person_id",
                table: "emergency_contacts",
                column: "person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_days_edition_id_day",
                table: "event_days",
                columns: new[] { "edition_id", "day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_editions_code",
                table: "event_editions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_editions_slug",
                table: "event_editions",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_editions_template_id",
                table: "event_editions",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_editions_venue_id",
                table: "event_editions",
                column: "venue_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_templates_code",
                table: "event_templates",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_templates_organization_id",
                table: "event_templates",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_guardian_authorizations_dependant_person_id",
                table: "guardian_authorizations",
                column: "dependant_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_guardian_authorizations_guardian_person_id_dependant_person_",
                table: "guardian_authorizations",
                columns: new[] { "guardian_person_id", "dependant_person_id", "live_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_health_disclosures_registration_id",
                table: "health_disclosures",
                column: "registration_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invitations_invited_by",
                table: "invitations",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_registration_id",
                table: "invitations",
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_token_hash",
                table: "invitations",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mfa_factors_account_id",
                table: "mfa_factors",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_person_relationships_from_person_id_to_person_id_kind",
                table: "person_relationships",
                columns: new[] { "from_person_id", "to_person_id", "kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_person_relationships_to_person_id",
                table: "person_relationships",
                column: "to_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_policy_versions_document_id_version",
                table: "policy_versions",
                columns: new[] { "document_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rate_plan_versions_approved_by",
                table: "rate_plan_versions",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "ix_rate_plan_versions_drafted_by",
                table: "rate_plan_versions",
                column: "drafted_by");

            migrationBuilder.CreateIndex(
                name: "ix_rate_plan_versions_one_published",
                table: "rate_plan_versions",
                columns: new[] { "plan_id", "published_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rate_plan_versions_plan_id_version",
                table: "rate_plan_versions",
                columns: new[] { "plan_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rate_plans_edition_id",
                table: "rate_plans",
                column: "edition_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rate_rules_version_id",
                table: "rate_rules",
                column: "version_id");

            migrationBuilder.CreateIndex(
                name: "ix_reference_counters_edition_id",
                table: "reference_counters",
                column: "edition_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_actors_person_id",
                table: "registration_actors",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_actors_registration_id_person_id_role",
                table: "registration_actors",
                columns: new[] { "registration_id", "person_id", "role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_status_histories_registration_id",
                table: "registration_status_histories",
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "ix_registrations_edition_id",
                table: "registrations",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "ix_registrations_one_active",
                table: "registrations",
                columns: new[] { "person_id", "edition_id", "active_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registrations_reference",
                table: "registrations",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_grants_account_id",
                table: "role_grants",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_grants_edition_id",
                table: "role_grants",
                column: "edition_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_grants_granted_by",
                table: "role_grants",
                column: "granted_by");

            migrationBuilder.CreateIndex(
                name: "ix_role_grants_role_id",
                table: "role_grants",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_role_id_resource",
                table: "role_permissions",
                columns: new[] { "role_id", "resource" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_key",
                table: "roles",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_verified_contacts_kind_value",
                table: "verified_contacts",
                columns: new[] { "kind", "value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_verified_contacts_person_id",
                table: "verified_contacts",
                column: "person_id");

            // ---- What EF cannot express: the row lock behind the audit hash chain, and the triggers behind the append-only and
            // immutability rules. (MySQL has no partial indexes, so "one active/published/live row" is a stored column in the model.)
            // Needs the TRIGGER privilege, and with binary logging on, SUPER or log_bin_trust_function_creators=1.
            migrationBuilder.Sql("CREATE TABLE audit_chain_lock (id TINYINT NOT NULL PRIMARY KEY);");
            migrationBuilder.Sql("INSERT INTO audit_chain_lock (id) VALUES (1);");

            // A consent answer is evidence of what a guardian agreed to and when; a change is a new row.
            foreach (var table in new[] { "audit_events", "access_logs", "registration_status_histories", "consent_records" })
            {
                migrationBuilder.Sql($@"CREATE TRIGGER {table}_no_update BEFORE UPDATE ON {table}
  FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = '{table} is append-only';");
                migrationBuilder.Sql($@"CREATE TRIGGER {table}_no_delete BEFORE DELETE ON {table}
  FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = '{table} is append-only';");
            }

            // Published and retired rate versions are immutable; only published -> retired is allowed.
            migrationBuilder.Sql(@"CREATE TRIGGER rate_plan_versions_guard_update BEFORE UPDATE ON rate_plan_versions
  FOR EACH ROW
BEGIN
  IF OLD.state IN ('Published', 'Retired') AND NOT (OLD.state = 'Published' AND NEW.state = 'Retired') THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'rate version is immutable once published';
  END IF;
END");
            migrationBuilder.Sql(@"CREATE TRIGGER rate_plan_versions_guard_delete BEFORE DELETE ON rate_plan_versions
  FOR EACH ROW
BEGIN
  IF OLD.state <> 'Draft' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'only draft rate versions can be deleted';
  END IF;
END");

            // Rules can only change while their version is a draft.
            foreach (var (op, row) in new[] { ("INSERT", "NEW"), ("UPDATE", "NEW"), ("DELETE", "OLD") })
                migrationBuilder.Sql($@"CREATE TRIGGER rate_rules_guard_{op.ToLowerInvariant()} BEFORE {op} ON rate_rules
  FOR EACH ROW
BEGIN
  DECLARE s VARCHAR(20);
  SELECT state INTO s FROM rate_plan_versions WHERE id = {row}.version_id;
  IF s <> 'Draft' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'rules can only change while the version is a draft';
  END IF;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var trigger in new[]
            {
                "rate_rules_guard_insert", "rate_rules_guard_update", "rate_rules_guard_delete",
                "rate_plan_versions_guard_update", "rate_plan_versions_guard_delete",
                "audit_events_no_update", "audit_events_no_delete", "access_logs_no_update", "access_logs_no_delete",
                "registration_status_histories_no_update", "registration_status_histories_no_delete",
                "consent_records_no_update", "consent_records_no_delete"
            })
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {trigger};");
            migrationBuilder.Sql("DROP TABLE IF EXISTS audit_chain_lock;");
            migrationBuilder.DropTable(
                name: "access_logs");

            migrationBuilder.DropTable(
                name: "account_claims");

            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "auth_sessions");

            migrationBuilder.DropTable(
                name: "authorized_collectors");

            migrationBuilder.DropTable(
                name: "camp_decisions");

            migrationBuilder.DropTable(
                name: "consent_records");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "duplicate_dismissals");

            migrationBuilder.DropTable(
                name: "edition_capabilities");

            migrationBuilder.DropTable(
                name: "edition_contents");

            migrationBuilder.DropTable(
                name: "email_verifications");

            migrationBuilder.DropTable(
                name: "emergency_contacts");

            migrationBuilder.DropTable(
                name: "event_days");

            migrationBuilder.DropTable(
                name: "guardian_authorizations");

            migrationBuilder.DropTable(
                name: "health_disclosures");

            migrationBuilder.DropTable(
                name: "invitations");

            migrationBuilder.DropTable(
                name: "mfa_factors");

            migrationBuilder.DropTable(
                name: "person_relationships");

            migrationBuilder.DropTable(
                name: "policy_versions");

            migrationBuilder.DropTable(
                name: "rate_rules");

            migrationBuilder.DropTable(
                name: "reference_counters");

            migrationBuilder.DropTable(
                name: "registration_actors");

            migrationBuilder.DropTable(
                name: "registration_status_histories");

            migrationBuilder.DropTable(
                name: "role_grants");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "verified_contacts");

            migrationBuilder.DropTable(
                name: "rate_plan_versions");

            migrationBuilder.DropTable(
                name: "registrations");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "rate_plans");

            migrationBuilder.DropTable(
                name: "people");

            migrationBuilder.DropTable(
                name: "event_editions");

            migrationBuilder.DropTable(
                name: "event_templates");

            migrationBuilder.DropTable(
                name: "venues");

            migrationBuilder.DropTable(
                name: "organizations");
        }
    }
}
