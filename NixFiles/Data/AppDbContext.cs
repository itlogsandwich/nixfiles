using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NixFiles.Models;

namespace NixFiles.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Note> Notes => Set<Note>();

    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();

    public DbSet<NoteVersion> NoteVersions => Set<NoteVersion>();

    public DbSet<NoteAccessLog> NoteAccessLogs => Set<NoteAccessLog>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<NoteTag> NoteTags => Set<NoteTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(note => note.Name);

            entity.Property(note => note.Name)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(note => note.Content)
                .IsRequired();

            entity.Property(note => note.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(note => note.UpdatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.HasKey(bookmark => bookmark.Id);

            entity.Property(bookmark => bookmark.NoteName)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(bookmark => bookmark.UserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(bookmark => bookmark.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasIndex(bookmark => new { bookmark.UserId, bookmark.NoteName })
                .IsUnique();

            entity.HasOne(bookmark => bookmark.User)
                .WithMany(user => user.Bookmarks)
                .HasForeignKey(bookmark => bookmark.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(bookmark => bookmark.Note)
                .WithMany(note => note.Bookmarks)
                .HasForeignKey(bookmark => bookmark.NoteName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NoteVersion>(entity =>
        {
            entity.HasKey(version => version.Id);

            entity.Property(version => version.NoteName)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(version => version.Content)
                .IsRequired();

            entity.Property(version => version.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(version => version.Note)
                .WithMany(note => note.Versions)
                .HasForeignKey(version => version.NoteName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NoteAccessLog>(entity =>
        {
            entity.HasKey(log => log.Id);

            entity.Property(log => log.NoteName)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(log => log.IpAddress)
                .HasMaxLength(45);

            entity.Property(log => log.AccessedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(log => log.Note)
                .WithMany(note => note.AccessLogs)
                .HasForeignKey(log => log.NoteName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(tag => tag.Id);

            entity.Property(tag => tag.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(tag => tag.Name)
                .IsUnique();
        });

        modelBuilder.Entity<NoteTag>(entity =>
        {
            entity.HasKey(noteTag => new { noteTag.NoteName, noteTag.TagId });

            entity.Property(noteTag => noteTag.NoteName)
                .HasMaxLength(450)
                .IsRequired();

            entity.HasOne(noteTag => noteTag.Note)
                .WithMany(note => note.NoteTags)
                .HasForeignKey(noteTag => noteTag.NoteName)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(noteTag => noteTag.Tag)
                .WithMany(tag => tag.NoteTags)
                .HasForeignKey(noteTag => noteTag.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
