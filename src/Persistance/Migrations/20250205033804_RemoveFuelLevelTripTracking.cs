using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFuelLevelTripTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FuelLevel",
                table: "TripTrackings");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "TripTrackings");

            migrationBuilder.DropColumn(
                name: "Longtitude",
                table: "TripTrackings");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "TripTrackings",
                type: "geometry",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "TripTrackings");

            migrationBuilder.AddColumn<decimal>(
                name: "FuelLevel",
                table: "TripTrackings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "TripTrackings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longtitude",
                table: "TripTrackings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
