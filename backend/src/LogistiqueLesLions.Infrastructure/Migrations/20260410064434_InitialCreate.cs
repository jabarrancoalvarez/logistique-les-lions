using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "messaging");

            migrationBuilder.EnsureSchema(
                name: "vehicles");

            migrationBuilder.EnsureSchema(
                name: "compliance");

            migrationBuilder.EnsureSchema(
                name: "users");

            migrationBuilder.CreateTable(
                name: "countries",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    name_es = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    flag_emoji = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "EUR"),
                    is_eu_member = table.Column<bool>(type: "boolean", nullable: false),
                    supports_import = table.Column<bool>(type: "boolean", nullable: false),
                    supports_export = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "country_requirements",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    destination_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    document_types_json = table.Column<string>(type: "jsonb", nullable: false),
                    homologation_required = table.Column<bool>(type: "boolean", nullable: false),
                    customs_rate_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    vat_rate_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    road_tax_formula = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    technical_inspection_required = table.Column<bool>(type: "boolean", nullable: false),
                    estimated_processing_cost_eur = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    estimated_days = table.Column<int>(type: "integer", nullable: false),
                    notes_es = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    notes_en = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    last_updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_country_requirements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customs_tariffs",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    destination_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    hs_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tariff_rate_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customs_tariffs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_templates",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    template_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    instructions_es = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    instructions_en = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    official_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    issuing_authority = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    estimated_cost_eur = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    estimated_days = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "homologation_requirements",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    vehicle_category = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    year_from = table.Column<int>(type: "integer", nullable: true),
                    year_to = table.Column<int>(type: "integer", nullable: true),
                    emission_standard = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    required_modifications = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    estimated_cost_eur = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    certifying_body = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes_es = table.Column<string>(type: "text", nullable: true),
                    notes_en = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_homologation_requirements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    refresh_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    refresh_token_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    company_vat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_makes",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_popular = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_makes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_models",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    make_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    body_types = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_models", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_models_vehicle_makes_make_id",
                        column: x => x.make_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicle_makes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description_es = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    description_en = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    make_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: true),
                    year = table.Column<int>(type: "integer", nullable: false),
                    mileage = table.Column<int>(type: "integer", nullable: true),
                    condition = table.Column<string>(type: "text", nullable: false),
                    body_type = table.Column<string>(type: "text", nullable: true),
                    fuel_type = table.Column<string>(type: "text", nullable: true),
                    transmission = table.Column<string>(type: "text", nullable: true),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    vin = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "EUR"),
                    price_negotiable = table.Column<bool>(type: "boolean", nullable: false),
                    country_origin = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    is_export_ready = table.Column<bool>(type: "boolean", nullable: false),
                    specs = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    features = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    views_count = table.Column<int>(type: "integer", nullable: false),
                    favorites_count = table.Column<int>(type: "integer", nullable: false),
                    contacts_count = table.Column<int>(type: "integer", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sold_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicles_user_profiles_seller_id",
                        column: x => x.seller_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vehicles_vehicle_makes_make_id",
                        column: x => x.make_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicle_makes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vehicles_vehicle_models_model_id",
                        column: x => x.model_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicle_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "conversations",
                schema: "messaging",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_archived_by_buyer = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived_by_seller = table.Column<bool>(type: "boolean", nullable: false),
                    last_message_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversations", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversations_user_profiles_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conversations_user_profiles_seller_id",
                        column: x => x.seller_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conversations_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "import_export_processes",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    destination_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    process_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    estimated_cost_eur = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    actual_cost_eur = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notes = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    completion_percent = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_export_processes", x => x.id);
                    table.ForeignKey(
                        name: "fk_import_export_processes_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saved_vehicles",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saved_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "fk_saved_vehicles_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_documents",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    file_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_documents_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_histories",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    event_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    mileage_at_event = table.Column<int>(type: "integer", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_histories_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_images",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    thumbnail_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    alt_text = table.Column<string>(type: "text", nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "webp"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_images_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "messaging",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_messages_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalSchema: "messaging",
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_messages_user_profiles_sender_id",
                        column: x => x.sender_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "process_documents",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    responsible_party = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    required_by_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    file_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    template_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    official_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    instructions_es = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    estimated_cost_eur = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_process_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_process_documents_import_export_processes_process_id",
                        column: x => x.process_id,
                        principalSchema: "compliance",
                        principalTable: "import_export_processes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "vehicles",
                table: "countries",
                columns: new[] { "id", "code", "created_at", "created_by", "currency", "deleted_at", "deleted_by", "display_order", "flag_emoji", "is_active", "is_eu_member", "name_en", "name_es", "supports_export", "supports_import", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "ES", new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4198), new TimeSpan(0, 0, 0, 0, 0)), null, "EUR", null, null, 1, "🇪🇸", true, true, "Spain", "España", true, true, new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4342), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "DE", new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4565), new TimeSpan(0, 0, 0, 0, 0)), null, "EUR", null, null, 2, "🇩🇪", true, true, "Germany", "Alemania", true, true, new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4566), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "FR", new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4589), new TimeSpan(0, 0, 0, 0, 0)), null, "EUR", null, null, 3, "🇫🇷", true, true, "France", "Francia", true, true, new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4589), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "IT", new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4592), new TimeSpan(0, 0, 0, 0, 0)), null, "EUR", null, null, 4, "🇮🇹", true, true, "Italy", "Italia", true, true, new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4592), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "PT", new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4595), new TimeSpan(0, 0, 0, 0, 0)), null, "EUR", null, null, 5, "🇵🇹", true, true, "Portugal", "Portugal", true, true, new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4595), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "NL", new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4598), new TimeSpan(0, 0, 0, 0, 0)), null, "EUR", null, null, 6, "🇳🇱", true, true, "Netherlands", "Países Bajos", true, true, new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4598), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "BE", new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4616), new TimeSpan(0, 0, 0, 0, 0)), null, "EUR", null, null, 7, "🇧🇪", true, true, "Belgium", "Bélgica", true, true, new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4617), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "GB", new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4620), new TimeSpan(0, 0, 0, 0, 0)), null, "GBP", null, null, 8, "🇬🇧", true, false, "United Kingdom", "Reino Unido", true, true, new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4620), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "US", new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4623), new TimeSpan(0, 0, 0, 0, 0)), null, "USD", null, null, 9, "🇺🇸", true, false, "United States", "Estados Unidos", false, true, new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4624), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "MA", new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4628), new TimeSpan(0, 0, 0, 0, 0)), null, "MAD", null, null, 10, "🇲🇦", true, false, "Morocco", "Marruecos", true, false, new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4628), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("10000000-0000-0000-0000-000000000011"), "JP", new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4632), new TimeSpan(0, 0, 0, 0, 0)), null, "JPY", null, null, 11, "🇯🇵", true, false, "Japan", "Japón", false, true, new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4632), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "CH", new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4651), new TimeSpan(0, 0, 0, 0, 0)), null, "CHF", null, null, 12, "🇨🇭", true, false, "Switzerland", "Suiza", true, true, new DateTimeOffset(new DateTime(2026, 4, 10, 6, 44, 32, 29, DateTimeKind.Unspecified).AddTicks(4651), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.InsertData(
                schema: "compliance",
                table: "country_requirements",
                columns: new[] { "id", "created_at", "created_by", "customs_rate_percent", "deleted_at", "deleted_by", "destination_country", "document_types_json", "estimated_days", "estimated_processing_cost_eur", "homologation_required", "last_updated_at", "notes_en", "notes_es", "origin_country", "road_tax_formula", "technical_inspection_required", "updated_at", "updated_by", "vat_rate_percent" },
                values: new object[,]
                {
                    { new Guid("10000001-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0m, null, null, "DE", "[\"FichaTecnica\",\"COC\",\"TituloPropiedad\",\"Itv\"]", 15, 300m, false, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Transferencia intra-UE. Sin aranceles. COC europeo requerido.", "ES", null, false, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0m },
                    { new Guid("10000001-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0m, null, null, "ES", "[\"FichaTecnica\",\"COC\",\"TituloPropiedad\"]", 10, 280m, false, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Transferencia intra-UE. ITV en España obligatoria al matricular.", "DE", null, false, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0m },
                    { new Guid("10000001-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0m, null, null, "FR", "[\"FichaTecnica\",\"COC\",\"TituloPropiedad\"]", 10, 250m, false, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Transferencia intra-UE Francia. Control técnico (CT) requerido al matricular.", "DE", null, false, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 0m },
                    { new Guid("10000001-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 6.5m, null, null, "ES", "[\"TituloPropiedad\",\"DeclaracionAduana\",\"PagoAranceles\",\"Homologacion\",\"Itv\",\"SeguroImportacion\"]", 45, 1500m, true, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Arancel UE 6.5% sobre valor CIF. IVA 21% sobre valor + arancel. Homologación obligatoria para vehículos no certificados ECE.", "US", null, true, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 21m },
                    { new Guid("10000001-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 6.5m, null, null, "ES", "[\"TituloPropiedad\",\"DeclaracionAduana\",\"PagoAranceles\",\"Homologacion\",\"Itv\",\"SeguroImportacion\"]", 60, 2000m, true, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Importación desde Japón. Requiere homologación y adaptación de luces/faros al estándar europeo.", "JP", null, true, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 21m },
                    { new Guid("10000001-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2.5m, null, null, "MA", "[\"TituloPropiedad\",\"FichaTecnica\",\"DeclaracionAduana\",\"SeguroImportacion\",\"InspeccionTecnica\"]", 30, 800m, true, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Exportación a Marruecos. Requiere vistazos en aduana de Melilla/Ceuta o flete marítimo. Vehículo debe tener menos de 5 años.", "ES", null, true, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 20m },
                    { new Guid("10000001-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 6.5m, null, null, "GB", "[\"TituloPropiedad\",\"FichaTecnica\",\"DeclaracionAduana\",\"MOT\",\"SeguroImportacion\"]", 30, 1200m, true, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Post-Brexit: se aplican aranceles UE-UK. Conversión a conducción por la izquierda no obligatoria pero faros deben ajustarse.", "ES", null, true, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 20m }
                });

            migrationBuilder.CreateIndex(
                name: "ix_conversations_buyer_id_seller_id_vehicle_id",
                schema: "messaging",
                table: "conversations",
                columns: new[] { "buyer_id", "seller_id", "vehicle_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conversations_seller_id",
                schema: "messaging",
                table: "conversations",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_vehicle_id",
                schema: "messaging",
                table: "conversations",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_countries_code",
                schema: "vehicles",
                table: "countries",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_country_requirements_origin_country_destination_country",
                schema: "compliance",
                table: "country_requirements",
                columns: new[] { "origin_country", "destination_country" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customs_tariffs_origin_country_destination_country_hs_code",
                schema: "compliance",
                table: "customs_tariffs",
                columns: new[] { "origin_country", "destination_country", "hs_code" });

            migrationBuilder.CreateIndex(
                name: "ix_customs_tariffs_valid_from",
                schema: "compliance",
                table: "customs_tariffs",
                column: "valid_from");

            migrationBuilder.CreateIndex(
                name: "ix_document_templates_country_document_type",
                schema: "compliance",
                table: "document_templates",
                columns: new[] { "country", "document_type" });

            migrationBuilder.CreateIndex(
                name: "ix_homologation_requirements_destination_country",
                schema: "compliance",
                table: "homologation_requirements",
                column: "destination_country");

            migrationBuilder.CreateIndex(
                name: "ix_homologation_requirements_destination_country_vehicle_categ",
                schema: "compliance",
                table: "homologation_requirements",
                columns: new[] { "destination_country", "vehicle_category" });

            migrationBuilder.CreateIndex(
                name: "ix_import_export_processes_buyer_id",
                schema: "compliance",
                table: "import_export_processes",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "ix_import_export_processes_seller_id",
                schema: "compliance",
                table: "import_export_processes",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "ix_import_export_processes_status",
                schema: "compliance",
                table: "import_export_processes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_import_export_processes_vehicle_id",
                schema: "compliance",
                table: "import_export_processes",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_id",
                schema: "messaging",
                table: "messages",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_messages_sender_id",
                schema: "messaging",
                table: "messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "ix_process_documents_process_id",
                schema: "compliance",
                table: "process_documents",
                column: "process_id");

            migrationBuilder.CreateIndex(
                name: "ix_process_documents_status",
                schema: "compliance",
                table: "process_documents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_saved_vehicles_user_id",
                schema: "vehicles",
                table: "saved_vehicles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_saved_vehicles_user_id_vehicle_id",
                schema: "vehicles",
                table: "saved_vehicles",
                columns: new[] { "user_id", "vehicle_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saved_vehicles_vehicle_id",
                schema: "vehicles",
                table: "saved_vehicles",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_email",
                schema: "users",
                table: "user_profiles",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_documents_vehicle_id_type",
                schema: "vehicles",
                table: "vehicle_documents",
                columns: new[] { "vehicle_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_histories_event_date",
                schema: "vehicles",
                table: "vehicle_histories",
                column: "event_date");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_histories_vehicle_id",
                schema: "vehicles",
                table: "vehicle_histories",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_images_vehicle_id_is_primary",
                schema: "vehicles",
                table: "vehicle_images",
                columns: new[] { "vehicle_id", "is_primary" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_images_vehicle_id_sort_order",
                schema: "vehicles",
                table: "vehicle_images",
                columns: new[] { "vehicle_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_makes_name",
                schema: "vehicles",
                table: "vehicle_makes",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_models_make_id_name",
                schema: "vehicles",
                table: "vehicle_models",
                columns: new[] { "make_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_active_featured",
                schema: "vehicles",
                table: "vehicles",
                columns: new[] { "status", "is_featured", "deleted_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_country_origin",
                schema: "vehicles",
                table: "vehicles",
                column: "country_origin");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_created_at",
                schema: "vehicles",
                table: "vehicles",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_make_id",
                schema: "vehicles",
                table: "vehicles",
                column: "make_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_model_id",
                schema: "vehicles",
                table: "vehicles",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_price_currency",
                schema: "vehicles",
                table: "vehicles",
                columns: new[] { "price", "currency" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_seller_id",
                schema: "vehicles",
                table: "vehicles",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_slug",
                schema: "vehicles",
                table: "vehicles",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_status",
                schema: "vehicles",
                table: "vehicles",
                column: "status");

            // tsvector computed column and GIN index (raw SQL — not mappable via EF)
            migrationBuilder.Sql(@"
                ALTER TABLE vehicles.vehicles
                    ADD COLUMN IF NOT EXISTS search_vector tsvector
                    GENERATED ALWAYS AS (
                        to_tsvector('spanish', coalesce(title,'') || ' ' || coalesce(description_es,''))
                    ) STORED;
                CREATE INDEX IF NOT EXISTS ix_vehicles_search_vector
                    ON vehicles.vehicles USING GIN(search_vector);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "countries",
                schema: "vehicles");

            migrationBuilder.DropTable(
                name: "country_requirements",
                schema: "compliance");

            migrationBuilder.DropTable(
                name: "customs_tariffs",
                schema: "compliance");

            migrationBuilder.DropTable(
                name: "document_templates",
                schema: "compliance");

            migrationBuilder.DropTable(
                name: "homologation_requirements",
                schema: "compliance");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "process_documents",
                schema: "compliance");

            migrationBuilder.DropTable(
                name: "saved_vehicles",
                schema: "vehicles");

            migrationBuilder.DropTable(
                name: "vehicle_documents",
                schema: "vehicles");

            migrationBuilder.DropTable(
                name: "vehicle_histories",
                schema: "vehicles");

            migrationBuilder.DropTable(
                name: "vehicle_images",
                schema: "vehicles");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "import_export_processes",
                schema: "compliance");

            migrationBuilder.DropTable(
                name: "vehicles",
                schema: "vehicles");

            migrationBuilder.DropTable(
                name: "user_profiles",
                schema: "users");

            migrationBuilder.DropTable(
                name: "vehicle_models",
                schema: "vehicles");

            migrationBuilder.DropTable(
                name: "vehicle_makes",
                schema: "vehicles");
        }
    }
}
