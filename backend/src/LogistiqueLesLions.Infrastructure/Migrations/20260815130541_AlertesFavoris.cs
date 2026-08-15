using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlertesFavoris : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ⚠️ EF genera `defaultValue: false` a partir de `default(bool)`, ignorando el
            // inicializador de la propiedad. Se fuerza a `true` porque la especificación
            // establece que, de partida, todos los favoritos reciben alertas.
            migrationBuilder.AddColumn<bool>(
                name: "favorite_alerts_all_enabled",
                schema: "users",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "last_alerted_price",
                schema: "vehicles",
                table: "saved_vehicles",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "price_alert_enabled",
                schema: "vehicles",
                table: "saved_vehicles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price_when_saved",
                schema: "vehicles",
                table: "saved_vehicles",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Los favoritos anteriores a esta migración no tienen precio de referencia.
            // Se toma el precio actual del anuncio: así parten de cero en lugar de
            // mostrar una bajada falsa desde 0 FCFA.
            migrationBuilder.Sql("""
                UPDATE vehicles.saved_vehicles s
                SET price_when_saved = v.price
                FROM vehicles.vehicles v
                WHERE v.id = s.vehicle_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "favorite_alerts_all_enabled",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "last_alerted_price",
                schema: "vehicles",
                table: "saved_vehicles");

            migrationBuilder.DropColumn(
                name: "price_alert_enabled",
                schema: "vehicles",
                table: "saved_vehicles");

            migrationBuilder.DropColumn(
                name: "price_when_saved",
                schema: "vehicles",
                table: "saved_vehicles");
        }
    }
}
