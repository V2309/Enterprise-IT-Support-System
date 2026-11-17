using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskPilot.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddDevicesAndTools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    device_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    device_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Computer"),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    mac_address = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    assigned_to = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "InUse"),
                    specifications = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    purchase_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    warranty_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    command_or_script = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    default_parameters = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "1.0.0"),
                    author = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "IT Admin"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tools", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_devices_department",
                table: "devices",
                column: "department");

            migrationBuilder.CreateIndex(
                name: "ix_devices_device_code",
                table: "devices",
                column: "device_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devices_device_type",
                table: "devices",
                column: "device_type");

            migrationBuilder.CreateIndex(
                name: "ix_devices_status",
                table: "devices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_tools_category",
                table: "tools",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_tools_code",
                table: "tools",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tools_is_active",
                table: "tools",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "tools");
        }
    }
}
