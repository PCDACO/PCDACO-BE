using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreFieldForCarContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GPSDeviceId",
                table: "CarContracts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionResults",
                table: "CarContracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OwnerSignatureDate",
                table: "CarContracts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CarContracts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TechnicianId",
                table: "CarContracts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TechnicianSignatureDate",
                table: "CarContracts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarContracts_GPSDeviceId",
                table: "CarContracts",
                column: "GPSDeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarContracts_TechnicianId",
                table: "CarContracts",
                column: "TechnicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_CarContracts_GPSDevices_GPSDeviceId",
                table: "CarContracts",
                column: "GPSDeviceId",
                principalTable: "GPSDevices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CarContracts_Users_TechnicianId",
                table: "CarContracts",
                column: "TechnicianId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarContracts_GPSDevices_GPSDeviceId",
                table: "CarContracts");

            migrationBuilder.DropForeignKey(
                name: "FK_CarContracts_Users_TechnicianId",
                table: "CarContracts");

            migrationBuilder.DropIndex(
                name: "IX_CarContracts_GPSDeviceId",
                table: "CarContracts");

            migrationBuilder.DropIndex(
                name: "IX_CarContracts_TechnicianId",
                table: "CarContracts");

            migrationBuilder.DropColumn(
                name: "GPSDeviceId",
                table: "CarContracts");

            migrationBuilder.DropColumn(
                name: "InspectionResults",
                table: "CarContracts");

            migrationBuilder.DropColumn(
                name: "OwnerSignatureDate",
                table: "CarContracts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CarContracts");

            migrationBuilder.DropColumn(
                name: "TechnicianId",
                table: "CarContracts");

            migrationBuilder.DropColumn(
                name: "TechnicianSignatureDate",
                table: "CarContracts");
        }
    }
}
