using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Migrations
{
    /// <inheritdoc />
    public partial class ChangeEncryptedLicensePlateToNormal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cars_EncryptionKeys_EncryptionKeyId",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Cars_EncryptionKeyId",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "EncryptionKeyId",
                table: "Cars");

            migrationBuilder.RenameColumn(
                name: "EncryptedLicensePlate",
                table: "Cars",
                newName: "LicensePlate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LicensePlate",
                table: "Cars",
                newName: "EncryptedLicensePlate");

            migrationBuilder.AddColumn<Guid>(
                name: "EncryptionKeyId",
                table: "Cars",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Cars_EncryptionKeyId",
                table: "Cars",
                column: "EncryptionKeyId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cars_EncryptionKeys_EncryptionKeyId",
                table: "Cars",
                column: "EncryptionKeyId",
                principalTable: "EncryptionKeys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
