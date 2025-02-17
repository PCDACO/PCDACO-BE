using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddGPS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "PricePerDay",
                table: "Cars");

            migrationBuilder.RenameColumn(
                name: "PricePerHour",
                table: "Cars",
                newName: "Price");

            migrationBuilder.CreateTable(
                name: "CarContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarId = table.Column<Guid>(type: "uuid", nullable: false),
                    Terms = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarContracts_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GPSDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GPSDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GPSDevices_DeviceStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "DeviceStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarGPSes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<Point>(type: "geometry", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarGPSes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarGPSes_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarGPSes_GPSDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "GPSDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarContracts_CarId",
                table: "CarContracts",
                column: "CarId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarGPSes_CarId",
                table: "CarGPSes",
                column: "CarId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarGPSes_DeviceId",
                table: "CarGPSes",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GPSDevices_StatusId",
                table: "GPSDevices",
                column: "StatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarContracts");

            migrationBuilder.DropTable(
                name: "CarGPSes");

            migrationBuilder.DropTable(
                name: "GPSDevices");

            migrationBuilder.DropTable(
                name: "DeviceStatuses");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Cars",
                newName: "PricePerHour");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Cars",
                type: "geometry",
                nullable: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerDay",
                table: "Cars",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
