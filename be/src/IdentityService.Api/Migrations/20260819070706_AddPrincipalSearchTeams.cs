using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPrincipalSearchTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_projects_principal_id",
                table: "projects");

            migrationBuilder.AddColumn<bool>(
                name: "available",
                table: "principals",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    principal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_principals_principal_id",
                        column: x => x.principal_id,
                        principalTable: "principals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_teams_scopes_scope_id",
                        column: x => x.scope_id,
                        principalTable: "scopes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_projects_principal_id",
                table: "projects",
                column: "principal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_principals_available_id",
                table: "principals",
                columns: new[] { "available", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_teams_name",
                table: "teams",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_principal_id",
                table: "teams",
                column: "principal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_scope_id",
                table: "teams",
                column: "scope_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropIndex(
                name: "IX_projects_principal_id",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "IX_principals_available_id",
                table: "principals");

            migrationBuilder.DropColumn(
                name: "available",
                table: "principals");

            migrationBuilder.CreateIndex(
                name: "IX_projects_principal_id",
                table: "projects",
                column: "principal_id");
        }
    }
}
