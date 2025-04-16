using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddEntitiesForBookingContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PickupAddress",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RentalPrice",
                table: "Contracts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickupAddress",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "RentalPrice",
                table: "Contracts");
        }
    }
}
