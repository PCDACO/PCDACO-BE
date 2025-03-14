using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDataTypeExpireDateForLicense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the old "ExpiryDate" column
            migrationBuilder.DropColumn(name: "ExpiryDate", table: "Licenses");

            // Step 2: Add a new "ExpiryDate" column with DateTimeOffset type
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiryDate",
                table: "Licenses",
                type: "timestamp with time zone",
                nullable: false
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the new column
            migrationBuilder.DropColumn(name: "ExpiryDate", table: "Licenses");

            // Step 2: Add the old "ExpiryDate" column as a string (text)
            migrationBuilder.AddColumn<string>(
                name: "ExpiryDate",
                table: "Licenses",
                type: "text",
                nullable: false,
                defaultValue: ""
            );
        }
    }
}
