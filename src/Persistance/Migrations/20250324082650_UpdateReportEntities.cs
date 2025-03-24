using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReportEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageReports_CarReports_CarReportId",
                table: "ImageReports");

            migrationBuilder.DropTable(
                name: "CarReports");

            migrationBuilder.RenameColumn(
                name: "CarReportId",
                table: "ImageReports",
                newName: "BookingReportId");

            migrationBuilder.RenameIndex(
                name: "IX_ImageReports_CarReportId",
                table: "ImageReports",
                newName: "IX_ImageReports_BookingReportId");

            migrationBuilder.AddColumn<string>(
                name: "BannedReason",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BookingReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionComments = table.Column<string>(type: "text", nullable: true),
                    CarId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingReports_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingReports_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BookingReports_Users_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingReports_BookingId",
                table: "BookingReports",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingReports_CarId",
                table: "BookingReports",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingReports_ReportedById",
                table: "BookingReports",
                column: "ReportedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ImageReports_BookingReports_BookingReportId",
                table: "ImageReports",
                column: "BookingReportId",
                principalTable: "BookingReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageReports_BookingReports_BookingReportId",
                table: "ImageReports");

            migrationBuilder.DropTable(
                name: "BookingReports");

            migrationBuilder.DropColumn(
                name: "BannedReason",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "BookingReportId",
                table: "ImageReports",
                newName: "CarReportId");

            migrationBuilder.RenameIndex(
                name: "IX_ImageReports_BookingReportId",
                table: "ImageReports",
                newName: "IX_ImageReports_CarReportId");

            migrationBuilder.CreateTable(
                name: "CarReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarReports_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarReports_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarReports_BookingId",
                table: "CarReports",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_CarReports_CarId",
                table: "CarReports",
                column: "CarId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImageReports_CarReports_CarReportId",
                table: "ImageReports",
                column: "CarReportId",
                principalTable: "CarReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
