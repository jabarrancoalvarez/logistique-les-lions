using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DemandesAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "assigned_admin_id",
                schema: "vehicles",
                table: "vehicle_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "additional_costs",
                schema: "vehicles",
                table: "vehicle_request_proposals",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fuel_type",
                schema: "vehicles",
                table: "vehicle_request_proposals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transmission",
                schema: "vehicles",
                table: "vehicle_request_proposals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "version",
                schema: "vehicles",
                table: "vehicle_request_proposals",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_requests_assigned_admin_id",
                schema: "vehicles",
                table: "vehicle_requests",
                column: "assigned_admin_id");

            migrationBuilder.AddForeignKey(
                name: "fk_vehicle_requests_user_profiles_assigned_admin_id",
                schema: "vehicles",
                table: "vehicle_requests",
                column: "assigned_admin_id",
                principalSchema: "users",
                principalTable: "user_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_vehicle_requests_user_profiles_assigned_admin_id",
                schema: "vehicles",
                table: "vehicle_requests");

            migrationBuilder.DropIndex(
                name: "ix_vehicle_requests_assigned_admin_id",
                schema: "vehicles",
                table: "vehicle_requests");

            migrationBuilder.DropColumn(
                name: "assigned_admin_id",
                schema: "vehicles",
                table: "vehicle_requests");

            migrationBuilder.DropColumn(
                name: "additional_costs",
                schema: "vehicles",
                table: "vehicle_request_proposals");

            migrationBuilder.DropColumn(
                name: "fuel_type",
                schema: "vehicles",
                table: "vehicle_request_proposals");

            migrationBuilder.DropColumn(
                name: "transmission",
                schema: "vehicles",
                table: "vehicle_request_proposals");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "vehicles",
                table: "vehicle_request_proposals");
        }
    }
}
