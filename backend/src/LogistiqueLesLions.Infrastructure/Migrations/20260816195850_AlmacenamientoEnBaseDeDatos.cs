using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlmacenamientoEnBaseDeDatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stored_files",
                schema: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    content_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stored_files", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stored_files_storage_key",
                schema: "vehicles",
                table: "stored_files",
                column: "storage_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stored_files",
                schema: "vehicles");
        }
    }
}
