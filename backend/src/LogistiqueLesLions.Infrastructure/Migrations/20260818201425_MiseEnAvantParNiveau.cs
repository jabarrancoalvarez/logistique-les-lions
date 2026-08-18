using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MiseEnAvantParNiveau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vehicles_active_featured",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "is_featured",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "featured_at",
                schema: "vehicles",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "featured_tier",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                // Las filas existentes quedan como «Aucune» (no destacadas). "" no es un
                // valor válido del enum y rompería al leerlas.
                defaultValue: "Aucune");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "featured_until",
                schema: "vehicles",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_active_featured",
                schema: "vehicles",
                table: "vehicles",
                columns: new[] { "status", "featured_tier", "featured_until" },
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vehicles_active_featured",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "featured_at",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "featured_tier",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "featured_until",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.AddColumn<bool>(
                name: "is_featured",
                schema: "vehicles",
                table: "vehicles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_active_featured",
                schema: "vehicles",
                table: "vehicles",
                columns: new[] { "status", "is_featured", "deleted_at" },
                filter: "deleted_at IS NULL");
        }
    }
}
