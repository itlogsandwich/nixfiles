# NixFiles Development Notes

This file is a short implementation guide for contributors. The user-facing overview, route list, and schema guide live in the repository root `README.md`.

## Current Scope

NixFiles is an ASP.NET Core MVC note app with anonymous note access and optional accounts. Accounts are not required to create or open notes; they only add user-specific bookmarks.

Implemented features:

- URL-addressable notes at `/{name}`
- Optional per-note passwords
- Browser-session unlock state for protected notes
- Markdown editing with EasyMDE and sanitized preview through DOMPurify
- Manual save and server auto-save for existing notes
- Local-only drafts before first manual save
- Expiring notes
- Tags and tag pages
- Note version history and restore
- Image uploads by picker, paste, or drag-and-drop
- ASP.NET Identity registration/login/logout
- Per-user bookmarks

## Backend Notes

- `Program.cs` wires MVC, sessions, EF Core, Identity, and the custom routes.
- `AppDbContext` owns entity configuration and Identity tables.
- `NoteInputRules` is the shared source of truth for note-name validation and tag normalization.
- Protected note checks should use the existing session helpers in `NotesController`.
- Runtime uploads belong under `wwwroot/uploads`; do not commit them.

## Frontend Notes

- The editor page contains the EasyMDE setup and upload/auto-save behavior.
- Existing notes auto-save to `/{name}/save` after a debounce.
- New notes keep content in `localStorage` until the first manual save so password, tag, and expiration choices are not bypassed.
- The theme toggle state is stored in `localStorage` as `nixfiles-theme`.

## Database Notes

- SQL Server / LocalDB is the default database target.
- EF Core migrations are in `Migrations`.
- The app applies migrations automatically only in Development.
- Production migration execution should be handled outside app startup.

## Review Checklist

- Run `dotnet build` before handing off changes.
- Check protected-note paths when changing save, restore, upload, or unlock behavior.
- Keep note-name and tag rules centralized in `NoteInputRules`.
- Do not commit `.vs`, `bin`, `obj`, logs, cookie captures, response dumps, or runtime uploads.
