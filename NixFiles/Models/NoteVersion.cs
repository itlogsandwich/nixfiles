namespace NixFiles.Models;

public class NoteVersion
{
    public int Id { get; set; }

    public string NoteName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Note? Note { get; set; }
}
