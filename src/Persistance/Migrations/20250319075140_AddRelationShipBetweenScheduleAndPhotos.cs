using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationShipBetweenScheduleAndPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionPhotos_CarInspections_InspectionId",
                table: "InspectionPhotos");

            migrationBuilder.AlterColumn<Guid>(
                name: "InspectionId",
                table: "InspectionPhotos",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduleId",
                table: "InspectionPhotos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InspectionPhotos_ScheduleId",
                table: "InspectionPhotos",
                column: "ScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionPhotos_CarInspections_InspectionId",
                table: "InspectionPhotos",
                column: "InspectionId",
                principalTable: "CarInspections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionPhotos_InspectionSchedules_ScheduleId",
                table: "InspectionPhotos",
                column: "ScheduleId",
                principalTable: "InspectionSchedules",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionPhotos_CarInspections_InspectionId",
                table: "InspectionPhotos");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionPhotos_InspectionSchedules_ScheduleId",
                table: "InspectionPhotos");

            migrationBuilder.DropIndex(
                name: "IX_InspectionPhotos_ScheduleId",
                table: "InspectionPhotos");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "InspectionPhotos");

            migrationBuilder.AlterColumn<Guid>(
                name: "InspectionId",
                table: "InspectionPhotos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionPhotos_CarInspections_InspectionId",
                table: "InspectionPhotos",
                column: "InspectionId",
                principalTable: "CarInspections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
