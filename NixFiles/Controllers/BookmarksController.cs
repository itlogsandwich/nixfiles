using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NixFiles.Data;
using NixFiles.Models;
using NixFiles.Services;

namespace NixFiles.Controllers;

[Authorize]
public class BookmarksController(AppDbContext dbContext) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var now = DateTime.UtcNow;
        var bookmarks = await dbContext.Bookmarks
            .AsNoTracking()
            .Where(bookmark => bookmark.UserId == userId)
            .Include(bookmark => bookmark.Note)
            .Where(bookmark => bookmark.Note != null &&
                (!bookmark.Note.ExpiresAt.HasValue || bookmark.Note.ExpiresAt > now))
            .OrderByDescending(bookmark => bookmark.CreatedAt)
            .Select(bookmark => new BookmarkedNoteViewModel
            {
                Name = bookmark.NoteName,
                UpdatedAt = bookmark.Note!.UpdatedAt,
                BookmarkedAt = bookmark.CreatedAt,
                IsProtected = !string.IsNullOrEmpty(bookmark.Note.PasswordHash)
            })
            .ToListAsync();

        return View(new BookmarkListViewModel { Bookmarks = bookmarks });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string noteName, string? returnUrl = null)
    {
        if (!NoteInputRules.IsValidNoteName(noteName))
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var note = await dbContext.Notes.SingleOrDefaultAsync(current => current.Name == noteName);
        if (note is null || (note.ExpiresAt.HasValue && note.ExpiresAt <= DateTime.UtcNow))
        {
            return NotFound();
        }

        var bookmark = await dbContext.Bookmarks
            .SingleOrDefaultAsync(current => current.UserId == userId && current.NoteName == noteName);

        if (bookmark is null)
        {
            dbContext.Bookmarks.Add(new Bookmark
            {
                UserId = userId,
                NoteName = noteName,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            dbContext.Bookmarks.Remove(bookmark);
        }

        await dbContext.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Open", "Notes", new { name = noteName });
    }
}
