using System.Text.RegularExpressions;

namespace NixFiles.Services;

public static class NoteInputRules
{
    private static readonly Regex NoteNamePattern = new(
        "^[A-Za-z0-9-]{1,450}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TagSplitter = new(
        "[,\\s]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValidNoteName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) && NoteNamePattern.IsMatch(name);
    }

    public static IReadOnlyList<string> ParseTags(string? tagsText)
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

    public static string? NormalizeTag(string? tag)
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
}
