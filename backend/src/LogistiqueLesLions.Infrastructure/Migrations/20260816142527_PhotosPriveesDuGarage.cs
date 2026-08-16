using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhotosPriveesDuGarage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "thumbnail_url",
                schema: "garage",
                table: "garage_vehicle_images");

            migrationBuilder.RenameColumn(
                name: "url",
                schema: "garage",
                table: "garage_vehicle_images",
                newName: "storage_key");

            migrationBuilder.AddColumn<string>(
                name: "content_type",
                schema: "garage",
                table: "garage_vehicle_images",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "file_name",
                schema: "garage",
                table: "garage_vehicle_images",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "size_bytes",
                schema: "garage",
                table: "garage_vehicle_images",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Las filas anteriores guardaban en «url» una ruta del almacenamiento
            // PÚBLICO, no una clave privada: tras el renombrado apuntarían a un archivo
            // que el lector privado no sabe abrir, y la ficha enseñaría fotos rotas.
            //
            // Se retiran. Son pocas y de prueba, y sus archivos estaban en la carpeta
            // pública —justo lo que esta migración viene a corregir—, así que no hay nada
            // que conservar. El usuario vuelve a subirlas y esta vez quedan privadas.
            //
            // ⚠️ Los archivos sueltos siguen en disco hasta que Render reinicie, que lo
            // borra todo por ser efímero (pendiente técnico nº 2).
            migrationBuilder.Sql(@"
                DELETE FROM garage.garage_vehicle_images
                WHERE storage_key LIKE '/uploads/%'
                   OR storage_key LIKE 'http%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "content_type",
                schema: "garage",
                table: "garage_vehicle_images");

            migrationBuilder.DropColumn(
                name: "file_name",
                schema: "garage",
                table: "garage_vehicle_images");

            migrationBuilder.DropColumn(
                name: "size_bytes",
                schema: "garage",
                table: "garage_vehicle_images");

            migrationBuilder.RenameColumn(
                name: "storage_key",
                schema: "garage",
                table: "garage_vehicle_images",
                newName: "url");

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_url",
                schema: "garage",
                table: "garage_vehicle_images",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
