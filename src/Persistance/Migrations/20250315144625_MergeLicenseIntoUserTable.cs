using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class MergeLicenseIntoUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Licenses");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedLicenseNumber",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LicenseApprovedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LicenseExpiryDate",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseImageBackUrl",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LicenseImageFrontUrl",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LicenseImageUploadedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LicenseIsApproved",
                table: "Users",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseRejectReason",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedLicenseNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LicenseApprovedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LicenseExpiryDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LicenseImageBackUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LicenseImageFrontUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LicenseImageUploadedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LicenseIsApproved",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LicenseRejectReason",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptionKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EncryptedLicenseNumber = table.Column<string>(type: "text", nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsApprove = table.Column<bool>(type: "boolean", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LicenseImageBackUrl = table.Column<string>(type: "text", nullable: false),
                    LicenseImageFrontUrl = table.Column<string>(type: "text", nullable: false),
                    RejectReason = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Licenses_EncryptionKeys_EncryptionKeyId",
                        column: x => x.EncryptionKeyId,
                        principalTable: "EncryptionKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Licenses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_EncryptionKeyId",
                table: "Licenses",
                column: "EncryptionKeyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_UserId",
                table: "Licenses",
                column: "UserId",
                unique: true);
        }
    }
}
