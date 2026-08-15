using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Contrats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Secuencia propia para las referencias "YC00125": identifica al contrato y,
            // con él, a la venta verificada. No se comparte con anuncios ni solicitudes.
            migrationBuilder.Sql(
                "CREATE SEQUENCE IF NOT EXISTS messaging.contract_reference_seq START WITH 1 INCREMENT BY 1;");

            migrationBuilder.CreateTable(
                name: "contracts",
                schema: "messaging",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    negotiation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    vehicle_make = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    vehicle_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    vehicle_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    vehicle_year = table.Column<int>(type: "integer", nullable: false),
                    vehicle_mileage = table.Column<int>(type: "integer", nullable: true),
                    vehicle_vin = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                    registration_plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    vehicle_reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    agreed_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    sale_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    seller_legal_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    seller_id_document = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    seller_address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    buyer_legal_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    buyer_id_document = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    buyer_address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    validated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    change_request_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    change_requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contracts", x => x.id);
                    table.ForeignKey(
                        name: "fk_contracts_negotiations_negotiation_id",
                        column: x => x.negotiation_id,
                        principalSchema: "messaging",
                        principalTable: "negotiations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contracts_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contracts_negotiation_id",
                schema: "messaging",
                table: "contracts",
                column: "negotiation_id",
                unique: true,
                filter: "status <> 'Annule'");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_public_reference",
                schema: "messaging",
                table: "contracts",
                column: "public_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contracts_status_created_at",
                schema: "messaging",
                table: "contracts",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_contracts_vehicle_id",
                schema: "messaging",
                table: "contracts",
                column: "vehicle_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contracts",
                schema: "messaging");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS messaging.contract_reference_seq;");
        }
    }
}
