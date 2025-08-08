using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongesApi.Migrations
{
    /// <inheritdoc />
    public partial class AddProofFilePathToLeaveRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveBalances_LeaveTypes_LeaveTypeId",
                table: "LeaveBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveBalances_Users_UserId",
                table: "LeaveBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveTypes_ApprovalFlowTypes_ApprovalFlow",
                table: "LeaveTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveTypes_LeavePolicies_PolicyId",
                table: "LeaveTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeaveTypes",
                table: "LeaveTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeaveBalances",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "CurrentLeaveBalance",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "LeaveTypes",
                newName: "LeaveType");

            migrationBuilder.RenameTable(
                name: "LeaveBalances",
                newName: "LeaveBalance");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveTypes_PolicyId",
                table: "LeaveType",
                newName: "IX_LeaveType_PolicyId");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveTypes_ApprovalFlow",
                table: "LeaveType",
                newName: "IX_LeaveType_ApprovalFlow");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveBalances_UserId",
                table: "LeaveBalance",
                newName: "IX_LeaveBalance_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveBalances_LeaveTypeId",
                table: "LeaveBalance",
                newName: "IX_LeaveBalance_LeaveTypeId");

            migrationBuilder.AddColumn<string>(
                name: "ProofFilePath",
                table: "LeaveRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeaveType",
                table: "LeaveType",
                column: "LeaveTypeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeaveBalance",
                table: "LeaveBalance",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveBalance_LeaveType_LeaveTypeId",
                table: "LeaveBalance",
                column: "LeaveTypeId",
                principalTable: "LeaveType",
                principalColumn: "LeaveTypeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveBalance_Users_UserId",
                table: "LeaveBalance",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_LeaveType_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId",
                principalTable: "LeaveType",
                principalColumn: "LeaveTypeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveType_ApprovalFlowTypes_ApprovalFlow",
                table: "LeaveType",
                column: "ApprovalFlow",
                principalTable: "ApprovalFlowTypes",
                principalColumn: "FlowType",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveType_LeavePolicies_PolicyId",
                table: "LeaveType",
                column: "PolicyId",
                principalTable: "LeavePolicies",
                principalColumn: "PolicyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveBalance_LeaveType_LeaveTypeId",
                table: "LeaveBalance");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveBalance_Users_UserId",
                table: "LeaveBalance");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_LeaveType_LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveType_ApprovalFlowTypes_ApprovalFlow",
                table: "LeaveType");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveType_LeavePolicies_PolicyId",
                table: "LeaveType");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeaveType",
                table: "LeaveType");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeaveBalance",
                table: "LeaveBalance");

            migrationBuilder.DropColumn(
                name: "ProofFilePath",
                table: "LeaveRequests");

            migrationBuilder.RenameTable(
                name: "LeaveType",
                newName: "LeaveTypes");

            migrationBuilder.RenameTable(
                name: "LeaveBalance",
                newName: "LeaveBalances");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveType_PolicyId",
                table: "LeaveTypes",
                newName: "IX_LeaveTypes_PolicyId");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveType_ApprovalFlow",
                table: "LeaveTypes",
                newName: "IX_LeaveTypes_ApprovalFlow");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveBalance_UserId",
                table: "LeaveBalances",
                newName: "IX_LeaveBalances_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveBalance_LeaveTypeId",
                table: "LeaveBalances",
                newName: "IX_LeaveBalances_LeaveTypeId");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentLeaveBalance",
                table: "Users",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeaveTypes",
                table: "LeaveTypes",
                column: "LeaveTypeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeaveBalances",
                table: "LeaveBalances",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveBalances_LeaveTypes_LeaveTypeId",
                table: "LeaveBalances",
                column: "LeaveTypeId",
                principalTable: "LeaveTypes",
                principalColumn: "LeaveTypeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveBalances_Users_UserId",
                table: "LeaveBalances",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId",
                principalTable: "LeaveTypes",
                principalColumn: "LeaveTypeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveTypes_ApprovalFlowTypes_ApprovalFlow",
                table: "LeaveTypes",
                column: "ApprovalFlow",
                principalTable: "ApprovalFlowTypes",
                principalColumn: "FlowType",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveTypes_LeavePolicies_PolicyId",
                table: "LeaveTypes",
                column: "PolicyId",
                principalTable: "LeavePolicies",
                principalColumn: "PolicyId");
        }
    }
}
