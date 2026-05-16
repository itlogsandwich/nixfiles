namespace NixFiles.Models;

public class TagDetailsViewModel
{
    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<TagNoteViewModel> Nixes { get; set; } = [];
}
