using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongesApi.Migrations
{
    public partial class AddRequiresDirectorOverrideToLeaveRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 👉 On ajoute uniquement la colonne sur LeaveRequests
            migrationBuilder.AddColumn<bool>(
                name: "RequiresDirectorOverride",
                table: "LeaveRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 👉 Rollback : on retire simplement la colonne
            migrationBuilder.DropColumn(
                name: "RequiresDirectorOverride",
                table: "LeaveRequests");
        }
    }
}
