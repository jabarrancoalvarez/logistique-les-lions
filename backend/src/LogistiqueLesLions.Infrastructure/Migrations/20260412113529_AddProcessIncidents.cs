using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "process_incidents",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolution = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_process_incidents", x => x.id);
                    table.ForeignKey(
                        name: "fk_process_incidents_import_export_processes_process_id",
                        column: x => x.process_id,
                        principalSchema: "compliance",
                        principalTable: "import_export_processes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2143), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2301), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2499), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2500), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2504), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2505), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2508), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2508), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2566), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2567), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2570), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2570), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2583), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2584), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2586), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2587), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2589), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2590), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2593), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2594), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2596), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2596), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2599), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 35, 28, 226, DateTimeKind.Unspecified).AddTicks(2599), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000001"),
                column: "road_tax_formula",
                value: null);

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000002"),
                column: "road_tax_formula",
                value: null);

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000003"),
                column: "road_tax_formula",
                value: null);

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000004"),
                column: "road_tax_formula",
                value: null);

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000005"),
                column: "road_tax_formula",
                value: null);

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000006"),
                column: "road_tax_formula",
                value: null);

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000007"),
                column: "road_tax_formula",
                value: null);

            migrationBuilder.CreateIndex(
                name: "ix_process_incidents_process_id",
                schema: "compliance",
                table: "process_incidents",
                column: "process_id");

            migrationBuilder.CreateIndex(
                name: "ix_process_incidents_status",
                schema: "compliance",
                table: "process_incidents",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "process_incidents",
                schema: "compliance");

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 123, DateTimeKind.Unspecified).AddTicks(9539), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 123, DateTimeKind.Unspecified).AddTicks(9781), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(48), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(49), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(55), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(56), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(61), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(63), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(67), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(68), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(72), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(73), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(79), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(79), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(83), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(84), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(93), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(94), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(98), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(99), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(103), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(103), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(108), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 10, 3, 46, 124, DateTimeKind.Unspecified).AddTicks(108), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000001"),
                column: "road_tax_formula",
                value: null);

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000002"),
                column: "road_tax_formula",
                value: null);

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000003"),
                column: "road_tax_formula",
                value: null);

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000004"),
                column: "road_tax_formula",
                value: null);

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000005"),
                column: "road_tax_formula",
                value: null);

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000006"),
                column: "road_tax_formula",
                value: null);

            migrationBuilder.UpdateData(
                schema: "compliance",
                table: "country_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000001-0000-0000-0000-000000000007"),
                column: "road_tax_formula",
                value: null);
        }
    }
}
