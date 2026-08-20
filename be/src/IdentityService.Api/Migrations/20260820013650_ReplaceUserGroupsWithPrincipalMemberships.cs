using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceUserGroupsWithPrincipalMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_groups_groups_group_id",
                table: "user_groups");
            migrationBuilder.DropIndex(
                name: "IX_user_groups_group_id_left_at_utc",
                table: "user_groups");
            migrationBuilder.RenameTable(
                name: "user_groups",
                newName: "principal_memberships");
            migrationBuilder.RenameColumn(
                name: "group_id",
                table: "principal_memberships",
                newName: "principal_id");
            migrationBuilder.Sql("""
                UPDATE principal_memberships membership
                SET principal_id = groups.principal_id
                FROM groups
                WHERE membership.principal_id = groups.id;
                """);
            migrationBuilder.AddForeignKey(
                name: "FK_principal_memberships_principals_principal_id",
                table: "principal_memberships",
                column: "principal_id",
                principalTable: "principals",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.CreateIndex(
                name: "IX_principal_memberships_principal_id_left_at_utc",
                table: "principal_memberships",
                columns: new[] { "principal_id", "left_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_principal_memberships_principals_principal_id",
                table: "principal_memberships");
            migrationBuilder.DropIndex(
                name: "IX_principal_memberships_principal_id_left_at_utc",
                table: "principal_memberships");
            migrationBuilder.RenameColumn(
                name: "principal_id",
                table: "principal_memberships",
                newName: "group_id");
            migrationBuilder.Sql("""
                UPDATE principal_memberships membership
                SET group_id = groups.id
                FROM groups
                WHERE membership.group_id = groups.principal_id;
                """);
            migrationBuilder.RenameTable(
                name: "principal_memberships",
                newName: "user_groups");
            migrationBuilder.AddForeignKey(
                name: "FK_user_groups_groups_group_id",
                table: "user_groups",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.CreateIndex(
                name: "IX_user_groups_group_id_left_at_utc",
                table: "user_groups",
                columns: new[] { "group_id", "left_at_utc" });
        }
    }
}
