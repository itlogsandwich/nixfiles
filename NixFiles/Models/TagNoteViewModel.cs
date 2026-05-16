namespace NixFiles.Models;

public class TagNoteViewModel
{
    public string Name { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }

    public bool IsProtected { get; set; }
}
