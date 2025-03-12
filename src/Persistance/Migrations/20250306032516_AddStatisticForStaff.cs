using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddStatisticForStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalApprovedInspectionSchedule",
                table: "UserStatistics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCreatedInspectionSchedule",
                table: "UserStatistics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalRejectedInspectionSchedule",
                table: "UserStatistics",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalApprovedInspectionSchedule",
                table: "UserStatistics");

            migrationBuilder.DropColumn(
                name: "TotalCreatedInspectionSchedule",
                table: "UserStatistics");

            migrationBuilder.DropColumn(
                name: "TotalRejectedInspectionSchedule",
                table: "UserStatistics");
        }
    }
}
