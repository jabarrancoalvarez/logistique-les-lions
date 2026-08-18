using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RetraitStatutDouanier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vehicles_customs_status",
                schema: "vehicles",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "customs_status",
                schema: "vehicles",
                table: "vehicles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "customs_status",
                schema: "vehicles",
                table: "vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_customs_status",
                schema: "vehicles",
                table: "vehicles",
                column: "customs_status");
        }
    }
}
