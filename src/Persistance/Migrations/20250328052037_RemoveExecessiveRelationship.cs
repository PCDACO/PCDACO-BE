using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExecessiveRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "InspectionScheduleId",
                table: "BookingReports");

            migrationBuilder.RenameColumn(
                name: "RelatedReportId",
                table: "InspectionSchedules",
                newName: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionSchedules_ReportId",
                table: "InspectionSchedules",
                column: "ReportId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionSchedules_BookingReports_ReportId",
                table: "InspectionSchedules",
                column: "ReportId",
                principalTable: "BookingReports",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionSchedules_BookingReports_ReportId",
                table: "InspectionSchedules");

            migrationBuilder.DropIndex(
                name: "IX_InspectionSchedules_ReportId",
                table: "InspectionSchedules");

            migrationBuilder.RenameColumn(
                name: "ReportId",
                table: "InspectionSchedules",
                newName: "RelatedReportId");

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
    }
}
