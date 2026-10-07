using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DeskPilot.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "action_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    target_machine = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    command_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    executed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "IT Admin"),
                    executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_action_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "machines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    machine_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cpu_usage_percent = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    ram_usage_percent = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    total_ram_gb = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    used_ram_gb = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    total_disk_space_gb = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    free_disk_space_gb = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    disk_usage_percent = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    is_online = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    spooler_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Running"),
                    status_message = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "Bình thường"),
                    last_seen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_machines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    machine_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Open"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tickets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_action_logs_executed_at",
                table: "action_logs",
                column: "executed_at");

            migrationBuilder.CreateIndex(
                name: "ix_machines_is_online_last_seen",
                table: "machines",
                columns: new[] { "is_online", "last_seen" });

            migrationBuilder.CreateIndex(
                name: "ix_machines_machine_name",
                table: "machines",
                column: "machine_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tickets_machine_name",
                table: "tickets",
                column: "machine_name");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_status",
                table: "tickets",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "action_logs");

            migrationBuilder.DropTable(
                name: "machines");

            migrationBuilder.DropTable(
                name: "tickets");
        }
    }
}
