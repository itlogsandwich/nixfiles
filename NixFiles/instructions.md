# NixFiles MVP Agent Specification

This document serves as the prompt blueprint for autonomous execution. Build a zero-auth, password-optional note-sharing website using **ASP.NET Core 10.0 MVC** and **Microsoft SQL Server**.

---

## 1. Stack Definition
*   **Framework:** ASP.NET Core 10.0 MVC
*   **Database:** Microsoft SQL Server (MSSQL)
*   **ORM:** Entity Framework Core 10.0 (`Microsoft.EntityFrameworkCore.SqlServer`)
*   **Markdown Engine:** Client-side **EasyMDE** via CDN

---

## 2. Database Schema (MSSQL / SSMS)

The system requires a single table named `Notes`. The primary key is the URL slug (`Name`).

### T-SQL DDL Script
```sql
CREATE TABLE [dbo].[Notes] (
    [Name]         NVARCHAR(450)  NOT NULL,
    [Content]      NVARCHAR(MAX)  NOT NULL,
    [PasswordHash] NVARCHAR(MAX)  NULL,
    [CreatedAt]    DATETIME2(7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]    DATETIME2(7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Notes] PRIMARY KEY CLUSTERED ([Name] ASC)
);
```

# 📄 NixFiles — MVP Agent Specification

A zero-auth, password-optional, shareable note system built with **ASP.NET Core MVC** and **SQL Server**.

---

## 🧠 0. Core Concept

NixFiles is a **stateless, anonymous note-sharing platform** where:

* Each note is accessed via a unique URL:

  ```
  /{noteName}
  ```
* Notes can optionally be **password-protected**
* No accounts, no sessions, no authentication system
* First visitor can **create the note if it doesn’t exist**

---

## ⚙️ 1. Tech Stack

| Layer    | Technology                          |
| -------- | ----------------------------------- |
| Backend  | ASP.NET Core 10 MVC                 |
| ORM      | Entity Framework Core 10            |
| Database | Microsoft SQL Server                |
| Frontend | Razor Views + Vanilla JS            |
| Markdown | EasyMDE (CDN)                       |
| Styling  | Minimal CSS (or Bootstrap optional) |

---

## 🗄️ 2. Database Schema

### Table: `Notes`

```sql
CREATE TABLE [dbo].[Notes] (
    [Name]         NVARCHAR(450)  NOT NULL,
    [Content]      NVARCHAR(MAX)  NOT NULL,
    [PasswordHash] NVARCHAR(MAX)  NULL,
    [CreatedAt]    DATETIME2(7)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]    DATETIME2(7)   NOT NULL DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_Notes] PRIMARY KEY CLUSTERED ([Name] ASC)
);
```

### Notes:

* `Name` = unique slug (URL identifier)
* `PasswordHash` = nullable → means public note
* Use **BCrypt or ASP.NET PasswordHasher**

---

## 🧩 3. Core Routes

| Route            | Method | Description         |
| ---------------- | ------ | ------------------- |
| `/`              | GET    | Landing page        |
| `/{name}`        | GET    | Open or create note |
| `/{name}/unlock` | POST   | Submit password     |
| `/{name}/save`   | POST   | Save/update note    |

---

## 🔁 4. Application Flow

### 4.1 Access Note

```text
User visits /test-note

IF note exists:
    IF password exists:
        → Show password prompt
    ELSE:
        → Show note editor

IF note does NOT exist:
    → Show "Create Note" editor
```

---

### 4.2 Password Unlock Flow

```text
User submits password

IF correct:
    → Grant access (temporary, client-side)
ELSE:
    → Show error
```

⚠️ No sessions required:

* Password can be stored temporarily in memory (JS variable)
* Or re-submitted on save

---

### 4.3 Save Note Flow

```text
User edits note → clicks Save

IF note is new:
    → Create row

IF note exists:
    → Validate password (if protected)
    → Update content

Always update:
    UpdatedAt = NOW
```

---

## 🧱 5. Backend Structure

### Models

```csharp
public class Note
{
    public string Name { get; set; }
    public string Content { get; set; }
    public string? PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

### DbContext

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Note> Notes { get; set; }
}
```

---

### Controller: `NotesController`

#### Actions

```csharp
GET /{name}
→ Load or initialize note

POST /{name}/unlock
→ Validate password

POST /{name}/save
→ Create or update note
```

---

## 🖥️ 6. Frontend Requirements

### Editor

* Use **EasyMDE via CDN**

```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/easymde/dist/easymde.min.css">
<script src="https://cdn.jsdelivr.net/npm/easymde/dist/easymde.min.js"></script>
```

---

### Features

* Markdown editor
* Live preview (EasyMDE default)
* Save button
* Password input (optional on create)
* Unlock prompt UI

---

## 🔐 7. Security Rules (MVP Level)

| Concern          | Solution                    |
| ---------------- | --------------------------- |
| Password storage | Hash only (never plaintext) |
| Brute force      | Ignore for MVP              |
| XSS              | Encode output               |
| CSRF             | Basic ASP.NET protection    |
| Validation       | Ensure Name is URL-safe     |

---

## 🧪 8. Validation Rules

### Note Name

* Required
* URL-safe (letters, numbers, dashes)
* Max length: 450

### Content

* Required
* No size limit (MVP)

---

## 🎯 9. MVP Constraints

* ❌ No authentication system

* ❌ No user accounts

* ❌ No note listing/search

* ❌ No file uploads

* ❌ No sharing permissions

* ✅ Direct URL access only

* ✅ Stateless interaction

---

## 🚀 10. Optional Enhancements (Post-MVP)

* Auto-save (debounced)
* Expiring notes
* View-only mode
* Rate limiting
* Syntax highlighting themes
* Copy/share button

---

## 🤖 11. Agent Execution Plan

### Step-by-Step

1. Initialize ASP.NET Core MVC project
2. Add EF Core + SQL Server
3. Create `Note` model + migration
4. Implement DbContext
5. Create `NotesController`
6. Implement routes
7. Build Razor views:

   * Editor page
   * Password prompt
8. Integrate EasyMDE
9. Implement save/unlock logic
10. Test flows:

    * Create note
    * Access public note
    * Access protected note

---

## 🧭 12. Expected Behavior Example

### Scenario

```
User visits:
nixfiles.com/test-note
```

### Outcome

| Case                      | Behavior        |
| ------------------------- | --------------- |
| Note doesn't exist        | Create new note |
| Note exists, no password  | Open directly   |
| Note exists, has password | Prompt password |

---

## 🧾 13. Design Philosophy

* **Minimal friction**
* **URL = identity**
* **Stateless simplicity**
* **Fast creation**
* **Privacy via obscurity + optional password**

---

If you want, I can next:

* Generate the **full ASP.NET MVC project structure**
* Write the **controller + Razor views**
* Or simulate how an **agent (Codex/Claude) would execute this step-by-step with prompts**

Just tell me 👍
