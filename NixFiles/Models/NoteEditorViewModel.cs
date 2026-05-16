namespace NixFiles.Models;

public class NoteEditorViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? Password { get; set; }

    public bool IsNew { get; set; }

    public bool IsProtected { get; set; }

    public string TagsText { get; set; } = string.Empty;

    public IReadOnlyList<string> Tags { get; set; } = [];

    public IReadOnlyList<NoteVersionSummaryViewModel> Versions { get; set; } = [];

    public int ViewCount { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public string? StatusMessage { get; set; }
}
