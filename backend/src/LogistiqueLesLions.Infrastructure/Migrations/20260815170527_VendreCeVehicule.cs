using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VendreCeVehicule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "listed_vehicle_id",
                schema: "garage",
                table: "garage_vehicles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "vehicle_transparency",
                schema: "garage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    garage_vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    show_maintenance_history = table.Column<bool>(type: "boolean", nullable: false),
                    show_maintenance_details = table.Column<bool>(type: "boolean", nullable: false),
                    show_mileage_evolution = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_transparency", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_transparency_garage_vehicles_garage_vehicle_id",
                        column: x => x.garage_vehicle_id,
                        principalSchema: "garage",
                        principalTable: "garage_vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vehicle_transparency_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shared_maintenance_records",
                schema: "garage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transparency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    share_invoice = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shared_maintenance_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_shared_maintenance_records_maintenance_records_maintenance_",
                        column: x => x.maintenance_record_id,
                        principalSchema: "garage",
                        principalTable: "maintenance_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shared_maintenance_records_vehicle_transparencies_transpare",
                        column: x => x.transparency_id,
                        principalSchema: "garage",
                        principalTable: "vehicle_transparency",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shared_maintenance_records_maintenance_record_id",
                schema: "garage",
                table: "shared_maintenance_records",
                column: "maintenance_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_shared_maintenance_records_transparency_id_maintenance_reco",
                schema: "garage",
                table: "shared_maintenance_records",
                columns: new[] { "transparency_id", "maintenance_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_transparency_garage_vehicle_id",
                schema: "garage",
                table: "vehicle_transparency",
                column: "garage_vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_transparency_vehicle_id",
                schema: "garage",
                table: "vehicle_transparency",
                column: "vehicle_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shared_maintenance_records",
                schema: "garage");

            migrationBuilder.DropTable(
                name: "vehicle_transparency",
                schema: "garage");

            migrationBuilder.DropColumn(
                name: "listed_vehicle_id",
                schema: "garage",
                table: "garage_vehicles");
        }
    }
}
