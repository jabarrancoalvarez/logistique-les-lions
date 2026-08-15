using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MonGarage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "garage");

            migrationBuilder.CreateTable(
                name: "garage_vehicles",
                schema: "garage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    make_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    year = table.Column<int>(type: "integer", nullable: false),
                    mileage = table.Column<int>(type: "integer", nullable: true),
                    fuel_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    transmission = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    body_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    power_cv = table.Column<int>(type: "integer", nullable: true),
                    engine_displacement_cc = table.Column<int>(type: "integer", nullable: true),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    registration_plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    vin = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                    purchase_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    purchase_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    source_contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_garage_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "fk_garage_vehicles_vehicle_makes_make_id",
                        column: x => x.make_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicle_makes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_garage_vehicles_vehicle_models_model_id",
                        column: x => x.model_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicle_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "garage_vehicle_images",
                schema: "garage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    thumbnail_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_garage_vehicle_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_garage_vehicle_images_garage_vehicles_garage_vehicle_id",
                        column: x => x.garage_vehicle_id,
                        principalSchema: "garage",
                        principalTable: "garage_vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_garage_vehicle_images_garage_vehicle_id",
                schema: "garage",
                table: "garage_vehicle_images",
                column: "garage_vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_garage_vehicles_make_id",
                schema: "garage",
                table: "garage_vehicles",
                column: "make_id");

            migrationBuilder.CreateIndex(
                name: "ix_garage_vehicles_model_id",
                schema: "garage",
                table: "garage_vehicles",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "ix_garage_vehicles_source_contract_id",
                schema: "garage",
                table: "garage_vehicles",
                column: "source_contract_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_garage_vehicles_user_id",
                schema: "garage",
                table: "garage_vehicles",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "garage_vehicle_images",
                schema: "garage");

            migrationBuilder.DropTable(
                name: "garage_vehicles",
                schema: "garage");
        }
    }
}
