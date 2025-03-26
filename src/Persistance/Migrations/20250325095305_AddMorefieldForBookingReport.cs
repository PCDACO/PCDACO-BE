using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddMorefieldForBookingReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingReports_Cars_CarId",
                table: "BookingReports");

            migrationBuilder.DropTable(
                name: "Compensations");

            migrationBuilder.RenameColumn(
                name: "CarId",
                table: "BookingReports",
                newName: "CompensationPaidUserId");

            migrationBuilder.RenameIndex(
                name: "IX_BookingReports_CarId",
                table: "BookingReports",
                newName: "IX_BookingReports_CompensationPaidUserId");

            migrationBuilder.AddColumn<decimal>(
                name: "CompensationAmount",
                table: "BookingReports",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompensationPaidAt",
                table: "BookingReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompensationPaidImageUrl",
                table: "BookingReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompensationReason",
                table: "BookingReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompensationPaid",
                table: "BookingReports",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingReports_ResolvedById",
                table: "BookingReports",
                column: "ResolvedById");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingReports_Users_CompensationPaidUserId",
                table: "BookingReports",
                column: "CompensationPaidUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingReports_Users_ResolvedById",
                table: "BookingReports",
                column: "ResolvedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingReports_Users_CompensationPaidUserId",
                table: "BookingReports");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingReports_Users_ResolvedById",
                table: "BookingReports");

            migrationBuilder.DropIndex(
                name: "IX_BookingReports_ResolvedById",
                table: "BookingReports");

            migrationBuilder.DropColumn(
                name: "CompensationAmount",
                table: "BookingReports");

            migrationBuilder.DropColumn(
                name: "CompensationPaidAt",
                table: "BookingReports");

            migrationBuilder.DropColumn(
                name: "CompensationPaidImageUrl",
                table: "BookingReports");

            migrationBuilder.DropColumn(
                name: "CompensationReason",
                table: "BookingReports");

            migrationBuilder.DropColumn(
                name: "IsCompensationPaid",
                table: "BookingReports");

            migrationBuilder.RenameColumn(
                name: "CompensationPaidUserId",
                table: "BookingReports",
                newName: "CarId");

            migrationBuilder.RenameIndex(
                name: "IX_BookingReports_CompensationPaidUserId",
                table: "BookingReports",
                newName: "IX_BookingReports_CarId");

            migrationBuilder.CreateTable(
                name: "Compensations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compensations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compensations_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Compensations_BookingId",
                table: "Compensations",
                column: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingReports_Cars_CarId",
                table: "BookingReports",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id");
        }
    }
}
