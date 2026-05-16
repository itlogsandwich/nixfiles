using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NixFiles.Data;
using NixFiles.Models;

namespace NixFiles.Controllers;

public class NotesController(
    AppDbContext dbContext,
    IPasswordHasher<Note> passwordHasher,
    IWebHostEnvironment environment) : Controller
{
    private static readonly Regex NoteNamePattern = new(
        "^[A-Za-z0-9-]{1,450}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TagSplitter = new(
        "[,\\s]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
        if (!IsValidName(name))
        {
            return NotFound();
        }

        var note = await dbContext.Notes
            .AsNoTracking()
            .SingleOrDefaultAsync(current => current.Name == name);

        if (note is null)
        {
            return View("Editor", new NoteEditorViewModel
            {
                Name = name,
                IsNew = true
            });
        }

        if (!string.IsNullOrEmpty(note.PasswordHash))
        {
            return View("Unlock", new UnlockNoteViewModel { Name = name });
        }

        await LogAccessAsync(note.Name);
        await dbContext.SaveChangesAsync();

        return View("Editor", await BuildEditorModelAsync(note.Name));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string name, UnlockNoteViewModel model)
    {
        if (!IsValidName(name))
        {
            return NotFound();
        }

        var note = await dbContext.Notes
            .AsNoTracking()
            .SingleOrDefaultAsync(current => current.Name == name);

        if (note is null)
        {
            return RedirectToAction(nameof(Open), new { name });
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

        var viewModel = await BuildEditorModelAsync(note.Name);
        viewModel.IsProtected = true;
        viewModel.StatusMessage = "Unlocked. Enter the password again when saving changes.";

        return View("Editor", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string name, NoteEditorViewModel model)
    {
        if (!IsValidName(name))
        {
            return NotFound();
        }

        var content = model.Content ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
        {
            model.Name = name;
            model.ErrorMessage = "Content is required.";
            return View("Editor", model);
        }

        var note = await dbContext.Notes
            .Include(current => current.NoteTags)
            .SingleOrDefaultAsync(current => current.Name == name);

        if (note is null)
        {
            note = new Note
            {
                Name = name,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                note.PasswordHash = passwordHasher.HashPassword(note, model.Password);
            }

            dbContext.Notes.Add(note);
            await dbContext.SaveChangesAsync();
            await UpdateTagsAsync(note.Name, model.TagsText);
            await dbContext.SaveChangesAsync();

            var viewModel = await BuildEditorModelAsync(note.Name);
            viewModel.StatusMessage = "Nix created.";
            return View("Editor", viewModel);
        }

        if (!string.IsNullOrEmpty(note.PasswordHash) && !PasswordMatches(note, model.Password))
        {
            var viewModel = await BuildEditorModelAsync(note.Name);
            viewModel.Content = content;
            viewModel.IsProtected = true;
            viewModel.TagsText = model.TagsText;
            viewModel.ErrorMessage = "Enter the Nix password to save changes.";
            return View("Editor", viewModel);
        }

        if (!string.Equals(note.Content, content, StringComparison.Ordinal))
        {
            dbContext.NoteVersions.Add(new NoteVersion
            {
                NoteName = note.Name,
                Content = note.Content,
                CreatedAt = DateTime.UtcNow
            });
        }

        note.Content = content;
        note.UpdatedAt = DateTime.UtcNow;
        await UpdateTagsAsync(note.Name, model.TagsText);
        await dbContext.SaveChangesAsync();

        var savedModel = await BuildEditorModelAsync(note.Name);
        savedModel.StatusMessage = "Saved.";
        return View("Editor", savedModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(string name, int versionId, string? password)
    {
        if (!IsValidName(name))
        {
            return NotFound();
        }

        var note = await dbContext.Notes
            .SingleOrDefaultAsync(current => current.Name == name);

        if (note is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(note.PasswordHash) && !PasswordMatches(note, password))
        {
            var lockedModel = await BuildEditorModelAsync(note.Name);
            lockedModel.IsProtected = true;
            lockedModel.ErrorMessage = "Enter the Nix password to restore a version.";
            return View("Editor", lockedModel);
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
    public async Task<IActionResult> UploadImage(string name, IFormFile image)
    {
        if (!IsValidName(name))
        {
            return BadRequest(new { error = "Invalid Nix name." });
        }

        if (image.Length == 0 || !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
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
        var normalizedTag = NormalizeTag(tagName);
        if (normalizedTag is null)
        {
            return NotFound();
        }

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
                .Where(noteTag => noteTag.Note is not null)
                .Select(noteTag => new TagNoteViewModel
                {
                    Name = noteTag.Note!.Name,
                    UpdatedAt = noteTag.Note.UpdatedAt,
                    IsProtected = !string.IsNullOrEmpty(noteTag.Note.PasswordHash)
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

        return new NoteEditorViewModel
        {
            Name = note.Name,
            Content = note.Content,
            IsProtected = !string.IsNullOrEmpty(note.PasswordHash),
            Tags = tags,
            TagsText = string.Join(", ", tags),
            Versions = versions,
            ViewCount = accessStats?.Count ?? 0,
            LastAccessedAt = accessStats?.LastAccessedAt,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }

    private async Task UpdateTagsAsync(string noteName, string? tagsText)
    {
        var tags = ParseTags(tagsText);

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

    private static IReadOnlyList<string> ParseTags(string? tagsText)
    {
        if (string.IsNullOrWhiteSpace(tagsText))
        {
            return [];
        }

        return TagSplitter.Split(tagsText)
            .Select(NormalizeTag)
            .Where(tag => tag is not null)
            .Select(tag => tag!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
    }

    private static string? NormalizeTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        var normalized = tag.Trim().TrimStart('#').ToLowerInvariant();
        normalized = Regex.Replace(normalized, "[^a-z0-9-]", "-");
        normalized = Regex.Replace(normalized, "-{2,}", "-").Trim('-');

        return normalized is { Length: > 0 and <= 100 } ? normalized : null;
    }

    private static bool IsValidName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) && NoteNamePattern.IsMatch(name);
    }
}
