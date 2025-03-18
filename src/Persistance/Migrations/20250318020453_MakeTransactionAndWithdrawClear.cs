using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class MakeTransactionAndWithdrawClear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WithdrawalRequests_UserId",
                table: "WithdrawalRequests");

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "WithdrawalRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "WithdrawalRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessedByAdminId",
                table: "WithdrawalRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionId",
                table: "WithdrawalRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WithdrawalCode",
                table: "WithdrawalRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceAfter",
                table: "Transactions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_ProcessedByAdminId",
                table: "WithdrawalRequests",
                column: "ProcessedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_TransactionId",
                table: "WithdrawalRequests",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_UserId",
                table: "WithdrawalRequests",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WithdrawalRequests_Transactions_TransactionId",
                table: "WithdrawalRequests",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WithdrawalRequests_Users_ProcessedByAdminId",
                table: "WithdrawalRequests",
                column: "ProcessedByAdminId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WithdrawalRequests_Transactions_TransactionId",
                table: "WithdrawalRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_WithdrawalRequests_Users_ProcessedByAdminId",
                table: "WithdrawalRequests");

            migrationBuilder.DropIndex(
                name: "IX_WithdrawalRequests_ProcessedByAdminId",
                table: "WithdrawalRequests");

            migrationBuilder.DropIndex(
                name: "IX_WithdrawalRequests_TransactionId",
                table: "WithdrawalRequests");

            migrationBuilder.DropIndex(
                name: "IX_WithdrawalRequests_UserId",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "ProcessedByAdminId",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "WithdrawalCode",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "BalanceAfter",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_UserId",
                table: "WithdrawalRequests",
                column: "UserId",
                unique: true);
        }
    }
}
