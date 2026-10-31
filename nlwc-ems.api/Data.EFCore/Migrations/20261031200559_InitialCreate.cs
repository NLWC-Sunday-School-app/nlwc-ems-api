using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "access_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    subject_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    edition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_access_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    seq = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actor_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    record_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    edition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    detail = table.Column<string>(type: "text", nullable: false),
                    prev_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_events", x => x.seq);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("pk_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    given_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    family_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    marital_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    country = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("pk_people", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "policy_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_policy_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "venues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_venues", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    config = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("pk_event_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_templates_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    invited_email = table.Column<string>(type: "citext", nullable: false),
                    state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_account_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_account_claims_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    google_sub = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failed_logins = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_accounts", x => x.id);
                    table.CheckConstraint("ck_accounts_credential", "password_hash IS NOT NULL OR google_sub IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_accounts_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "authorized_collectors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    relationship = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_encrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    valid_days = table.Column<List<DateOnly>>(type: "date[]", nullable: true),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_authorized_collectors", x => x.id);
                    table.ForeignKey(
                        name: "fk_authorized_collectors_people_child_person_id",
                        column: x => x.child_person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "emergency_contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    relationship = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_encrypted = table.Column<byte[]>(type: "bytea", nullable: false),
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
                    table.PrimaryKey("pk_emergency_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_emergency_contacts_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guardian_authorizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dependant_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    declared_relationship = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                });

            migrationBuilder.CreateTable(
                name: "person_relationships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "verified_contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    value = table.Column<string>(type: "citext", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_verified_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_verified_contacts_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("pk_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_editions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    timezone = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false),
                    age_reference_date = table.Column<DateOnly>(type: "date", nullable: false),
                    camp_min_age = table.Column<int>(type: "integer", nullable: true),
                    camp_max_age = table.Column<int>(type: "integer", nullable: true),
                    camp_capacity = table.Column<int>(type: "integer", nullable: true),
                    pickup_window_start = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    pickup_window_end = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    registration_window = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    live_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "auth_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    surface = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    idle_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    absolute_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    mfa_verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_auth_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_auth_sessions_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_devices_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email_verifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_email_verifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_verifications_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mfa_factors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    secret_encrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_mfa_factors", x => x.id);
                    table.ForeignKey(
                        name: "fk_mfa_factors_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "edition_capabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    edition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capability = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_edition_capabilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_edition_capabilities_event_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "event_editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "edition_contents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    edition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    body = table.Column<string>(type: "jsonb", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_edition_contents", x => x.id);
                    table.ForeignKey(
                        name: "fk_edition_contents_event_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "event_editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    edition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("pk_event_days", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_days_event_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "event_editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rate_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    edition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("pk_rate_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_rate_plans_event_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "event_editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reference_counters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    edition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("pk_reference_counters", x => x.id);
                    table.ForeignKey(
                        name: "fk_reference_counters_event_editions_edition_id",
                        column: x => x.edition_id,
                        principalTable: "event_editions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    edition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    participation_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    age_on_reference_date = table.Column<int>(type: "integer", nullable: true),
                    age_band = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    draft = table.Column<string>(type: "jsonb", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "role_grants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    edition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                });

            migrationBuilder.CreateTable(
                name: "rate_plan_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    drafted_by = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                });

            migrationBuilder.CreateTable(
                name: "invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                });

            migrationBuilder.CreateTable(
                name: "registration_actors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "registration_status_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    to_state = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    actor_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_registration_status_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_registration_status_histories_registrations_registration_id",
                        column: x => x.registration_id,
                        principalTable: "registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rate_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    age_min = table.Column<int>(type: "integer", nullable: false),
                    age_max = table.Column<int>(type: "integer", nullable: true),
                    participation_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    accommodation_category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount_kobo = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("pk_rate_rules", x => x.id);
                    table.CheckConstraint("ck_rate_rules_ages", "age_min >= 0 AND (age_max IS NULL OR age_max >= age_min)");
                    table.CheckConstraint("ck_rate_rules_kind", "(kind = 'Amount' AND amount_kobo IS NOT NULL AND amount_kobo > 0) OR (kind = 'Free' AND amount_kobo IS NULL)");
                    table.ForeignKey(
                        name: "fk_rate_rules_rate_plan_versions_version_id",
                        column: x => x.version_id,
                        principalTable: "rate_plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                column: "account_id",
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_auth_sessions_token_hash",
                table: "auth_sessions",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_authorized_collectors_child_person_id",
                table: "authorized_collectors",
                column: "child_person_id",
                filter: "removed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_devices_account_id",
                table: "devices",
                column: "account_id");

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
                name: "ix_guardian_authorizations_guardian_person_id_dependant_person",
                table: "guardian_authorizations",
                columns: new[] { "guardian_person_id", "dependant_person_id" },
                unique: true,
                filter: "revoked_at IS NULL");

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
                column: "plan_id",
                unique: true,
                filter: "state = 'Published'");

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
                columns: new[] { "person_id", "edition_id" },
                unique: true,
                filter: "state NOT IN ('Cancelled', 'Rejected', 'Expired')");

            migrationBuilder.CreateIndex(
                name: "ix_registrations_reference",
                table: "registrations",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_grants_account_id",
                table: "role_grants",
                column: "account_id",
                filter: "revoked_at IS NULL");

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

            // ---- What EF cannot express: an expression index and the triggers behind the append-only / immutability rules.
            migrationBuilder.Sql(@"CREATE INDEX ix_people_name_trgm ON people USING gin ((given_name || ' ' || family_name) gin_trgm_ops);");

            migrationBuilder.Sql(@"
CREATE FUNCTION forbid_mutation() RETURNS trigger LANGUAGE plpgsql AS
$$ BEGIN RAISE EXCEPTION '% is append-only', TG_TABLE_NAME; END $$;");
            foreach (var table in new[] { "audit_events", "access_logs", "registration_status_histories" })
                migrationBuilder.Sql($@"CREATE TRIGGER {table}_append_only BEFORE UPDATE OR DELETE ON {table}
  FOR EACH ROW EXECUTE FUNCTION forbid_mutation();");

            // Published and retired rate versions are immutable; only published -> retired is allowed.
            migrationBuilder.Sql(@"
CREATE FUNCTION guard_rate_version() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP = 'DELETE' THEN
    IF OLD.state <> 'Draft' THEN RAISE EXCEPTION 'only draft rate versions can be deleted'; END IF;
    RETURN OLD;
  END IF;
  IF OLD.state IN ('Published', 'Retired') AND NOT (OLD.state = 'Published' AND NEW.state = 'Retired') THEN
    RAISE EXCEPTION 'rate version is immutable once published';
  END IF;
  RETURN NEW;
END $$;");
            migrationBuilder.Sql(@"CREATE TRIGGER rate_plan_versions_guard BEFORE UPDATE OR DELETE ON rate_plan_versions
  FOR EACH ROW EXECUTE FUNCTION guard_rate_version();");

            migrationBuilder.Sql(@"
CREATE FUNCTION guard_rate_rule() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v uuid; s text;
BEGIN
  v := CASE WHEN TG_OP = 'DELETE' THEN OLD.version_id ELSE NEW.version_id END;
  SELECT state INTO s FROM rate_plan_versions WHERE id = v;
  IF s <> 'Draft' THEN RAISE EXCEPTION 'rules can only change while the version is a draft'; END IF;
  RETURN CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END;
END $$;");
            migrationBuilder.Sql(@"CREATE TRIGGER rate_rules_guard BEFORE INSERT OR UPDATE OR DELETE ON rate_rules
  FOR EACH ROW EXECUTE FUNCTION guard_rate_rule();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS rate_rules_guard ON rate_rules;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS rate_plan_versions_guard ON rate_plan_versions;");
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
                name: "devices");

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

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS guard_rate_rule();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS guard_rate_version();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS forbid_mutation();");
        }
    }
}
