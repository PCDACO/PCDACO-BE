using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionScheduleToBookingReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RelatedReportId",
                table: "InspectionSchedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "InspectionSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "InspectionScheduleId",
                table: "BookingReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InspectionSchedules_RelatedReportId",
                table: "InspectionSchedules",
                column: "RelatedReportId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingReports_InspectionScheduleId",
                table: "BookingReports",
                column: "InspectionScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingReports_InspectionSchedules_InspectionScheduleId",
                table: "BookingReports",
                column: "InspectionScheduleId",
                principalTable: "InspectionSchedules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionSchedules_BookingReports_RelatedReportId",
                table: "InspectionSchedules",
                column: "RelatedReportId",
                principalTable: "BookingReports",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingReports_InspectionSchedules_InspectionScheduleId",
                table: "BookingReports");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionSchedules_BookingReports_RelatedReportId",
                table: "InspectionSchedules");

            migrationBuilder.DropIndex(
                name: "IX_InspectionSchedules_RelatedReportId",
                table: "InspectionSchedules");

            migrationBuilder.DropIndex(
                name: "IX_BookingReports_InspectionScheduleId",
                table: "BookingReports");

            migrationBuilder.DropColumn(
                name: "RelatedReportId",
                table: "InspectionSchedules");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "InspectionSchedules");

            migrationBuilder.DropColumn(
                name: "InspectionScheduleId",
                table: "BookingReports");
        }
    }
}
