namespace NixFiles.Models;

public class UnlockNoteViewModel
{
    public string Name { get; set; } = string.Empty;

    public string? Password { get; set; }

    public string? ErrorMessage { get; set; }
}
