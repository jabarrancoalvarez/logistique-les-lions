using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EstimationDeValeur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "valuation_settings",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    min_comparables = table.Column<int>(type: "integer", nullable: false),
                    max_listing_age_days = table.Column<int>(type: "integer", nullable: false),
                    year_band = table.Column<int>(type: "integer", nullable: false),
                    mileage_band_km = table.Column<int>(type: "integer", nullable: false),
                    range_spread = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    snapshot_interval_days = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "valuation_snapshots",
                schema: "garage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estimated_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    low_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    high_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    comparable_count = table.Column<int>(type: "integer", nullable: false),
                    mileage = table.Column<int>(type: "integer", nullable: true),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_valuation_snapshots_garage_vehicles_garage_vehicle_id",
                        column: x => x.garage_vehicle_id,
                        principalSchema: "garage",
                        principalTable: "garage_vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "vehicles",
                table: "valuation_settings",
                columns: new[] { "id", "created_at", "created_by", "deleted_at", "deleted_by", "max_listing_age_days", "mileage_band_km", "min_comparables", "range_spread", "snapshot_interval_days", "updated_at", "updated_by", "year_band" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 365, 30000, 5, 0.05m, 30, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2 });

            migrationBuilder.CreateIndex(
                name: "ix_valuation_snapshots_garage_vehicle_id_captured_at",
                schema: "garage",
                table: "valuation_snapshots",
                columns: new[] { "garage_vehicle_id", "captured_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "valuation_settings",
                schema: "vehicles");

            migrationBuilder.DropTable(
                name: "valuation_snapshots",
                schema: "garage");
        }
    }
}
