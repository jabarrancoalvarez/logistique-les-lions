using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CodeVerificationContrat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "verification_code",
                schema: "messaging",
                table: "contracts",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_contracts_verification_code",
                schema: "messaging",
                table: "contracts",
                column: "verification_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_contracts_verification_code",
                schema: "messaging",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "verification_code",
                schema: "messaging",
                table: "contracts");
        }
    }
}
