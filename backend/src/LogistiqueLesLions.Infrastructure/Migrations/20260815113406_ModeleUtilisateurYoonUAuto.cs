using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogistiqueLesLions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModeleUtilisateurYoonUAuto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ────────────────────────────────────────────────────────────────────
            // Migración escrita a mano sobre el andamiaje de EF por dos motivos:
            //  1. EF colocaba los DropColumn ANTES de crear las columnas nuevas, lo
            //     que habría perdido first_name / last_name / company_name.
            //  2. EF infirió erróneamente un rename is_active → allow_whats_app_contact.
            //     Son conceptos distintos: is_active se convierte en `status` y
            //     allow_whats_app_contact es una preferencia nueva.
            // ────────────────────────────────────────────────────────────────────

            // ─── 1. Columnas nuevas ─────────────────────────────────────────────
            migrationBuilder.RenameColumn(
                name: "is_verified",
                schema: "users",
                table: "user_profiles",
                newName: "phone_verified");

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                schema: "users",
                table: "user_profiles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "account_type",
                schema: "users",
                table: "user_profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Particulier");

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "users",
                table: "user_profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<bool>(
                name: "allow_whats_app_contact",
                schema: "users",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "region",
                schema: "users",
                table: "user_profiles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "verified_sales_count",
                schema: "users",
                table: "user_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_activity_at",
                schema: "users",
                table: "user_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "phone_verified_at",
                schema: "users",
                table: "user_profiles",
                type: "timestamp with time zone",
                nullable: true);

            // ─── 2. Traspaso de datos antes de borrar nada ──────────────────────
            // El nombre mostrado toma el nombre comercial si existía; si no, nombre
            // y apellido concatenados.
            migrationBuilder.Sql("""
                UPDATE users.user_profiles
                SET display_name = COALESCE(
                        NULLIF(TRIM(company_name), ''),
                        NULLIF(TRIM(CONCAT_WS(' ', first_name, last_name)), ''),
                        'Utilisateur');
                """);

            // Los antiguos concesionarios pasan a ser cuentas "Professionnel";
            // el resto, "Particulier". Es solo un campo informativo del perfil.
            migrationBuilder.Sql("""
                UPDATE users.user_profiles
                SET account_type = CASE WHEN role = 'Dealer' THEN 'Professionnel'
                                        ELSE 'Particulier' END;
                """);

            // is_active se convierte en el estado administrativo de la cuenta.
            migrationBuilder.Sql("""
                UPDATE users.user_profiles
                SET status = CASE WHEN is_active THEN 'Active' ELSE 'Suspended' END;
                """);

            // Solo quedan dos roles. Buyer/Seller/Dealer/Moderator pasan a User;
            // Admin se conserva.
            migrationBuilder.Sql("""
                UPDATE users.user_profiles
                SET role = 'User'
                WHERE role IN ('Buyer', 'Seller', 'Dealer', 'Moderator');
                """);

            // El teléfono pasa a ser el identificador único de la cuenta y debe estar
            // en E.164 senegalés. Los valores heredados que no cumplen el formato se
            // vacían: el usuario los volverá a introducir y verificar.
            migrationBuilder.Sql(@"
                UPDATE users.user_profiles
                SET phone = NULL, phone_verified = FALSE
                WHERE phone IS NOT NULL AND phone !~ '^\+221[0-9]{9}$';
                ");

            // Y si tras la limpieza quedaran duplicados, se conserva el más antiguo.
            migrationBuilder.Sql("""
                UPDATE users.user_profiles u
                SET phone = NULL, phone_verified = FALSE
                WHERE u.phone IS NOT NULL
                  AND EXISTS (SELECT 1 FROM users.user_profiles o
                              WHERE o.phone = u.phone
                                AND o.id <> u.id
                                AND o.created_at < u.created_at);
                """);

            // ─── 3. Retirada de las columnas del modelo anterior ────────────────
            migrationBuilder.DropColumn(
                name: "company_name",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "company_vat",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "country_code",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "first_name",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "last_name",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "users",
                table: "user_profiles");

            // ─── 4. Ajuste de tipos ────────────────────────────────────────────
            migrationBuilder.AlterColumn<string>(
                name: "role",
                schema: "users",
                table: "user_profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "users",
                table: "user_profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            // El correo pasa a ser opcional: el identificador de la cuenta es el teléfono.
            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "users",
                table: "user_profiles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(6771), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(6922), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7068), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7069), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7072), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7073), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7076), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7076), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7079), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7079), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7096), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7097), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7100), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7100), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7103), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7103), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7106), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7107), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7109), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7110), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7112), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7113), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7116), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 34, 5, 776, DateTimeKind.Unspecified).AddTicks(7116), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_phone",
                schema: "users",
                table: "user_profiles",
                column: "phone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_profiles_phone",
                schema: "users",
                table: "user_profiles");

            // Se restituye is_active a partir del estado administrativo antes de
            // eliminar la columna `status`.
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "users",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("""
                UPDATE users.user_profiles SET is_active = (status = 'Active');
                """);

            migrationBuilder.DropColumn(
                name: "account_type",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "display_name",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "last_activity_at",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "phone_verified_at",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "region",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "verified_sales_count",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "allow_whats_app_contact",
                schema: "users",
                table: "user_profiles");

            migrationBuilder.RenameColumn(
                name: "phone_verified",
                schema: "users",
                table: "user_profiles",
                newName: "is_verified");

            migrationBuilder.AlterColumn<string>(
                name: "role",
                schema: "users",
                table: "user_profiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "users",
                table: "user_profiles",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            // El modelo anterior exigía correo: se rellena un valor sintético para las
            // cuentas que solo tienen teléfono, o el ALTER fallaría.
            migrationBuilder.Sql("""
                UPDATE users.user_profiles
                SET email = CONCAT(id::text, '@sans-email.yoonuauto.local')
                WHERE email IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "users",
                table: "user_profiles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_name",
                schema: "users",
                table: "user_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_vat",
                schema: "users",
                table: "user_profiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                schema: "users",
                table: "user_profiles",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                schema: "users",
                table: "user_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                schema: "users",
                table: "user_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5015), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5175), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5314), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5315), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5318), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5319), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5322), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5322), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5325), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5325), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5328), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5328), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5330), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5331), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5333), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5334), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5340), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5341), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5343), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5344), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5346), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5347), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "vehicles",
                table: "countries",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"),
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5349), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 8, 15, 11, 21, 20, 528, DateTimeKind.Unspecified).AddTicks(5349), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
