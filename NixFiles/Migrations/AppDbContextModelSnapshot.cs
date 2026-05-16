using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NixFiles.Data;

#nullable disable

namespace NixFiles.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
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

        modelBuilder.Entity("NixFiles.Models.NoteAccessLog", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            b.Property<DateTime>("AccessedAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            b.Property<string>("IpAddress")
                .HasMaxLength(45)
                .HasColumnType("nvarchar(45)");

            b.Property<string>("NoteName")
                .IsRequired()
                .HasMaxLength(450)
                .HasColumnType("nvarchar(450)");

            b.Property<string>("UserAgent")
                .HasColumnType("nvarchar(max)");

            b.HasKey("Id");

            b.HasIndex("NoteName");

            b.ToTable("NoteAccessLogs");
        });

        modelBuilder.Entity("NixFiles.Models.NoteTag", b =>
        {
            b.Property<string>("NoteName")
                .HasMaxLength(450)
                .HasColumnType("nvarchar(450)");

            b.Property<int>("TagId")
                .HasColumnType("int");

            b.HasKey("NoteName", "TagId");

            b.HasIndex("TagId");

            b.ToTable("NoteTags");
        });

        modelBuilder.Entity("NixFiles.Models.NoteVersion", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            b.Property<string>("Content")
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            b.Property<DateTime>("CreatedAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            b.Property<string>("NoteName")
                .IsRequired()
                .HasMaxLength(450)
                .HasColumnType("nvarchar(450)");

            b.HasKey("Id");

            b.HasIndex("NoteName");

            b.ToTable("NoteVersions");
        });

        modelBuilder.Entity("NixFiles.Models.Tag", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            b.HasKey("Id");

            b.HasIndex("Name")
                .IsUnique();

            b.ToTable("Tags");
        });

        modelBuilder.Entity("NixFiles.Models.NoteAccessLog", b =>
        {
            b.HasOne("NixFiles.Models.Note", "Note")
                .WithMany("AccessLogs")
                .HasForeignKey("NoteName")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Note");
        });

        modelBuilder.Entity("NixFiles.Models.NoteTag", b =>
        {
            b.HasOne("NixFiles.Models.Note", "Note")
                .WithMany("NoteTags")
                .HasForeignKey("NoteName")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("NixFiles.Models.Tag", "Tag")
                .WithMany("NoteTags")
                .HasForeignKey("TagId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Note");

            b.Navigation("Tag");
        });

        modelBuilder.Entity("NixFiles.Models.NoteVersion", b =>
        {
            b.HasOne("NixFiles.Models.Note", "Note")
                .WithMany("Versions")
                .HasForeignKey("NoteName")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Note");
        });

        modelBuilder.Entity("NixFiles.Models.Note", b =>
        {
            b.Navigation("AccessLogs");

            b.Navigation("NoteTags");

            b.Navigation("Versions");
        });

        modelBuilder.Entity("NixFiles.Models.Tag", b =>
        {
            b.Navigation("NoteTags");
        });
    }
}
