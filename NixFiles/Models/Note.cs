namespace NixFiles.Models;

public class Note
{
    public string Name { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public ICollection<NoteVersion> Versions { get; set; } = new List<NoteVersion>();

    public ICollection<NoteAccessLog> AccessLogs { get; set; } = new List<NoteAccessLog>();

    public ICollection<NoteTag> NoteTags { get; set; } = new List<NoteTag>();

    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
}
