using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddInpectionSchedueAndInpectionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contracts_BookingId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Cars");

            migrationBuilder.CreateTable(
                name: "InspectionStatuses",
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
                    table.PrimaryKey("PK_InspectionStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InspectionSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicianId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionStatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    InspectionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionSchedules_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InspectionSchedules_InspectionStatuses_InspectionStatusId",
                        column: x => x.InspectionStatusId,
                        principalTable: "InspectionStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InspectionSchedules_Users_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_BookingId",
                table: "Contracts",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InspectionSchedules_CarId",
                table: "InspectionSchedules",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionSchedules_InspectionStatusId",
                table: "InspectionSchedules",
                column: "InspectionStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionSchedules_TechnicianId",
                table: "InspectionSchedules",
                column: "TechnicianId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspectionSchedules");

            migrationBuilder.DropTable(
                name: "InspectionStatuses");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_BookingId",
                table: "Contracts");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Cars",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_BookingId",
                table: "Contracts",
                column: "BookingId");
        }
    }
}
