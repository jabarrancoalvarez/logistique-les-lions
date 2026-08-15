using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModeleVehiculeYoonUAuto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ────────────────────────────────────────────────────────────────────
            // Migración ajustada a mano sobre el andamiaje de EF:
            //  · `specs` se vaciaba antes de trasladar potencia y cilindrada.
            //  · `public_reference` se creaba con "" en todas las filas y a
            //    continuación un índice UNIQUE, que habría fallado.
            //  · Los enums pasan de los valores del producto anterior (Active,
            //    Gasoline, Sedan…) a los de la especificación (Actif, Essence,
            //    Berline…): hay que reescribir las filas existentes.
            // ────────────────────────────────────────────────────────────────────

            // La descripción del vendedor pasa a ser un único texto en francés.
            migrationBuilder.RenameColumn(
                name: "description_es",
                schema: "vehicles",
                table: "vehicles",
                newName: "description");

            migrationBuilder.DropColumn(
                name: "description_en",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.AlterColumn<string>(
                name: "transmission",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "fuel_type",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "country_origin",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "SN",
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "condition",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "body_type",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customs_status",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "district",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "doors",
                schema: "vehicles",
                table: "vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "drivetrain",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "engine_displacement_cc",
                schema: "vehicles",
                table: "vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "engine_name",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "power_cv",
                schema: "vehicles",
                table: "vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "public_reference",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "published_at",
                schema: "vehicles",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "region",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reserved_at",
                schema: "vehicles",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "seats",
                schema: "vehicles",
                table: "vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "version",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // ─── Traspaso de datos ──────────────────────────────────────────────

            // Potencia y cilindrada vivían sueltas dentro del JSONB `specs`; ahora son
            // columnas tipadas porque el Marketplace filtra por ellas.
            migrationBuilder.Sql(@"
                UPDATE vehicles.vehicles
                SET power_cv = (specs->>'powerCv')::int
                WHERE specs IS NOT NULL
                  AND specs->>'powerCv' ~ '^[0-9]+$';
                ");

            migrationBuilder.Sql(@"
                UPDATE vehicles.vehicles
                SET engine_displacement_cc = (specs->>'displacementCc')::int
                WHERE specs IS NOT NULL
                  AND specs->>'displacementCc' ~ '^[0-9]+$';
                ");

            migrationBuilder.DropColumn(
                name: "specs",
                schema: "vehicles",
                table: "vehicles");

            // El equipamiento pasa del JSONB `features` al catálogo vehicle_equipments.
            // Los anuncios anteriores no guardaban un formato estable, por lo que no hay
            // nada fiable que trasladar.
            migrationBuilder.DropColumn(
                name: "features",
                schema: "vehicles",
                table: "vehicles");

            // Enums: valores del producto anterior → valores de la especificación.
            migrationBuilder.Sql("""
                UPDATE vehicles.vehicles SET status = CASE status
                    WHEN 'Reviewing' THEN 'Brouillon'
                    WHEN 'Active'    THEN 'Actif'
                    WHEN 'Paused'    THEN 'EnPause'
                    WHEN 'Sold'      THEN 'Vendu'
                    WHEN 'Rejected'  THEN 'Archive'
                    WHEN 'Expired'   THEN 'Archive'
                    ELSE status END;
                """);

            migrationBuilder.Sql("""
                UPDATE vehicles.vehicles SET fuel_type = CASE fuel_type
                    WHEN 'Gasoline'     THEN 'Essence'
                    WHEN 'Diesel'       THEN 'Diesel'
                    WHEN 'Electric'     THEN 'Electrique'
                    WHEN 'Hybrid'       THEN 'Hybride'
                    WHEN 'PluginHybrid' THEN 'HybrideRechargeable'
                    WHEN 'LPG'          THEN 'Autre'
                    WHEN 'CNG'          THEN 'Autre'
                    WHEN 'Hydrogen'     THEN 'Autre'
                    ELSE fuel_type END
                WHERE fuel_type IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE vehicles.vehicles SET transmission = CASE transmission
                    WHEN 'Manual'        THEN 'Manuel'
                    WHEN 'Automatic'     THEN 'Automatique'
                    WHEN 'SemiAutomatic' THEN 'Automatique'
                    WHEN 'CVT'           THEN 'Automatique'
                    ELSE transmission END
                WHERE transmission IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE vehicles.vehicles SET body_type = CASE body_type
                    WHEN 'Sedan'       THEN 'Berline'
                    WHEN 'Hatchback'   THEN 'Citadine'
                    WHEN 'SUV'         THEN 'Suv'
                    WHEN 'Coupe'       THEN 'Coupe'
                    WHEN 'Convertible' THEN 'Cabriolet'
                    WHEN 'Wagon'       THEN 'Break'
                    WHEN 'Van'         THEN 'Utilitaire'
                    WHEN 'Truck'       THEN 'Utilitaire'
                    WHEN 'Pickup'      THEN 'PickUp'
                    WHEN 'Minivan'     THEN 'Monospace'
                    WHEN 'Motorcycle'  THEN 'Autre'
                    WHEN 'Other'       THEN 'Autre'
                    ELSE body_type END
                WHERE body_type IS NOT NULL;
                """);

            // Referencia pública: una secuencia garantiza que dos altas simultáneas no
            // obtengan el mismo número. Arranca en 10000 para que las referencias tengan
            // siempre la misma longitud.
            migrationBuilder.Sql(
                "CREATE SEQUENCE IF NOT EXISTS vehicles.vehicle_reference_seq START WITH 10000 INCREMENT BY 1;");

            migrationBuilder.Sql("""
                UPDATE vehicles.vehicles
                SET public_reference = 'YU' || LPAD(nextval('vehicles.vehicle_reference_seq')::text, 5, '0')
                WHERE public_reference = '';
                """);

            // Un anuncio ya activo se considera publicado desde su creación.
            migrationBuilder.Sql("""
                UPDATE vehicles.vehicles
                SET published_at = created_at
                WHERE status IN ('Actif', 'Reserve', 'Vendu') AND published_at IS NULL;
                """);

            migrationBuilder.CreateTable(
                name: "vehicle_equipments",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_equipments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_price_history",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_price_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_price_history_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_equipment_links",
                schema: "vehicles",
                columns: table => new
                {
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_equipment_links", x => new { x.vehicle_id, x.equipment_id });
                    table.ForeignKey(
                        name: "fk_vehicle_equipment_links_vehicle_equipments_equipment_id",
                        column: x => x.equipment_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicle_equipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vehicle_equipment_links_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "vehicles",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8066), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8221), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8364), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8365), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8369), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8369), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8383), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8384), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8386), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8386), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8389), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8389), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8392), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8392), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8395), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8395), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8397), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8398), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8400), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8400), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8403), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8403), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8407), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 57, 44, 568, DateTimeKind.Unspecified).AddTicks(8408), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                schema: "vehicles",
                table: "vehicle_equipments",
                columns: new[] { "id", "code", "created_at", "created_by", "deleted_at", "deleted_by", "display_order", "is_active", "name", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "CLIMATISATION", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 1, true, "Climatisation", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "BLUETOOTH", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 2, true, "Bluetooth", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "GPS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 3, true, "Navigation / GPS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000004"), "CAMERA_RECUL", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 4, true, "Caméra de recul", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000005"), "RADAR_STATION", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 5, true, "Radar de stationnement", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000006"), "TOIT_OUVRANT", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 6, true, "Toit ouvrant", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000007"), "INTERIEUR_CUIR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 7, true, "Intérieur cuir", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000008"), "ISOFIX", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 8, true, "ISOFIX", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000009"), "PHARES_LED", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 9, true, "Phares LED", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000010"), "REGULATEUR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 10, true, "Régulateur de vitesse", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000011"), "JANTES_ALLIAGE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 11, true, "Jantes alliage", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000012"), "VITRES_ELEC", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 12, true, "Vitres électriques", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000013"), "VERROUILLAGE", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 13, true, "Verrouillage centralisé", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000014"), "ABS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 14, true, "ABS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000015"), "AIRBAGS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 15, true, "Airbags", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("20000000-0000-0000-0000-000000000016"), "DIRECTION_ASSIST", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, 16, true, "Direction assistée", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            // Primer punto del histórico para los anuncios ya existentes: sin él,
            // «Évolution du prix» no tendría precio inicial con el que comparar.
            migrationBuilder.Sql("""
                INSERT INTO vehicles.vehicle_price_history (id, vehicle_id, price, changed_at)
                SELECT gen_random_uuid(), id, price, created_at
                FROM vehicles.vehicles;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_customs_status",
                schema: "vehicles",
                table: "vehicles",
                column: "customs_status");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_public_reference",
                schema: "vehicles",
                table: "vehicles",
                column: "public_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_region_city",
                schema: "vehicles",
                table: "vehicles",
                columns: new[] { "region", "city" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_equipment_links_equipment_id",
                schema: "vehicles",
                table: "vehicle_equipment_links",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_equipments_code",
                schema: "vehicles",
                table: "vehicle_equipments",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_price_history_vehicle_id_changed_at",
                schema: "vehicles",
                table: "vehicle_price_history",
                columns: new[] { "vehicle_id", "changed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Los enums vuelven a los valores del modelo anterior.
            migrationBuilder.Sql("""
                UPDATE vehicles.vehicles SET status = CASE status
                    WHEN 'Brouillon' THEN 'Reviewing'
                    WHEN 'Actif'     THEN 'Active'
                    WHEN 'EnPause'   THEN 'Paused'
                    WHEN 'Reserve'   THEN 'Active'
                    WHEN 'Vendu'     THEN 'Sold'
                    WHEN 'Archive'   THEN 'Expired'
                    ELSE status END;
                """);

            migrationBuilder.Sql("""
                UPDATE vehicles.vehicles SET fuel_type = CASE fuel_type
                    WHEN 'Essence'             THEN 'Gasoline'
                    WHEN 'Electrique'          THEN 'Electric'
                    WHEN 'Hybride'             THEN 'Hybrid'
                    WHEN 'HybrideRechargeable' THEN 'PluginHybrid'
                    WHEN 'Autre'               THEN 'Gasoline'
                    ELSE fuel_type END
                WHERE fuel_type IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE vehicles.vehicles SET transmission = CASE transmission
                    WHEN 'Manuel'      THEN 'Manual'
                    WHEN 'Automatique' THEN 'Automatic'
                    ELSE transmission END
                WHERE transmission IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE vehicles.vehicles SET body_type = CASE body_type
                    WHEN 'Berline'     THEN 'Sedan'
                    WHEN 'Citadine'    THEN 'Hatchback'
                    WHEN 'Suv'         THEN 'SUV'
                    WHEN 'Cabriolet'   THEN 'Convertible'
                    WHEN 'Break'       THEN 'Wagon'
                    WHEN 'Utilitaire'  THEN 'Van'
                    WHEN 'PickUp'      THEN 'Pickup'
                    WHEN 'Monospace'   THEN 'Minivan'
                    WHEN 'Autre'       THEN 'Other'
                    ELSE body_type END
                WHERE body_type IS NOT NULL;
                """);

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS vehicles.vehicle_reference_seq;");

            migrationBuilder.DropTable(
                name: "vehicle_equipment_links",
                schema: "vehicles");

            migrationBuilder.DropTable(
                name: "vehicle_price_history",
                schema: "vehicles");

            migrationBuilder.DropTable(
                name: "vehicle_equipments",
                schema: "vehicles");

            migrationBuilder.DropIndex(
                name: "ix_vehicles_customs_status",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "ix_vehicles_public_reference",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "ix_vehicles_region_city",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "customs_status",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "district",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "doors",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "drivetrain",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "engine_displacement_cc",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "engine_name",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "power_cv",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "public_reference",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "published_at",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "region",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "reserved_at",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "seats",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "vehicles",
                table: "vehicles",
                newName: "description_es");

            migrationBuilder.AlterColumn<string>(
                name: "transmission",
                schema: "vehicles",
                table: "vehicles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "vehicles",
                table: "vehicles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "fuel_type",
                schema: "vehicles",
                table: "vehicles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "country_origin",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldDefaultValue: "SN");

            migrationBuilder.AlterColumn<string>(
                name: "condition",
                schema: "vehicles",
                table: "vehicles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "body_type",
                schema: "vehicles",
                table: "vehicles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_en",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "features",
                schema: "vehicles",
                table: "vehicles",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specs",
                schema: "vehicles",
                table: "vehicles",
                type: "jsonb",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(6771), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(6922), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7068), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7069), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7072), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7073), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7076), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7076), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7079), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7079), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7096), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7097), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7100), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7100), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7103), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7103), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7106), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7107), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7109), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7110), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7112), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7113), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7116), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7116), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
