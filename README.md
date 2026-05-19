# NixFiles

NixFiles is an ASP.NET Core MVC note system built around zero-friction access:

> Open a Nix by name. If it isn't there, it simply isn't.

Users can create and open notes without signing in. Accounts are optional and only add convenience features such as bookmarks.

## Features

- Anonymous notes by URL-friendly name
- Markdown editor with live preview through EasyMDE
- Debounced AJAX auto-save
- Manual save fallback
- Local draft backup in browser storage
- Optional note passwords
- Expiring notes with automatic deletion on access
- Tags and tag result pages
- Note version history and restore
- Image upload by file picker, paste, or drag-and-drop
- Access insights such as views and last opened time
- Optional ASP.NET Identity accounts
- Per-user bookmarks at `/me/bookmarks`
- Dark/light theme toggle

## Product Philosophy

NixFiles is anonymous by default, but remembers things only if you ask it to.

Identity is additive, not required. A user can still open `/project-notes`, write content, protect it with a note password, tag it, attach images, and let it expire without ever creating an account. Registering only enables personal bookmark persistence.

## Tech Stack

- ASP.NET Core MVC on .NET 10
- Entity Framework Core 10
- Microsoft SQL Server / LocalDB
- ASP.NET Identity
- Razor views
- Bootstrap
- EasyMDE
- DOMPurify

## Run Locally

Prerequisites:

- .NET 10 SDK
- SQL Server LocalDB or another SQL Server instance
- `dotnet-ef` if you want to run migrations manually

From the repository root:

```powershell
cd NixFiles
dotnet restore
dotnet ef database update
dotnet run
```

Default development database:

```json
"Server=(localdb)\\MSSQLLocalDB;Database=NixFiles;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

The app also applies migrations automatically in Development.

Uploaded images are stored under `NixFiles/wwwroot/uploads` at runtime. That directory is ignored by git.

## Project Structure

| Path | Purpose |
| --- | --- |
| `NixFiles/Controllers` | MVC request handlers for notes, accounts, bookmarks, and home pages. |
| `NixFiles/Data` | EF Core `AppDbContext` and model configuration. |
| `NixFiles/Models` | Entity and view-model classes. |
| `NixFiles/Services` | Shared input rules for note names and tags. |
| `NixFiles/Views` | Razor pages for the editor, account forms, bookmarks, tags, and layout. |
| `NixFiles/wwwroot` | Static CSS, JavaScript, vendored libraries, and runtime uploads. |
| `NixFiles/Migrations` | EF Core schema migrations. |

## Main Routes

| Route | Purpose |
| --- | --- |
| `/` | Open/create a Nix by name |
| `/{name}` | Open a note |
| `/{name}/save` | Save note content manually or through JSON auto-save |
| `/{name}/unlock` | Unlock protected notes |
| `/{name}/restore/{versionId}` | Restore a previous version |
| `/{name}/image` | Upload image attachments |
| `/tags/{tagName}` | View notes by tag |
| `/account/register` | Optional account registration |
| `/account/login` | Optional account login |
| `/me/bookmarks` | Signed-in user's bookmarks |
| `/bookmarks/toggle` | Add/remove a bookmark |

## ASCII System View

```text
+-------------------+        +------------------------+
| Browser / Razor UI | -----> | ASP.NET Core MVC       |
|                   |        | Controllers            |
| - Markdown editor |        | - NotesController      |
| - Auto-save JS    |        | - AccountController    |
| - Bookmark button |        | - BookmarksController  |
+-------------------+        +-----------+------------+
                                         |
                                         v
                              +------------------------+
                              | EF Core AppDbContext   |
                              | IdentityDbContext      |
                              +-----------+------------+
                                          |
                                          v
                              +------------------------+
                              | SQL Server / LocalDB   |
                              | Notes, Tags, Identity, |
                              | Bookmarks, Versions    |
                              +------------------------+
```

## Database ERD

```text
+-------------------+          +-------------------+
| AspNetUsers       |          | Notes             |
|-------------------|          |-------------------|
| PK Id             |          | PK Name           |
| UserName          |          | Content           |
| Email             |          | PasswordHash      |
| PasswordHash      |          | CreatedAt         |
| SecurityStamp     |          | UpdatedAt         |
| ...Identity cols  |          | ExpiresAt         |
+---------+---------+          +----+----+----+----+
          |                         |    |    |
          | 1                       | 1  | 1  | 1
          |                         |    |    |
          | *                       | *  | *  | *
+---------v---------+   +-----------v+ +-v----+----------+   +-------------------+
| Bookmarks         |   | NoteTags    | | NoteVersions   |   | NoteAccessLogs    |
|-------------------|   |-------------| |----------------|   |-------------------|
| PK Id             |   | PK NoteName | | PK Id          |   | PK Id             |
| FK UserId         |   | PK TagId    | | FK NoteName    |   | FK NoteName       |
| FK NoteName       |   +------^------+ | Content        |   | AccessedAt        |
| CreatedAt         |          |        | CreatedAt      |   | IpAddress         |
+-------------------+          |        +----------------+   | UserAgent         |
                               |                             +-------------------+
                               | *
                               | 1
                         +-----+-------------+
                         | Tags              |
                         |-------------------|
                         | PK Id             |
                         | UQ Name           |
                         +-------------------+

ASP.NET Identity support tables:

+-------------------+       +-------------------+
| AspNetRoles       |       | AspNetUserRoles   |
|-------------------|       |-------------------|
| PK Id             |<----->| PK/FK UserId      |
| Name              |       | PK/FK RoleId      |
| NormalizedName    |       +-------------------+
+-------------------+
          |
          | 1
          | *
+---------v---------+
| AspNetRoleClaims  |
|-------------------|
| PK Id             |
| FK RoleId         |
| ClaimType         |
| ClaimValue        |
+-------------------+

+-------------------+       +-------------------+       +-------------------+
| AspNetUserClaims  |       | AspNetUserLogins  |       | AspNetUserTokens  |
|-------------------|       |-------------------|       |-------------------|
| PK Id             |       | PK LoginProvider  |       | PK UserId         |
| FK UserId         |       | PK ProviderKey    |       | PK LoginProvider  |
| ClaimType         |       | FK UserId         |       | PK Name           |
| ClaimValue        |       | ProviderDisplay   |       | Value             |
+-------------------+       +-------------------+       +-------------------+
```

## Table Guide

| Table | Purpose |
| --- | --- |
| `Notes` | Main note records. The note name is the primary key and also the public URL path. Stores content, optional password hash, timestamps, and optional expiration. |
| `NoteVersions` | Stores previous note content when a note changes, allowing restore from history. Deleted automatically with the parent note. |
| `NoteAccessLogs` | Tracks note opens with timestamp, IP address, and user agent for basic insights. Deleted automatically with the parent note. |
| `Tags` | Stores normalized tag names. `Name` is unique. |
| `NoteTags` | Many-to-many join between notes and tags. Composite primary key is `NoteName + TagId`. |
| `AspNetUsers` | ASP.NET Identity user accounts. Accounts are optional and do not own notes. |
| `Bookmarks` | Connects a signed-in user to a note they want to remember. Unique per `UserId + NoteName`. |
| `AspNetRoles` | Standard Identity role records. Present for Identity completeness. |
| `AspNetUserRoles` | Standard Identity user-role join table. |
| `AspNetUserClaims` | Standard Identity claims assigned to users. |
| `AspNetRoleClaims` | Standard Identity claims assigned to roles. |
| `AspNetUserLogins` | Standard Identity external login records. |
| `AspNetUserTokens` | Standard Identity token storage. |

## Feature Behavior

### Auto-Save

The editor listens for content changes, waits for two seconds of inactivity, then posts JSON to `/{name}/save`. The save status moves through idle, typing, saving, saved, password needed, expired, and error states.

Manual Save still works through the same route and redirects back to the canonical `/{name}` URL after saving.

For a brand-new Nix, drafts are kept in browser storage until the first manual Save. This preserves the user's chance to set the initial password, tags, and expiration before the note exists on the server. Once a note exists, server auto-save resumes.

### Expiring Notes

Notes can be set to expire after one hour, one day, or seven days. Expired notes are deleted when accessed and display the expired page instead of the editor.

### Bookmarks

Bookmarks require login, but notes do not. A signed-in user can bookmark any existing non-expired note. Bookmarks are personal shortcuts and do not grant ownership or restrict access.

### Password Protection

Note passwords are per-note, independent of Identity accounts. A protected note asks for the note password before opening the editor. After a successful unlock, that note stays unlocked only while the user remains on that Nix, so manual save, auto-save, and restore actions do not ask for the password again until the user leaves the Nix or logs out.

Image uploads for protected notes also require that the note is unlocked in the current session.

## Security and Operations

- Note passwords are hashed with ASP.NET Core `PasswordHasher<Note>`.
- Account passwords are handled by ASP.NET Identity.
- CSRF validation is enabled on form, JSON save, restore, bookmark, logout, and image upload posts.
- Note names are limited to letters, numbers, and dashes.
- Tags are normalized to lowercase URL-safe labels.
- Development auto-migration is convenient locally, but production deployments should run migrations as an explicit release step.
- The editor depends on EasyMDE and DOMPurify from jsDelivr. For locked-down or offline deployments, vendor those assets into `wwwroot/lib`.

## Repository Hygiene

The `.gitignore` excludes build outputs, Visual Studio local state, logs, scratch response/cookie captures, and runtime uploads. If those files were already tracked before the ignore rules existed, remove them from git with a normal repository cleanup commit instead of editing application code around them.
