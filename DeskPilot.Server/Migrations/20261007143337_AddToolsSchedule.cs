using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeskPilot.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddToolsSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "maintenance_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    maintenance_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "FullInspection"),
                    frequency = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Quarterly"),
                    scheduled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_performed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    assigned_technician = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "IT Support"),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Planned"),
                    checklist_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    result_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintenance_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_maintenance_schedules_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_schedules_department",
                table: "maintenance_schedules",
                column: "department");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_schedules_device_id",
                table: "maintenance_schedules",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_schedules_next_due_date",
                table: "maintenance_schedules",
                column: "next_due_date");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_schedules_status",
                table: "maintenance_schedules",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maintenance_schedules");
        }
    }
}
