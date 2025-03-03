using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreStatisticForCarAndUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalCancel",
                table: "UserStatistics",
                newName: "TotalRejected");

            migrationBuilder.RenameColumn(
                name: "TotalRented",
                table: "CarStatistics",
                newName: "TotalRejected");

            migrationBuilder.RenameColumn(
                name: "TotalCancellation",
                table: "CarStatistics",
                newName: "TotalExpired");

            migrationBuilder.AddColumn<int>(
                name: "TotalCancelled",
                table: "UserStatistics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCompleted",
                table: "UserStatistics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalExpired",
                table: "UserStatistics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalBooking",
                table: "CarStatistics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCancelled",
                table: "CarStatistics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCompleted",
                table: "CarStatistics",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalCancelled",
                table: "UserStatistics");

            migrationBuilder.DropColumn(
                name: "TotalCompleted",
                table: "UserStatistics");

            migrationBuilder.DropColumn(
                name: "TotalExpired",
                table: "UserStatistics");

            migrationBuilder.DropColumn(
                name: "TotalBooking",
                table: "CarStatistics");

            migrationBuilder.DropColumn(
                name: "TotalCancelled",
                table: "CarStatistics");

            migrationBuilder.DropColumn(
                name: "TotalCompleted",
                table: "CarStatistics");

            migrationBuilder.RenameColumn(
                name: "TotalRejected",
                table: "UserStatistics",
                newName: "TotalCancel");

            migrationBuilder.RenameColumn(
                name: "TotalRejected",
                table: "CarStatistics",
                newName: "TotalRented");

            migrationBuilder.RenameColumn(
                name: "TotalExpired",
                table: "CarStatistics",
                newName: "TotalCancellation");
        }
    }
}
