using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NixFiles.Data;

#nullable disable

namespace NixFiles.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260516000000_InitialCreate")]
partial class InitialCreate
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity("NixFiles.Models.Note", b =>
        {
            b.Property<string>("Name")
                .HasMaxLength(450)
                .HasColumnType("nvarchar(450)");

            b.Property<string>("Content")
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            b.Property<DateTime>("CreatedAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            b.Property<string>("PasswordHash")
                .HasColumnType("nvarchar(max)");

            b.Property<DateTime>("UpdatedAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            b.HasKey("Name");

            b.ToTable("Notes");
        });
    }
}
