using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessTrackingCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tracking_code",
                schema: "compliance",
                table: "import_export_processes",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            // Backfill: cualquier proceso existente recibe un código aleatorio único
            // antes de aplicar el índice UNIQUE. Usamos substr+md5+random para
            // garantizar unicidad sin necesidad de extensiones extra.
            migrationBuilder.Sql(@"
                UPDATE compliance.import_export_processes
                SET tracking_code = upper(substr(md5(random()::text || id::text), 1, 12))
                WHERE tracking_code = '';
            ");

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

            migrationBuilder.CreateIndex(
                name: "ix_import_export_processes_tracking_code",
                schema: "compliance",
                table: "import_export_processes",
                column: "tracking_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_import_export_processes_tracking_code",
                schema: "compliance",
                table: "import_export_processes");

            migrationBuilder.DropColumn(
                name: "tracking_code",
                schema: "compliance",
                table: "import_export_processes");

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(4164), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(4978), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5500), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5501), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5509), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5510), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5518), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5519), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5523), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5523), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5541), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5541), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5548), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5548), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5558), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5558), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5564), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5564), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5568), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5569), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5607), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5607), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5611), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 9, 39, 29, 707, DateTimeKind.Unspecified).AddTicks(5611), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
