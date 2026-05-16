using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NixFiles.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Notes",
            columns: table => new
            {
                Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notes", x => x.Name);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Notes");
    }
}
