using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    changed_columns = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

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

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_name_entity_id",
                schema: "vehicles",
                table: "audit_logs",
                columns: new[] { "entity_name", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_timestamp",
                schema: "vehicles",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                schema: "vehicles",
                table: "audit_logs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "vehicles");

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9565), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9696), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9820), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9821), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9824), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9825), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9850), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9851), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9853), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9854), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9868), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9868), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9871), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9871), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9884), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9885), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9888), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9888), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9891), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9891), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9894), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9894), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9900), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 10, 6, 54, 39, 356, DateTimeKind.Unspecified).AddTicks(9900), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
