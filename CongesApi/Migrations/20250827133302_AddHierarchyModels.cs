using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongesApi.Migrations
{
    /// <inheritdoc />
    public partial class AddHierarchyModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Hierarchies_HierarchyId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "HierarchyApprovalPolicyId",
                table: "HierarchyApprovalPolicies",
                newName: "PolicyId");

            migrationBuilder.RenameColumn(
                name: "OwnerUserId",
                table: "ApprovalDelegations",
                newName: "ToUserId");

            migrationBuilder.RenameColumn(
                name: "DelegateUserId",
                table: "ApprovalDelegations",
                newName: "FromUserId");

            migrationBuilder.RenameColumn(
                name: "Active",
                table: "ApprovalDelegations",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "ApprovalDelegationId",
                table: "ApprovalDelegations",
                newName: "DelegationId");

            migrationBuilder.AlterColumn<string>(
                name: "PeerSelectionMode",
                table: "HierarchyApprovalPolicies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Hierarchies",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Hierarchies",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Hierarchies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "ApprovalDelegations",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "HierarchyId",
                table: "ApprovalDelegations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HierarchyMembers",
                columns: table => new
                {
                    HierarchyMemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HierarchyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarchyMembers", x => x.HierarchyMemberId);
                    table.ForeignKey(
                        name: "FK_HierarchyMembers_Hierarchies_HierarchyId",
                        column: x => x.HierarchyId,
                        principalTable: "Hierarchies",
                        principalColumn: "HierarchyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HierarchyMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hierarchies_Name",
                table: "Hierarchies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDelegations_FromUserId",
                table: "ApprovalDelegations",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDelegations_HierarchyId",
                table: "ApprovalDelegations",
                column: "HierarchyId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDelegations_ToUserId",
                table: "ApprovalDelegations",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarchyMembers_HierarchyId",
                table: "HierarchyMembers",
                column: "HierarchyId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarchyMembers_UserId",
                table: "HierarchyMembers",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalDelegations_Hierarchies_HierarchyId",
                table: "ApprovalDelegations",
                column: "HierarchyId",
                principalTable: "Hierarchies",
                principalColumn: "HierarchyId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalDelegations_Users_FromUserId",
                table: "ApprovalDelegations",
                column: "FromUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalDelegations_Users_ToUserId",
                table: "ApprovalDelegations",
                column: "ToUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Hierarchies_HierarchyId",
                table: "Users",
                column: "HierarchyId",
                principalTable: "Hierarchies",
                principalColumn: "HierarchyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalDelegations_Hierarchies_HierarchyId",
                table: "ApprovalDelegations");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalDelegations_Users_FromUserId",
                table: "ApprovalDelegations");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalDelegations_Users_ToUserId",
                table: "ApprovalDelegations");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Hierarchies_HierarchyId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "HierarchyMembers");

            migrationBuilder.DropIndex(
                name: "IX_Hierarchies_Name",
                table: "Hierarchies");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDelegations_FromUserId",
                table: "ApprovalDelegations");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDelegations_HierarchyId",
                table: "ApprovalDelegations");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDelegations_ToUserId",
                table: "ApprovalDelegations");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Hierarchies");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Hierarchies");

            migrationBuilder.DropColumn(
                name: "HierarchyId",
                table: "ApprovalDelegations");

            migrationBuilder.RenameColumn(
                name: "PolicyId",
                table: "HierarchyApprovalPolicies",
                newName: "HierarchyApprovalPolicyId");

            migrationBuilder.RenameColumn(
                name: "ToUserId",
                table: "ApprovalDelegations",
                newName: "OwnerUserId");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "ApprovalDelegations",
                newName: "Active");

            migrationBuilder.RenameColumn(
                name: "FromUserId",
                table: "ApprovalDelegations",
                newName: "DelegateUserId");

            migrationBuilder.RenameColumn(
                name: "DelegationId",
                table: "ApprovalDelegations",
                newName: "ApprovalDelegationId");

            migrationBuilder.AlterColumn<string>(
                name: "PeerSelectionMode",
                table: "HierarchyApprovalPolicies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Hierarchies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "ApprovalDelegations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Hierarchies_HierarchyId",
                table: "Users",
                column: "HierarchyId",
                principalTable: "Hierarchies",
                principalColumn: "HierarchyId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
