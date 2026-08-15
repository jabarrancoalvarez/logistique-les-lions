using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Negociations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ────────────────────────────────────────────────────────────────────
            // EF propuso BORRAR `conversations` y CREAR `negotiations`, lo que habría
            // destruido todas las conversaciones existentes. Se sustituye por un
            // renombrado: es la misma entidad, que ahora pasa a ser el agregado raíz de
            // la Etapa 2 y gana estado y cronología.
            //
            // `RenameTable` conserva índices y restricciones con su nombre antiguo, así
            // que se renombran también para que el esquema quede coherente.
            // ────────────────────────────────────────────────────────────────────

            migrationBuilder.RenameTable(
                name: "conversations",
                schema: "messaging",
                newName: "negotiations",
                newSchema: "messaging");

            migrationBuilder.Sql(@"
                ALTER TABLE messaging.negotiations
                    RENAME CONSTRAINT pk_conversations TO pk_negotiations;
                ALTER TABLE messaging.negotiations
                    RENAME CONSTRAINT fk_conversations_user_profiles_buyer_id
                                   TO fk_negotiations_user_profiles_buyer_id;
                ALTER TABLE messaging.negotiations
                    RENAME CONSTRAINT fk_conversations_user_profiles_seller_id
                                   TO fk_negotiations_user_profiles_seller_id;
                ALTER TABLE messaging.negotiations
                    RENAME CONSTRAINT fk_conversations_vehicles_vehicle_id
                                   TO fk_negotiations_vehicles_vehicle_id;

                ALTER INDEX messaging.ix_conversations_buyer_id_seller_id_vehicle_id
                    RENAME TO ix_negotiations_buyer_id_seller_id_vehicle_id;
                ALTER INDEX messaging.ix_conversations_seller_id
                    RENAME TO ix_negotiations_seller_id;
                ALTER INDEX messaging.ix_conversations_vehicle_id
                    RENAME TO ix_negotiations_vehicle_id;
            ");

            migrationBuilder.RenameColumn(
                name: "conversation_id",
                schema: "messaging",
                table: "messages",
                newName: "negotiation_id");

            migrationBuilder.RenameIndex(
                name: "ix_messages_conversation_id",
                schema: "messaging",
                table: "messages",
                newName: "ix_messages_negotiation_id");

            migrationBuilder.Sql(@"
                ALTER TABLE messaging.messages
                    RENAME CONSTRAINT fk_messages_conversations_conversation_id
                                   TO fk_messages_negotiations_negotiation_id;
            ");

            // ─── Columnas nuevas de la negociación ─────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "messaging",
                table: "negotiations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                // Las conversaciones existentes pasan a ser negociaciones en curso.
                defaultValue: "EnCours");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_activity_at",
                schema: "messaging",
                table: "negotiations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at",
                schema: "messaging",
                table: "negotiations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_negotiations_status_last_activity_at",
                schema: "messaging",
                table: "negotiations",
                columns: new[] { "status", "last_activity_at" });

            // ─── Cronología ────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "negotiation_events",
                schema: "messaging",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    negotiation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_negotiation_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_negotiation_events_negotiations_negotiation_id",
                        column: x => x.negotiation_id,
                        principalSchema: "messaging",
                        principalTable: "negotiations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_negotiation_events_user_profiles_actor_id",
                        column: x => x.actor_id,
                        principalSchema: "users",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_negotiation_events_actor_id",
                schema: "messaging",
                table: "negotiation_events",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_negotiation_events_negotiation_id_sequence",
                schema: "messaging",
                table: "negotiation_events",
                columns: new[] { "negotiation_id", "sequence" });

            // ─── Traspaso de datos ─────────────────────────────────────────────
            // La última actividad de una conversación previa es su último mensaje.
            migrationBuilder.Sql(@"
                UPDATE messaging.negotiations
                SET last_activity_at = COALESCE(last_message_at, created_at);
            ");

            // Cada negociación existente arranca su cronología con el hito de apertura.
            migrationBuilder.Sql(@"
                INSERT INTO messaging.negotiation_events
                    (id, negotiation_id, sequence, type, actor_id, amount, created_at, updated_at)
                SELECT gen_random_uuid(), id, 1, 'ConversationStarted', buyer_id, NULL,
                       created_at, created_at
                FROM messaging.negotiations;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "negotiation_events",
                schema: "messaging");

            migrationBuilder.DropIndex(
                name: "ix_negotiations_status_last_activity_at",
                schema: "messaging",
                table: "negotiations");

            migrationBuilder.DropColumn(
                name: "status", schema: "messaging", table: "negotiations");
            migrationBuilder.DropColumn(
                name: "last_activity_at", schema: "messaging", table: "negotiations");
            migrationBuilder.DropColumn(
                name: "closed_at", schema: "messaging", table: "negotiations");

            migrationBuilder.Sql(@"
                ALTER TABLE messaging.messages
                    RENAME CONSTRAINT fk_messages_negotiations_negotiation_id
                                   TO fk_messages_conversations_conversation_id;

                ALTER TABLE messaging.negotiations
                    RENAME CONSTRAINT pk_negotiations TO pk_conversations;
                ALTER TABLE messaging.negotiations
                    RENAME CONSTRAINT fk_negotiations_user_profiles_buyer_id
                                   TO fk_conversations_user_profiles_buyer_id;
                ALTER TABLE messaging.negotiations
                    RENAME CONSTRAINT fk_negotiations_user_profiles_seller_id
                                   TO fk_conversations_user_profiles_seller_id;
                ALTER TABLE messaging.negotiations
                    RENAME CONSTRAINT fk_negotiations_vehicles_vehicle_id
                                   TO fk_conversations_vehicles_vehicle_id;

                ALTER INDEX messaging.ix_negotiations_buyer_id_seller_id_vehicle_id
                    RENAME TO ix_conversations_buyer_id_seller_id_vehicle_id;
                ALTER INDEX messaging.ix_negotiations_seller_id
                    RENAME TO ix_conversations_seller_id;
                ALTER INDEX messaging.ix_negotiations_vehicle_id
                    RENAME TO ix_conversations_vehicle_id;
            ");

            migrationBuilder.RenameIndex(
                name: "ix_messages_negotiation_id",
                schema: "messaging",
                table: "messages",
                newName: "ix_messages_conversation_id");

            migrationBuilder.RenameColumn(
                name: "negotiation_id",
                schema: "messaging",
                table: "messages",
                newName: "conversation_id");

            migrationBuilder.RenameTable(
                name: "negotiations",
                schema: "messaging",
                newName: "conversations",
                newSchema: "messaging");
        }
    }
}
