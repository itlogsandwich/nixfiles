using Microsoft.AspNetCore.Identity;

namespace NixFiles.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
}
