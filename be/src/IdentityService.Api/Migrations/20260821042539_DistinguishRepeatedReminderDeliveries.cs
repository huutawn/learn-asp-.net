using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Api.Migrations
{
    /// <inheritdoc />
    public partial class DistinguishRepeatedReminderDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_reminder_id_occurrence_start_at_utc",
                table: "outbox_messages");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "scheduled_reminder_at_utc",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reminder_scheduled_at_utc",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE outbox_messages
                SET scheduled_reminder_at_utc = created_at_utc;

                UPDATE outbox_messages
                SET payload = jsonb_set(
                    payload,
                    '{ScheduledReminderAtUtc}',
                    to_jsonb(scheduled_reminder_at_utc));

                UPDATE notifications
                SET reminder_scheduled_at_utc = sent_at_utc;
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "scheduled_reminder_at_utc",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "reminder_scheduled_at_utc",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_reminder_id_occurrence_start_at_utc_schedul~",
                table: "outbox_messages",
                columns: new[] { "reminder_id", "occurrence_start_at_utc", "scheduled_reminder_at_utc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_reminder_id_occurrence_start_at_utc_schedul~",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "scheduled_reminder_at_utc",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "reminder_scheduled_at_utc",
                table: "notifications");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_reminder_id_occurrence_start_at_utc",
                table: "outbox_messages",
                columns: new[] { "reminder_id", "occurrence_start_at_utc" },
                unique: true);
        }
    }
}
