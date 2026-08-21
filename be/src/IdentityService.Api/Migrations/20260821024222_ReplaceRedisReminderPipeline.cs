using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceRedisReminderPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reminders_event_id_remind_before_minutes",
                table: "reminders");

            migrationBuilder.DropIndex(
                name: "IX_notifications_reminder_id_recipient_user_id_occurrence_star~",
                table: "notifications");

            migrationBuilder.RenameColumn(
                name: "next_notify_at_utc",
                table: "reminders",
                newName: "next_reminder_at_utc");

            migrationBuilder.RenameIndex(
                name: "IX_reminders_status_next_notify_at_utc",
                table: "reminders",
                newName: "IX_reminders_status_next_reminder_at_utc");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_occurrence_start_at_utc",
                table: "reminders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "repeat_every_minutes",
                table: "reminders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "notifications",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<short[]>(
                name: "recurrence_weekdays",
                table: "events",
                type: "smallint[]",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE events
                SET recurrence_weekdays = CASE
                    WHEN recurrence_dates = '' THEN ARRAY[]::smallint[]
                    ELSE ARRAY(
                        SELECT CASE trim(day)
                            WHEN 'Sunday' THEN 0
                            WHEN 'Monday' THEN 1
                            WHEN 'Tuesday' THEN 2
                            WHEN 'Wednesday' THEN 3
                            WHEN 'Thursday' THEN 4
                            WHEN 'Friday' THEN 5
                            WHEN 'Saturday' THEN 6
                        END::smallint
                        FROM unnest(string_to_array(recurrence_dates, ',')) AS day)
                END;
                """);
            migrationBuilder.Sql("""
                UPDATE reminders AS r
                SET next_occurrence_start_at_utc = e.start_at_utc
                FROM events AS e
                WHERE e.id = r.event_id;
                """);
            migrationBuilder.Sql("""
                UPDATE notifications
                SET idempotency_key = reminder_id::text || ':' || occurrence_start_at_utc::text || ':' || recipient_user_id::text;
                """);

            migrationBuilder.AlterColumn<short[]>(
                name: "recurrence_weekdays",
                table: "events",
                type: "smallint[]",
                nullable: false,
                oldClrType: typeof(short[]),
                oldType: "smallint[]",
                oldNullable: true);
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "next_occurrence_start_at_utc",
                table: "reminders",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                table: "notifications",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "recurrence_dates",
                table: "events");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reminder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurrence_start_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    publishing_lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reminders_event_id_remind_before_minutes_repeat_every_minut~",
                table: "reminders",
                columns: new[] { "event_id", "remind_before_minutes", "repeat_every_minutes" },
                unique: true);

            migrationBuilder.Sql("""
                DROP INDEX "IX_reminders_event_id_remind_before_minutes_repeat_every_minut~";
                CREATE UNIQUE INDEX "IX_reminders_event_id_remind_before_minutes_repeat_every_minut~"
                    ON reminders (event_id, remind_before_minutes, repeat_every_minutes) NULLS NOT DISTINCT;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_idempotency_key",
                table: "notifications",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_reminder_id",
                table: "notifications",
                column: "reminder_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_reminder_id_occurrence_start_at_utc",
                table: "outbox_messages",
                columns: new[] { "reminder_id", "occurrence_start_at_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_status_next_attempt_at_utc",
                table: "outbox_messages",
                columns: new[] { "status", "next_attempt_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_reminders_event_id_remind_before_minutes_repeat_every_minut~",
                table: "reminders");

            migrationBuilder.DropIndex(
                name: "IX_notifications_idempotency_key",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_reminder_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "next_occurrence_start_at_utc",
                table: "reminders");

            migrationBuilder.DropColumn(
                name: "repeat_every_minutes",
                table: "reminders");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "recurrence_weekdays",
                table: "events");

            migrationBuilder.RenameColumn(
                name: "next_reminder_at_utc",
                table: "reminders",
                newName: "next_notify_at_utc");

            migrationBuilder.RenameIndex(
                name: "IX_reminders_status_next_reminder_at_utc",
                table: "reminders",
                newName: "IX_reminders_status_next_notify_at_utc");

            migrationBuilder.AddColumn<string>(
                name: "recurrence_dates",
                table: "events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_reminders_event_id_remind_before_minutes",
                table: "reminders",
                columns: new[] { "event_id", "remind_before_minutes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_reminder_id_recipient_user_id_occurrence_star~",
                table: "notifications",
                columns: new[] { "reminder_id", "recipient_user_id", "occurrence_start_at_utc" },
                unique: true);
        }
    }
}
