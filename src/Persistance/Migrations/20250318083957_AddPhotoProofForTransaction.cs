using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoProofForTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WithdrawalCode",
                table: "WithdrawalRequests");

            migrationBuilder.AddColumn<string>(
                name: "ProofUrl",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProofUrl",
                table: "Transactions");

            migrationBuilder.AddColumn<string>(
                name: "WithdrawalCode",
                table: "WithdrawalRequests",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
