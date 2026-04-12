using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsAndMarketplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "marketplace");

            migrationBuilder.CreateTable(
                name: "service_partners",
                schema: "marketplace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    countries_csv = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    reviews_count = table.Column<int>(type: "integer", nullable: false),
                    base_price_eur = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_service_partners", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_notifications",
                schema: "messaging",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_user_notifications", x => x.id);
                });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(991), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1163), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1320), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1320), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1327), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1327), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1330), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1331), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1334), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1334), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1357), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1357), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1361), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1361), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1364), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1364), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1372), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1372), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1375), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1376), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1379), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1379), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1382), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 4, 12, 11, 52, 30, 146, DateTimeKind.Unspecified).AddTicks(1383), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "ix_service_partners_slug",
                schema: "marketplace",
                table: "service_partners",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_partners_type_is_active",
                schema: "marketplace",
                table: "service_partners",
                columns: new[] { "type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_user_notifications_user_id_created_at",
                schema: "messaging",
                table: "user_notifications",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_user_notifications_user_id_is_read",
                schema: "messaging",
                table: "user_notifications",
                columns: new[] { "user_id", "is_read" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_partners",
                schema: "marketplace");

            migrationBuilder.DropTable(
                name: "user_notifications",
                schema: "messaging");

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
        }
    }
}
