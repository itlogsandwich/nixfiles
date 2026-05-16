namespace NixFiles.Models;

public class NoteTag
{
    public string NoteName { get; set; } = string.Empty;

    public int TagId { get; set; }

    public Note? Note { get; set; }

    public Tag? Tag { get; set; }
}
