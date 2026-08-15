using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfigurationEtPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "loyalty_points",
                schema: "users",
                table: "user_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "new_value",
                schema: "users",
                table: "admin_actions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "old_value",
                schema: "users",
                table: "admin_actions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "feature_flags",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_flags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_point_entries",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    origin = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_point_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_loyalty_point_entries_user_profiles_admin_id",
                        column: x => x.admin_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loyalty_point_entries_user_profiles_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_settings",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    comparator_max_vehicles = table.Column<int>(type: "integer", nullable: false),
                    points_per_verified_sale = table.Column<int>(type: "integer", nullable: false),
                    listing_freshness_days = table.Column<int>(type: "integer", nullable: false),
                    max_images_per_listing = table.Column<int>(type: "integer", nullable: false),
                    legal_terms_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    legal_terms_updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "upcoming_features",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_upcoming_features", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feature_interests",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_interests", x => x.id);
                    table.ForeignKey(
                        name: "fk_feature_interests_upcoming_features_feature_id",
                        column: x => x.feature_id,
                        principalSchema: "users",
                        principalTable: "upcoming_features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_feature_interests_user_profiles_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "feature_flags",
                columns: new[] { "id", "created_at", "created_by", "deleted_at", "deleted_by", "description", "is_enabled", "key", "label", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("30000001-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Affiche « Bonne affaire / Prix correct / Prix élevé » sur les annonces.", true, "price_indicator", "Indicateur de prix", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("30000001-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Estimation statistique de la valeur des véhicules de Mon Garage.", true, "vehicle_valuation", "Valeur estimée", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("30000001-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Comparaison de plusieurs véhicules côte à côte.", true, "comparator", "Comparateur", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("30000001-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Demandes d'importation gérées par l'équipe.", true, "vehicle_requests", "Trouvez-moi cette voiture", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("30000001-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Fonctionnalités à venir et bouton « Ça m'intéresse ».", true, "upcoming_features", "Prochainement", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "platform_settings",
                columns: new[] { "id", "comparator_max_vehicles", "created_at", "created_by", "deleted_at", "deleted_by", "legal_terms_updated_at", "legal_terms_version", "listing_freshness_days", "max_images_per_listing", "points_per_verified_sale", "updated_at", "updated_by" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000003"), 3, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "1.0", 60, 20, 100, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });

            migrationBuilder.InsertData(
                schema: "users",
                table: "upcoming_features",
                columns: new[] { "id", "code", "created_at", "created_by", "deleted_at", "deleted_by", "description", "display_order", "is_active", "name", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("30000002-0000-0000-0000-000000000001"), "STOCK", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Gérer un parc de véhicules et publier plusieurs annonces d'un coup.", 1, true, "Gestion de stock", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("30000002-0000-0000-0000-000000000002"), "WHATSAPP", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Recevoir les messages des acheteurs directement sur WhatsApp.", 2, true, "WhatsApp Business", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("30000002-0000-0000-0000-000000000003"), "CRM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Suivre ses contacts, ses relances et ses ventes.", 3, true, "CRM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("30000002-0000-0000-0000-000000000004"), "TENDANCES", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Voir l'évolution des prix et de la demande par modèle.", 4, true, "Tendances du marché", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("30000002-0000-0000-0000-000000000005"), "OUTILS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "Aide à la fixation du prix et à la rédaction des annonces.", 5, true, "Outils intelligents", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_admin_actions_created_at",
                schema: "users",
                table: "admin_actions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_feature_flags_key",
                schema: "users",
                table: "feature_flags",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_feature_interests_feature_id_user_id",
                schema: "users",
                table: "feature_interests",
                columns: new[] { "feature_id", "user_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_feature_interests_user_id",
                schema: "users",
                table: "feature_interests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_point_entries_admin_id",
                schema: "users",
                table: "loyalty_point_entries",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_point_entries_user_id_created_at",
                schema: "users",
                table: "loyalty_point_entries",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_upcoming_features_code",
                schema: "users",
                table: "upcoming_features",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feature_flags",
                schema: "users");

            migrationBuilder.DropTable(
                name: "feature_interests",
                schema: "users");

            migrationBuilder.DropTable(
                name: "loyalty_point_entries",
                schema: "users");

            migrationBuilder.DropTable(
                name: "platform_settings",
                schema: "users");

            migrationBuilder.DropTable(
                name: "upcoming_features",
                schema: "users");

            migrationBuilder.DropIndex(
                name: "ix_admin_actions_created_at",
                schema: "users",
                table: "admin_actions");

            migrationBuilder.DropColumn(
                name: "loyalty_points",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "new_value",
                schema: "users",
                table: "admin_actions");

            migrationBuilder.DropColumn(
                name: "old_value",
                schema: "users",
                table: "admin_actions");
        }
    }
}
