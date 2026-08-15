using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DemandesDeVehicules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Secuencia propia para las referencias "YD00248": no debe compartirse con
            // la de los anuncios, o las dos numeraciones se entrelazarían.
            migrationBuilder.Sql(
                "CREATE SEQUENCE IF NOT EXISTS vehicles.vehicle_request_reference_seq START WITH 1 INCREMENT BY 1;");

            migrationBuilder.CreateTable(
                name: "vehicle_requests",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    make_id = table.Column<Guid>(type: "uuid", nullable: true),
                    make_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    year_from = table.Column<int>(type: "integer", nullable: true),
                    year_to = table.Column<int>(type: "integer", nullable: true),
                    max_mileage = table.Column<int>(type: "integer", nullable: true),
                    fuel_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    transmission = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    body_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    important_equipment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    max_budget = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    origin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_requests_user_profiles_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vehicle_requests_vehicle_makes_make_id",
                        column: x => x.make_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicle_makes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_request_messages",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_from_admin = table.Column<bool>(type: "boolean", nullable: false),
                    is_internal_note = table.Column<bool>(type: "boolean", nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_request_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_request_messages_user_profiles_sender_id",
                        column: x => x.sender_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_vehicle_request_messages_vehicle_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicle_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_request_proposals",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    make_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    year = table.Column<int>(type: "integer", nullable: true),
                    mileage = table.Column<int>(type: "integer", nullable: true),
                    estimated_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    country_of_origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    photo_urls = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    external_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_seen_by_user = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_request_proposals", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_request_proposals_vehicle_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicle_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vehicle_request_proposals_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_request_messages_request_id_created_at",
                schema: "vehicles",
                table: "vehicle_request_messages",
                columns: new[] { "request_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_request_messages_sender_id",
                schema: "vehicles",
                table: "vehicle_request_messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_request_proposals_request_id",
                schema: "vehicles",
                table: "vehicle_request_proposals",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_request_proposals_vehicle_id",
                schema: "vehicles",
                table: "vehicle_request_proposals",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_requests_make_id",
                schema: "vehicles",
                table: "vehicle_requests",
                column: "make_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_requests_public_reference",
                schema: "vehicles",
                table: "vehicle_requests",
                column: "public_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_requests_status_created_at",
                schema: "vehicles",
                table: "vehicle_requests",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_requests_user_id",
                schema: "vehicles",
                table: "vehicle_requests",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS vehicles.vehicle_request_reference_seq;");

            migrationBuilder.DropTable(
                name: "vehicle_request_messages",
                schema: "vehicles");

            migrationBuilder.DropTable(
                name: "vehicle_request_proposals",
                schema: "vehicles");

            migrationBuilder.DropTable(
                name: "vehicle_requests",
                schema: "vehicles");
        }
    }
}
