using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendDateBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExtensionAmount",
                table: "Bookings",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExtensionPaid",
                table: "Bookings",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtensionAmount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsExtensionPaid",
                table: "Bookings");
        }
    }
}
