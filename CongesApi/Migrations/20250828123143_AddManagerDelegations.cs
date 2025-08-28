using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongesApi.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerDelegations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagerDelegations",
                columns: table => new
                {
                    ManagerDelegationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HierarchyId = table.Column<int>(type: "int", nullable: false),
                    ManagerUserId = table.Column<int>(type: "int", nullable: false),
                    DelegateManagerUserId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerDelegations", x => x.ManagerDelegationId);

                    // FK -> Hierarchies: on peut laisser CASCADE
                    table.ForeignKey(
                        name: "FK_ManagerDelegations_Hierarchies_HierarchyId",
                        column: x => x.HierarchyId,
                        principalTable: "Hierarchies",
                        principalColumn: "HierarchyId",
                        onDelete: ReferentialAction.Cascade);

                    // FK -> Users (Manager): **NO ACTION** pour éviter multiple cascade paths
                    table.ForeignKey(
                        name: "FK_ManagerDelegations_Users_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);

                    // FK -> Users (Delegate): **NO ACTION** aussi
                    table.ForeignKey(
                        name: "FK_ManagerDelegations_Users_DelegateManagerUserId",
                        column: x => x.DelegateManagerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerDelegations_HierarchyId",
                table: "ManagerDelegations",
                column: "HierarchyId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerDelegations_ManagerUserId",
                table: "ManagerDelegations",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerDelegations_DelegateManagerUserId",
                table: "ManagerDelegations",
                column: "DelegateManagerUserId");

            // Unicité logique optionnelle : un manager ne peut avoir qu’une délégation active chevauchante
            // (facultatif : on pourrait l’assurer côté code; SQL Server ne gère pas l’unicité sur intervalle)
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagerDelegations");
        }
    }
}
