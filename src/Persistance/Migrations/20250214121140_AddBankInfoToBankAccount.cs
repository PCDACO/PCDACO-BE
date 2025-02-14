using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddBankInfoToBankAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankName",
                table: "BankAccounts");

            migrationBuilder.AddColumn<Guid>(
                name: "BankInfoId",
                table: "BankAccounts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_BankInfoId",
                table: "BankAccounts",
                column: "BankInfoId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankAccounts_BankInfos_BankInfoId",
                table: "BankAccounts",
                column: "BankInfoId",
                principalTable: "BankInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankAccounts_BankInfos_BankInfoId",
                table: "BankAccounts");

            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_BankInfoId",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "BankInfoId",
                table: "BankAccounts");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "BankAccounts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
