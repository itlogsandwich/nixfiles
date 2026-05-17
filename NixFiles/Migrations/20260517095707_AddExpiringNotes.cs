using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NixFiles.Migrations
{
    /// <inheritdoc />
    public partial class AddExpiringNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Notes",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Notes");
        }
    }
}
