using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NixFiles.Data;

#nullable disable

namespace NixFiles.Migrations;

[Migration("20260516001000_AddNixRelations")]
[DbContext(typeof(AppDbContext))]
public partial class AddNixRelations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NoteAccessLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                NoteName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                AccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NoteAccessLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_NoteAccessLogs_Notes_NoteName",
                    column: x => x.NoteName,
                    principalTable: "Notes",
                    principalColumn: "Name",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "NoteVersions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                NoteName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NoteVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_NoteVersions_Notes_NoteName",
                    column: x => x.NoteName,
                    principalTable: "Notes",
                    principalColumn: "Name",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Tags",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tags", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "NoteTags",
            columns: table => new
            {
                NoteName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                TagId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NoteTags", x => new { x.NoteName, x.TagId });
                table.ForeignKey(
                    name: "FK_NoteTags_Notes_NoteName",
                    column: x => x.NoteName,
                    principalTable: "Notes",
                    principalColumn: "Name",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_NoteTags_Tags_TagId",
                    column: x => x.TagId,
                    principalTable: "Tags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NoteAccessLogs_NoteName",
            table: "NoteAccessLogs",
            column: "NoteName");

        migrationBuilder.CreateIndex(
            name: "IX_NoteTags_TagId",
            table: "NoteTags",
            column: "TagId");

        migrationBuilder.CreateIndex(
            name: "IX_NoteVersions_NoteName",
            table: "NoteVersions",
            column: "NoteName");

        migrationBuilder.CreateIndex(
            name: "IX_Tags_Name",
            table: "Tags",
            column: "Name",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "NoteAccessLogs");

        migrationBuilder.DropTable(name: "NoteTags");

        migrationBuilder.DropTable(name: "NoteVersions");

        migrationBuilder.DropTable(name: "Tags");
    }
}
