using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongesApi.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagerAssignments",
                columns: table => new
                {
                    ManagerAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HierarchyId = table.Column<int>(type: "int", nullable: false),
                    EmployeeUserId = table.Column<int>(type: "int", nullable: false),
                    ManagerUserId = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerAssignments", x => x.ManagerAssignmentId);

                    // FK -> Hierarchies
                    table.ForeignKey(
                        name: "FK_ManagerAssignments_Hierarchies_HierarchyId",
                        column: x => x.HierarchyId,
                        principalTable: "Hierarchies",
                        principalColumn: "HierarchyId",
                        onDelete: ReferentialAction.Cascade);

                    // FK -> Users (Employee)
                    table.ForeignKey(
                        name: "FK_ManagerAssignments_Users_EmployeeUserId",
                        column: x => x.EmployeeUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);

                    // FK -> Users (Manager)
                    table.ForeignKey(
                        name: "FK_ManagerAssignments_Users_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                });

            // Index pour éviter plusieurs affectations actives du même employé dans une hiérarchie
            migrationBuilder.CreateIndex(
                name: "IX_ManagerAssignments_HierarchyId_EmployeeUserId_Active",
                table: "ManagerAssignments",
                columns: new[] { "HierarchyId", "EmployeeUserId", "Active" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagerAssignments_ManagerUserId",
                table: "ManagerAssignments",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerAssignments_EmployeeUserId",
                table: "ManagerAssignments",
                column: "EmployeeUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagerAssignments");
        }
    }
}
