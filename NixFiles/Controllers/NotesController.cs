using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NixFiles.Data;
using NixFiles.Models;
using NixFiles.Services;

namespace NixFiles.Controllers;

public class NotesController(
    AppDbContext dbContext,
    IPasswordHasher<Note> passwordHasher,
    IWebHostEnvironment environment) : Controller
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".gif",
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    [HttpGet]
    public async Task<IActionResult> Open(string name)
    {
        if (!NoteInputRules.IsValidNoteName(name))
        {
            return NotFound();
        }

        var note = await dbContext.Notes
            .SingleOrDefaultAsync(current => current.Name == name);

        if (note is null)
        {
            return View("Editor", new NoteEditorViewModel
            {
                Name = name,
                IsNew = true,
                ExpiresIn = 0
            });
        }

        if (await ExpireIfNeededAsync(note))
        {
            return View("Expired", name);
        }

        if (!string.IsNullOrEmpty(note.PasswordHash) && !IsNoteUnlocked(note))
        {
            return View("Unlock", new UnlockNoteViewModel { Name = name });
        }

        await LogAccessAsync(note.Name);
        await dbContext.SaveChangesAsync();

        var viewModel = await BuildEditorModelAsync(note.Name);
        viewModel.StatusMessage = TempData["StatusMessage"] as string;

        return View("Editor", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Unlock(string name)
    {
        if (!NoteInputRules.IsValidNoteName(name))
        {
            return NotFound();
        }

        var note = await dbContext.Notes
            .SingleOrDefaultAsync(current => current.Name == name);

        if (note is null)
        {
            return RedirectToAction(nameof(Open), new { name });
        }

        if (await ExpireIfNeededAsync(note))
        {
            return View("Expired", name);
        }

        if (string.IsNullOrEmpty(note.PasswordHash) || IsNoteUnlocked(note))
        {
            return RedirectToAction(nameof(Open), new { name });
        }

        return View("Unlock", new UnlockNoteViewModel { Name = note.Name });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string name, UnlockNoteViewModel model)
    {
        if (!NoteInputRules.IsValidNoteName(name))
        {
            return NotFound();
        }

        var note = await dbContext.Notes
            .SingleOrDefaultAsync(current => current.Name == name);

        if (note is null)
        {
            return RedirectToAction(nameof(Open), new { name });
        }

        if (await ExpireIfNeededAsync(note))
        {
            return View("Expired", name);
        }

        if (string.IsNullOrEmpty(note.PasswordHash))
        {
            return RedirectToAction(nameof(Open), new { name });
        }

        if (string.IsNullOrWhiteSpace(model.Password) ||
            passwordHasher.VerifyHashedPassword(note, note.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
        {
            return View("Unlock", new UnlockNoteViewModel
            {
                Name = name,
                ErrorMessage = "The password was not correct."
            });
        }

        await LogAccessAsync(note.Name);
        await dbContext.SaveChangesAsync();
        MarkNoteUnlocked(note);

        var viewModel = await BuildEditorModelAsync(note.Name);
        viewModel.IsProtected = true;
        viewModel.StatusMessage = "Unlocked for this browser session.";

        return View("Editor", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    public async Task<IActionResult> Save(string name, NoteEditorViewModel model)
    {
        if (!NoteInputRules.IsValidNoteName(name))
        {
            return NotFound();
        }

        var result = await SaveNoteAsync(name, model.Content, model.Password, model.TagsText, model.ExpiresIn);

        if (result.Status == SaveNoteStatus.ContentRequired)
        {
            model.Name = name;
            model.ErrorMessage = "Content is required.";
            return View("Editor", model);
        }

        if (result.Status == SaveNoteStatus.Expired)
        {
            return View("Expired", name);
        }

        if (result.Status == SaveNoteStatus.PasswordRequired)
        {
            var viewModel = await BuildEditorModelAsync(name);
            viewModel.Content = model.Content ?? string.Empty;
            viewModel.IsProtected = true;
            viewModel.TagsText = model.TagsText;
            viewModel.ExpiresIn = model.ExpiresIn;
            viewModel.ErrorMessage = "Enter the Nix password to save changes.";
            return View("Editor", viewModel);
        }

        TempData["StatusMessage"] = result.Created ? "Nix created." : "Saved.";
        return RedirectToAction(nameof(Open), new { name });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Consumes("application/json")]
    [ActionName(nameof(Save))]
    public async Task<IActionResult> SaveJson(string name, [FromBody] SaveNoteRequest? model)
    {
        if (!NoteInputRules.IsValidNoteName(name))
        {
            return NotFound();
        }

        if (model is null)
        {
            return BadRequest(new { success = false, error = "Invalid save request." });
        }

        var result = await SaveNoteAsync(name, model.Content, model.Password, model.TagsText, model.ExpiresIn);

        return result.Status switch
        {
            SaveNoteStatus.Saved => Json(new
            {
                success = true,
                created = result.Created,
                updatedAt = result.UpdatedAt,
                expiresAt = result.ExpiresAt
            }),
            SaveNoteStatus.ContentRequired => BadRequest(new { success = false, error = "Content is required." }),
            SaveNoteStatus.PasswordRequired => Unauthorized(new { success = false, error = "Password is required." }),
            SaveNoteStatus.Expired => StatusCode(StatusCodes.Status410Gone, new { success = false, error = "This Nix has expired." }),
            _ => BadRequest(new { success = false, error = "The Nix could not be saved." })
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(string name, int versionId, string? password)
    {
        if (!NoteInputRules.IsValidNoteName(name))
        {
            return NotFound();
        }

        var note = await dbContext.Notes
            .SingleOrDefaultAsync(current => current.Name == name);

        if (note is null)
        {
            return NotFound();
        }

        if (await ExpireIfNeededAsync(note))
        {
            return View("Expired", name);
        }

        if (!string.IsNullOrEmpty(note.PasswordHash) && !IsNoteUnlocked(note))
        {
            if (!PasswordMatches(note, password))
            {
                return View("Unlock", new UnlockNoteViewModel
                {
                    Name = note.Name,
                    ErrorMessage = "Enter the Nix password to restore a version."
                });
            }

            MarkNoteUnlocked(note);
        }

        var version = await dbContext.NoteVersions
            .SingleOrDefaultAsync(current => current.Id == versionId && current.NoteName == note.Name);

        if (version is null)
        {
            return NotFound();
        }

        dbContext.NoteVersions.Add(new NoteVersion
        {
            NoteName = note.Name,
            Content = note.Content,
            CreatedAt = DateTime.UtcNow
        });

        note.Content = version.Content;
        note.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var viewModel = await BuildEditorModelAsync(note.Name);
        viewModel.StatusMessage = $"Restored version from {version.CreatedAt:g}.";
        return View("Editor", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(string name, IFormFile? image)
    {
        if (!NoteInputRules.IsValidNoteName(name))
        {
            return BadRequest(new { error = "Invalid Nix name." });
        }

        var note = await dbContext.Notes.SingleOrDefaultAsync(current => current.Name == name);
        if (note is not null && await ExpireIfNeededAsync(note))
        {
            return StatusCode(StatusCodes.Status410Gone, new { error = "This Nix has expired." });
        }

        if (note is not null && !string.IsNullOrEmpty(note.PasswordHash) && !IsNoteUnlocked(note))
        {
            return Unauthorized(new { error = "Unlock this Nix before attaching images." });
        }

        if (image is null ||
            image.Length == 0 ||
            string.IsNullOrWhiteSpace(image.ContentType) ||
            !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Select an image file." });
        }

        var extension = Path.GetExtension(image.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
        {
            extension = ".png";
        }

        var uploadRoot = Path.Combine(environment.WebRootPath, "uploads", name);
        Directory.CreateDirectory(uploadRoot);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(uploadRoot, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await image.CopyToAsync(stream);
        }

        return Json(new
        {
            url = Url.Content($"~/uploads/{name}/{fileName}")
        });
    }

    [HttpGet]
    public async Task<IActionResult> Tagged(string tagName)
    {
        var normalizedTag = NoteInputRules.NormalizeTag(tagName);
        if (normalizedTag is null)
        {
            return NotFound();
        }

        var now = DateTime.UtcNow;
        var tag = await dbContext.Tags
            .AsNoTracking()
            .Include(current => current.NoteTags)
            .ThenInclude(noteTag => noteTag.Note)
            .SingleOrDefaultAsync(current => current.Name == normalizedTag);

        if (tag is null)
        {
            return NotFound();
        }

        var viewModel = new TagDetailsViewModel
        {
            Name = tag.Name,
            Nixes = tag.NoteTags
                .Where(noteTag => noteTag.Note is not null &&
                    (!noteTag.Note.ExpiresAt.HasValue || noteTag.Note.ExpiresAt > now))
                .Select(noteTag => new TagNoteViewModel
                {
                    Name = noteTag.Note!.Name,
                    UpdatedAt = noteTag.Note.UpdatedAt,
                    IsProtected = !string.IsNullOrEmpty(noteTag.Note.PasswordHash),
                    IsUnlocked = IsNoteUnlocked(noteTag.Note)
                })
                .OrderByDescending(note => note.UpdatedAt)
                .ToList()
        };

        return View("Tagged", viewModel);
    }

    private async Task<NoteEditorViewModel> BuildEditorModelAsync(string name)
    {
        var note = await dbContext.Notes
            .AsNoTracking()
            .SingleAsync(current => current.Name == name);

        var tags = await dbContext.NoteTags
            .AsNoTracking()
            .Where(noteTag => noteTag.NoteName == name)
            .Include(noteTag => noteTag.Tag)
            .Select(noteTag => noteTag.Tag!.Name)
            .OrderBy(tag => tag)
            .ToListAsync();

        var versions = await dbContext.NoteVersions
            .AsNoTracking()
            .Where(version => version.NoteName == name)
            .OrderByDescending(version => version.CreatedAt)
            .Take(8)
            .Select(version => new NoteVersionSummaryViewModel
            {
                Id = version.Id,
                CreatedAt = version.CreatedAt
            })
            .ToListAsync();

        var accessStats = await dbContext.NoteAccessLogs
            .AsNoTracking()
            .Where(log => log.NoteName == name)
            .GroupBy(log => log.NoteName)
            .Select(group => new
            {
                Count = group.Count(),
                LastAccessedAt = group.Max(log => log.AccessedAt)
            })
            .SingleOrDefaultAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isBookmarked = userId is not null &&
            await dbContext.Bookmarks
                .AsNoTracking()
                .AnyAsync(bookmark => bookmark.UserId == userId && bookmark.NoteName == name);

        return new NoteEditorViewModel
        {
            Name = note.Name,
            Content = note.Content,
            IsProtected = !string.IsNullOrEmpty(note.PasswordHash),
            IsUnlocked = IsNoteUnlocked(note),
            IsBookmarked = isBookmarked,
            Tags = tags,
            TagsText = string.Join(", ", tags),
            Versions = versions,
            ViewCount = accessStats?.Count ?? 0,
            LastAccessedAt = accessStats?.LastAccessedAt,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt,
            ExpiresAt = note.ExpiresAt,
            ExpiresIn = note.ExpiresAt.HasValue ? null : 0
        };
    }

    private async Task<SaveNoteResult> SaveNoteAsync(
        string name,
        string? contentValue,
        string? password,
        string? tagsText,
        int? expiresIn)
    {
        var content = contentValue ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
        {
            return SaveNoteResult.ContentRequired();
        }

        var note = await dbContext.Notes
            .Include(current => current.NoteTags)
            .SingleOrDefaultAsync(current => current.Name == name);

        var now = DateTime.UtcNow;
        var created = false;

        if (note is null)
        {
            created = true;
            note = new Note
            {
                Name = name,
                Content = content,
                CreatedAt = now,
                UpdatedAt = now
            };

            if (!string.IsNullOrWhiteSpace(password))
            {
                note.PasswordHash = passwordHasher.HashPassword(note, password);
                MarkNoteUnlocked(note);
            }

            ApplyExpiration(note, expiresIn, now);
            dbContext.Notes.Add(note);
            await dbContext.SaveChangesAsync();
            await UpdateTagsAsync(note.Name, tagsText);
            await dbContext.SaveChangesAsync();

            return SaveNoteResult.Saved(created, note.UpdatedAt, note.ExpiresAt);
        }

        if (await ExpireIfNeededAsync(note))
        {
            return SaveNoteResult.Expired();
        }

        if (!string.IsNullOrEmpty(note.PasswordHash) && !IsNoteUnlocked(note))
        {
            if (!PasswordMatches(note, password))
            {
                return SaveNoteResult.PasswordRequired();
            }

            MarkNoteUnlocked(note);
        }

        if (!string.Equals(note.Content, content, StringComparison.Ordinal))
        {
            dbContext.NoteVersions.Add(new NoteVersion
            {
                NoteName = note.Name,
                Content = note.Content,
                CreatedAt = now
            });
        }

        note.Content = content;
        note.UpdatedAt = now;
        ApplyExpiration(note, expiresIn, now);
        await UpdateTagsAsync(note.Name, tagsText);
        await dbContext.SaveChangesAsync();

        return SaveNoteResult.Saved(created, note.UpdatedAt, note.ExpiresAt);
    }

    private async Task<bool> ExpireIfNeededAsync(Note note)
    {
        if (!note.ExpiresAt.HasValue || note.ExpiresAt > DateTime.UtcNow)
        {
            return false;
        }

        dbContext.Notes.Remove(note);
        await dbContext.SaveChangesAsync();
        return true;
    }

    private static void ApplyExpiration(Note note, int? expiresIn, DateTime now)
    {
        if (!expiresIn.HasValue)
        {
            return;
        }

        note.ExpiresAt = expiresIn.Value <= 0
            ? null
            : now.AddSeconds(expiresIn.Value);
    }

    private async Task UpdateTagsAsync(string noteName, string? tagsText)
    {
        var tags = NoteInputRules.ParseTags(tagsText);

        var existing = await dbContext.NoteTags
            .Where(noteTag => noteTag.NoteName == noteName)
            .ToListAsync();

        dbContext.NoteTags.RemoveRange(existing);

        foreach (var tagName in tags)
        {
            var tag = await dbContext.Tags.SingleOrDefaultAsync(current => current.Name == tagName);
            if (tag is null)
            {
                tag = new Tag { Name = tagName };
                dbContext.Tags.Add(tag);
            }

            dbContext.NoteTags.Add(new NoteTag
            {
                NoteName = noteName,
                Tag = tag
            });
        }
    }

    private Task LogAccessAsync(string noteName)
    {
        dbContext.NoteAccessLogs.Add(new NoteAccessLog
        {
            NoteName = noteName,
            AccessedAt = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        });

        return Task.CompletedTask;
    }

    private bool PasswordMatches(Note note, string? password)
    {
        return !string.IsNullOrWhiteSpace(password) &&
            !string.IsNullOrEmpty(note.PasswordHash) &&
            passwordHasher.VerifyHashedPassword(note, note.PasswordHash, password) != PasswordVerificationResult.Failed;
    }

    private bool IsNoteUnlocked(Note note)
    {
        return string.IsNullOrEmpty(note.PasswordHash) ||
            HttpContext.Session.GetString(GetUnlockSessionKey(note.Name)) == note.PasswordHash;
    }

    private void MarkNoteUnlocked(Note note)
    {
        if (!string.IsNullOrEmpty(note.PasswordHash))
        {
            HttpContext.Session.SetString(GetUnlockSessionKey(note.Name), note.PasswordHash);
        }
    }

    private static string GetUnlockSessionKey(string noteName)
    {
        return $"UnlockedNote:{noteName}";
    }

    private enum SaveNoteStatus
    {
        Saved,
        ContentRequired,
        PasswordRequired,
        Expired
    }

    private sealed record SaveNoteResult(
        SaveNoteStatus Status,
        bool Created = false,
        DateTime? UpdatedAt = null,
        DateTime? ExpiresAt = null)
    {
        public static SaveNoteResult Saved(bool created, DateTime updatedAt, DateTime? expiresAt) =>
            new(SaveNoteStatus.Saved, created, updatedAt, expiresAt);

        public static SaveNoteResult ContentRequired() => new(SaveNoteStatus.ContentRequired);

        public static SaveNoteResult PasswordRequired() => new(SaveNoteStatus.PasswordRequired);

        public static SaveNoteResult Expired() => new(SaveNoteStatus.Expired);
    }
}
