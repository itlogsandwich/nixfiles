namespace NixFiles.Models;

public class NoteAccessLog
{
    public int Id { get; set; }

    public string NoteName { get; set; } = string.Empty;

    public DateTime AccessedAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public Note? Note { get; set; }
}
