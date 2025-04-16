using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddCarReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageReports_BookingReports_BookingReportId",
                table: "ImageReports");

            migrationBuilder.AddColumn<Guid>(
                name: "CarReportId",
                table: "InspectionSchedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BookingReportId",
                table: "ImageReports",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "CarReportId",
                table: "ImageReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CarReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    ReportType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionComments = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarReports_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarReports_Users_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarReports_Users_ResolvedById",
                        column: x => x.ResolvedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionSchedules_CarReportId",
                table: "InspectionSchedules",
                column: "CarReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageReports_CarReportId",
                table: "ImageReports",
                column: "CarReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CarReports_CarId",
                table: "CarReports",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_CarReports_ReportedById",
                table: "CarReports",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_CarReports_ResolvedById",
                table: "CarReports",
                column: "ResolvedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ImageReports_BookingReports_BookingReportId",
                table: "ImageReports",
                column: "BookingReportId",
                principalTable: "BookingReports",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ImageReports_CarReports_CarReportId",
                table: "ImageReports",
                column: "CarReportId",
                principalTable: "CarReports",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionSchedules_CarReports_CarReportId",
                table: "InspectionSchedules",
                column: "CarReportId",
                principalTable: "CarReports",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageReports_BookingReports_BookingReportId",
                table: "ImageReports");

            migrationBuilder.DropForeignKey(
                name: "FK_ImageReports_CarReports_CarReportId",
                table: "ImageReports");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionSchedules_CarReports_CarReportId",
                table: "InspectionSchedules");

            migrationBuilder.DropTable(
                name: "CarReports");

            migrationBuilder.DropIndex(
                name: "IX_InspectionSchedules_CarReportId",
                table: "InspectionSchedules");

            migrationBuilder.DropIndex(
                name: "IX_ImageReports_CarReportId",
                table: "ImageReports");

            migrationBuilder.DropColumn(
                name: "CarReportId",
                table: "InspectionSchedules");

            migrationBuilder.DropColumn(
                name: "CarReportId",
                table: "ImageReports");

            migrationBuilder.AlterColumn<Guid>(
                name: "BookingReportId",
                table: "ImageReports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ImageReports_BookingReports_BookingReportId",
                table: "ImageReports",
                column: "BookingReportId",
                principalTable: "BookingReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
