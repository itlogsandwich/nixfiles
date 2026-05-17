namespace NixFiles.Models;

public class Bookmark
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public string NoteName { get; set; } = string.Empty;

    public Note? Note { get; set; }

    public DateTime CreatedAt { get; set; }
}
