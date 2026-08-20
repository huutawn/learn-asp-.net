using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Api.Migrations
{
    /// <inheritdoc />
    public partial class PrincipalBoundRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_groups_scopes_scope_id",
                table: "groups");

            migrationBuilder.DropForeignKey(
                name: "FK_projects_scopes_scope_id",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_role_assignments_principals_principal_id",
                table: "role_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_role_assignments_scopes_scope_id",
                table: "role_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_teams_scopes_scope_id",
                table: "teams");

            migrationBuilder.DropTable(
                name: "scopes");

            migrationBuilder.DropIndex(
                name: "IX_teams_scope_id",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_role_assignments_principal_id_role_id_scope_id",
                table: "role_assignments");

            migrationBuilder.DropIndex(
                name: "IX_role_assignments_scope_id",
                table: "role_assignments");

            migrationBuilder.DropIndex(
                name: "IX_projects_scope_id",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "IX_groups_scope_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "scope_id",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "scope_id",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "scope_id",
                table: "groups");

            migrationBuilder.RenameColumn(
                name: "principal_id",
                table: "role_assignments",
                newName: "subject_principal_id");

            migrationBuilder.DropColumn(
                name: "scope_id",
                table: "role_assignments");

            migrationBuilder.AddColumn<Guid>(
                name: "resource_principal_id",
                table: "role_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_owner",
                table: "principal_memberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "permission_grants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_principal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_principal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    granted_by_principal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission_grants", x => x.id);
                    table.ForeignKey(
                        name: "FK_permission_grants_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_permission_grants_principals_resource_principal_id",
                        column: x => x.resource_principal_id,
                        principalTable: "principals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_permission_grants_principals_subject_principal_id",
                        column: x => x.subject_principal_id,
                        principalTable: "principals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_role_assignments_resource_principal_id",
                table: "role_assignments",
                column: "resource_principal_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_assignments_subject_principal_id_role_id_resource_prin~",
                table: "role_assignments",
                columns: new[] { "subject_principal_id", "role_id", "resource_principal_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permission_grants_permission_id",
                table: "permission_grants",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_permission_grants_resource_principal_id",
                table: "permission_grants",
                column: "resource_principal_id");

            migrationBuilder.CreateIndex(
                name: "IX_permission_grants_subject_principal_id_permission_id_resour~",
                table: "permission_grants",
                columns: new[] { "subject_principal_id", "permission_id", "resource_principal_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_role_assignments_principals_resource_principal_id",
                table: "role_assignments",
                column: "resource_principal_id",
                principalTable: "principals",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_role_assignments_principals_subject_principal_id",
                table: "role_assignments",
                column: "subject_principal_id",
                principalTable: "principals",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_role_assignments_principals_resource_principal_id",
                table: "role_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_role_assignments_principals_subject_principal_id",
                table: "role_assignments");

            migrationBuilder.DropTable(
                name: "permission_grants");

            migrationBuilder.DropIndex(
                name: "IX_role_assignments_resource_principal_id",
                table: "role_assignments");

            migrationBuilder.DropIndex(
                name: "IX_role_assignments_subject_principal_id_role_id_resource_prin~",
                table: "role_assignments");

            migrationBuilder.DropColumn(
                name: "resource_principal_id",
                table: "role_assignments");

            migrationBuilder.DropColumn(
                name: "is_owner",
                table: "principal_memberships");

            migrationBuilder.RenameColumn(
                name: "subject_principal_id",
                table: "role_assignments",
                newName: "scope_id");

            migrationBuilder.AddColumn<Guid>(
                name: "scope_id",
                table: "teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "principal_id",
                table: "role_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "scope_id",
                table: "projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "scope_id",
                table: "groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "scopes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scopes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teams_scope_id",
                table: "teams",
                column: "scope_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_assignments_principal_id_role_id_scope_id",
                table: "role_assignments",
                columns: new[] { "principal_id", "role_id", "scope_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_assignments_scope_id",
                table: "role_assignments",
                column: "scope_id");

            migrationBuilder.CreateIndex(
                name: "IX_projects_scope_id",
                table: "projects",
                column: "scope_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_scope_id",
                table: "groups",
                column: "scope_id");

            migrationBuilder.CreateIndex(
                name: "IX_scopes_type",
                table: "scopes",
                column: "type");

            migrationBuilder.AddForeignKey(
                name: "FK_groups_scopes_scope_id",
                table: "groups",
                column: "scope_id",
                principalTable: "scopes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_projects_scopes_scope_id",
                table: "projects",
                column: "scope_id",
                principalTable: "scopes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_role_assignments_principals_principal_id",
                table: "role_assignments",
                column: "principal_id",
                principalTable: "principals",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_role_assignments_scopes_scope_id",
                table: "role_assignments",
                column: "scope_id",
                principalTable: "scopes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_teams_scopes_scope_id",
                table: "teams",
                column: "scope_id",
                principalTable: "scopes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
