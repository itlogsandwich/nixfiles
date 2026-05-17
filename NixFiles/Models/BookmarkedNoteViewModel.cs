namespace NixFiles.Models;

public class BookmarkedNoteViewModel
{
    public string Name { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }

    public DateTime BookmarkedAt { get; set; }

    public bool IsProtected { get; set; }
}
