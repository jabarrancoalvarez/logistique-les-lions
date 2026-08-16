using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SessionsMultiples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_refresh_tokens",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_agent = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_refresh_tokens_user_profiles_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_token",
                schema: "users",
                table: "user_refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_user_id",
                schema: "users",
                table: "user_refresh_tokens",
                column: "user_id");

            // Las sesiones abiertas viven en dos columnas de user_profiles. Si no se
            // trasladan, este despliegue expulsa a todo el mundo sin motivo.
            migrationBuilder.Sql(@"
                INSERT INTO users.user_refresh_tokens
                    (id, user_id, token, expires_at, revoked_at, user_agent,
                     created_at, updated_at, deleted_at)
                SELECT gen_random_uuid(), id, refresh_token, refresh_token_expires_at,
                       NULL, NULL, NOW(), NOW(), NULL
                FROM users.user_profiles
                WHERE refresh_token IS NOT NULL
                  AND refresh_token_expires_at IS NOT NULL
                  AND refresh_token_expires_at > NOW();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_refresh_tokens",
                schema: "users");
        }
    }
}
