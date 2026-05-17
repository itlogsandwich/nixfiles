namespace NixFiles.Models;

public class SaveNoteRequest
{
    public string? Content { get; set; }

    public string? Password { get; set; }

    public string? TagsText { get; set; }

    public int? ExpiresIn { get; set; }
}
