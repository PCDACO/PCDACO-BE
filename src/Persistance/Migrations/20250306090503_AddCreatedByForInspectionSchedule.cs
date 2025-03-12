using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByForInspectionSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "InspectionSchedules",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_InspectionSchedules_CreatedBy",
                table: "InspectionSchedules",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionSchedules_Users_CreatedBy",
                table: "InspectionSchedules",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionSchedules_Users_CreatedBy",
                table: "InspectionSchedules");

            migrationBuilder.DropIndex(
                name: "IX_InspectionSchedules_CreatedBy",
                table: "InspectionSchedules");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "InspectionSchedules");
        }
    }
}
