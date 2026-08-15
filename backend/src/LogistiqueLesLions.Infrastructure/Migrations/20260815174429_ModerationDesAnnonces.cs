using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModerationDesAnnonces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "admin_flagged_at",
                schema: "vehicles",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "admin_hidden_at",
                schema: "vehicles",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "admin_flagged_at",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "admin_hidden_at",
                schema: "vehicles",
                table: "vehicles");
        }
    }
}
