namespace NixFiles.Models;

public class BookmarkListViewModel
{
    public IReadOnlyList<BookmarkedNoteViewModel> Bookmarks { get; set; } = [];
}
