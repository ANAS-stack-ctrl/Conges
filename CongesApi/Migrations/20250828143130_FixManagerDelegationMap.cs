using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongesApi.Migrations
{
    /// <inheritdoc />
    public partial class FixManagerDelegationMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManagerDelegations_Users_DelegateManagerUserId",
                table: "ManagerDelegations");

            migrationBuilder.DropForeignKey(
                name: "FK_ManagerDelegations_Users_ManagerUserId",
                table: "ManagerDelegations");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagerDelegations_Users_DelegateManagerUserId",
                table: "ManagerDelegations",
                column: "DelegateManagerUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagerDelegations_Users_ManagerUserId",
                table: "ManagerDelegations",
                column: "ManagerUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManagerDelegations_Users_DelegateManagerUserId",
                table: "ManagerDelegations");

            migrationBuilder.DropForeignKey(
                name: "FK_ManagerDelegations_Users_ManagerUserId",
                table: "ManagerDelegations");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagerDelegations_Users_DelegateManagerUserId",
                table: "ManagerDelegations",
                column: "DelegateManagerUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ManagerDelegations_Users_ManagerUserId",
                table: "ManagerDelegations",
                column: "ManagerUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
