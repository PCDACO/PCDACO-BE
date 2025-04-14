using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddSignatureForBookingContractAndCarContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriverSignature",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerSignature",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerSignature",
                table: "CarContracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicianSignature",
                table: "CarContracts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverSignature",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "OwnerSignature",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "OwnerSignature",
                table: "CarContracts");

            migrationBuilder.DropColumn(
                name: "TechnicianSignature",
                table: "CarContracts");
        }
    }
}
