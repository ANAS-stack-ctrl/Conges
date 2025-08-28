using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongesApi.Migrations
{
    /// <inheritdoc />
    public partial class Sync_ManagerAssignments_Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ManagerAssignments_EmployeeUserId",
                table: "ManagerAssignments",
                column: "EmployeeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerAssignments_HierarchyId_EmployeeUserId_Active",
                table: "ManagerAssignments",
                columns: new[] { "HierarchyId", "EmployeeUserId", "Active" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagerAssignments_ManagerUserId",
                table: "ManagerAssignments",
                column: "ManagerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagerAssignments_Hierarchies_HierarchyId",
                table: "ManagerAssignments",
                column: "HierarchyId",
                principalTable: "Hierarchies",
                principalColumn: "HierarchyId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ManagerAssignments_Users_EmployeeUserId",
                table: "ManagerAssignments",
                column: "EmployeeUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManagerAssignments_Users_ManagerUserId",
                table: "ManagerAssignments",
                column: "ManagerUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManagerAssignments_Hierarchies_HierarchyId",
                table: "ManagerAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ManagerAssignments_Users_EmployeeUserId",
                table: "ManagerAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ManagerAssignments_Users_ManagerUserId",
                table: "ManagerAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ManagerAssignments_EmployeeUserId",
                table: "ManagerAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ManagerAssignments_HierarchyId_EmployeeUserId_Active",
                table: "ManagerAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ManagerAssignments_ManagerUserId",
                table: "ManagerAssignments");
        }
    }
}
