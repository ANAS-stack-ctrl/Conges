using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongesApi.Migrations
{
    /// <inheritdoc />
    public partial class Hierarchies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HierarchyId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HierarchyId",
                table: "LeaveRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApprovalDelegations",
                columns: table => new
                {
                    ApprovalDelegationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerUserId = table.Column<int>(type: "int", nullable: false),
                    DelegateUserId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDelegations", x => x.ApprovalDelegationId);
                });

            migrationBuilder.CreateTable(
                name: "Hierarchies",
                columns: table => new
                {
                    HierarchyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hierarchies", x => x.HierarchyId);
                });

            migrationBuilder.CreateTable(
                name: "HierarchyApprovalPolicies",
                columns: table => new
                {
                    HierarchyApprovalPolicyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HierarchyId = table.Column<int>(type: "int", nullable: false),
                    ManagerPeerFirst = table.Column<bool>(type: "bit", nullable: false),
                    RequiredPeerCount = table.Column<int>(type: "int", nullable: false),
                    PeerSelectionMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FallbackToDirector = table.Column<bool>(type: "bit", nullable: false),
                    FallbackToHR = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarchyApprovalPolicies", x => x.HierarchyApprovalPolicyId);
                    table.ForeignKey(
                        name: "FK_HierarchyApprovalPolicies_Hierarchies_HierarchyId",
                        column: x => x.HierarchyId,
                        principalTable: "Hierarchies",
                        principalColumn: "HierarchyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_HierarchyId",
                table: "Users",
                column: "HierarchyId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_HierarchyId",
                table: "LeaveRequests",
                column: "HierarchyId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarchyApprovalPolicies_HierarchyId",
                table: "HierarchyApprovalPolicies",
                column: "HierarchyId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Hierarchies_HierarchyId",
                table: "LeaveRequests",
                column: "HierarchyId",
                principalTable: "Hierarchies",
                principalColumn: "HierarchyId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Hierarchies_HierarchyId",
                table: "Users",
                column: "HierarchyId",
                principalTable: "Hierarchies",
                principalColumn: "HierarchyId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Hierarchies_HierarchyId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Hierarchies_HierarchyId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ApprovalDelegations");

            migrationBuilder.DropTable(
                name: "HierarchyApprovalPolicies");

            migrationBuilder.DropTable(
                name: "Hierarchies");

            migrationBuilder.DropIndex(
                name: "IX_Users_HierarchyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_HierarchyId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "HierarchyId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HierarchyId",
                table: "LeaveRequests");
        }
    }
}
