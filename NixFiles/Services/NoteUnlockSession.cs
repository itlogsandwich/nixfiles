using Microsoft.AspNetCore.Http;
using NixFiles.Models;

namespace NixFiles.Services;

public static class NoteUnlockSession
{
    private const string UnlockPrefix = "UnlockedNote:";
    private const string ActiveNoteKey = "ActiveUnlockedNote";

    public static bool IsUnlocked(ISession session, Note note)
    {
        return string.IsNullOrEmpty(note.PasswordHash) ||
            session.GetString(GetUnlockKey(note.Name)) == note.PasswordHash;
    }

    public static void MarkUnlocked(ISession session, Note note)
    {
        if (!string.IsNullOrEmpty(note.PasswordHash))
        {
            session.SetString(GetUnlockKey(note.Name), note.PasswordHash);
            session.SetString(ActiveNoteKey, note.Name);
        }
    }

    public static void Forget(ISession session, string noteName)
    {
        session.Remove(GetUnlockKey(noteName));

        if (string.Equals(session.GetString(ActiveNoteKey), noteName, StringComparison.Ordinal))
        {
            session.Remove(ActiveNoteKey);
        }
    }

    public static void ForgetAll(ISession session)
    {
        foreach (var key in session.Keys.Where(key => key.StartsWith(UnlockPrefix, StringComparison.Ordinal)).ToList())
        {
            session.Remove(key);
        }

        session.Remove(ActiveNoteKey);
    }

    public static string? GetActiveNoteName(ISession session)
    {
        return session.GetString(ActiveNoteKey);
    }

    private static string GetUnlockKey(string noteName)
    {
        return $"{UnlockPrefix}{noteName}";
    }
}
