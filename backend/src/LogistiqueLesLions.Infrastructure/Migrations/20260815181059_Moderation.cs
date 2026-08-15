using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Moderation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Secuencia propia para las referencias "SG00042": es lo que el usuario y el
            // administrador citan al hablar de un signalement.
            migrationBuilder.Sql(
                "CREATE SEQUENCE IF NOT EXISTS messaging.report_reference_seq START WITH 1 INCREMENT BY 1;");

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "messaging",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reported_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    evidence = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    resolution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    handled_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_reports_user_profiles_handled_by_admin_id",
                        column: x => x.handled_by_admin_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_reports_user_profiles_reported_user_id",
                        column: x => x.reported_user_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_reports_user_profiles_reporter_id",
                        column: x => x.reporter_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reports_handled_by_admin_id",
                schema: "messaging",
                table: "reports",
                column: "handled_by_admin_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_public_reference",
                schema: "messaging",
                table: "reports",
                column: "public_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reports_reported_user_id",
                schema: "messaging",
                table: "reports",
                column: "reported_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_reporter_id",
                schema: "messaging",
                table: "reports",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_status_created_at",
                schema: "messaging",
                table: "reports",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_target_type_target_id_status",
                schema: "messaging",
                table: "reports",
                columns: new[] { "target_type", "target_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reports",
                schema: "messaging");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS messaging.report_reference_seq;");
        }
    }
}
