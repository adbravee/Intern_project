# ItemProcessingApp

A full-featured ASP.NET Core MVC app that manages Items in a recursive **parent/child (tree) hierarchy**, backed by SQL Server using **Entity Framework Core**.

It covers the full assignment flow: login, item CRUD (plus search), building the parent/child tree, and “processing” an item by creating one or more output child items.

What’s included:

- CRUD for items (with validation)
- Tree hierarchy with unlimited depth
- AddChild support (attach existing items)
- Circular reference prevention
- Cookie-based login (credentials in `appsettings.json`)
- Process Item + Processed Items pages
- Expandable Tree View UI

---

## 📁 Project Structure

```
ItemProcessingApp/
├── Controllers/
│   ├── AccountController.cs      # Login / Logout
│   ├── HomeController.cs         # Error page
│   └── ItemsController.cs        # Full CRUD + Tree + Search + AddChild
├── Data/
│   └── AppDbContext.cs           # EF Core DbContext
├── Migrations/
│   ├── 20240101000000_InitialCreate.cs
│   ├── 20240101000000_InitialCreate.Designer.cs
│   └── AppDbContextModelSnapshot.cs
├── Models/
│   ├── Item.cs                   # Item entity (self-referencing)
│   ├── ItemTreeNode.cs           # View-model for tree rendering
│   └── LoginViewModel.cs         # Login form model
├── Views/
│   ├── Account/
│   │   ├── AccessDenied.cshtml
│   │   └── Login.cshtml
│   ├── Home/
│   │   └── Error.cshtml
│   ├── Items/
│   │   ├── _TreeNode.cshtml      # Recursive tree node partial
│   │   ├── AddChild.cshtml
│   │   ├── Create.cshtml
│   │   ├── Delete.cshtml
│   │   ├── Details.cshtml
│   │   ├── Edit.cshtml
│   │   ├── Index.cshtml
│   │   └── Tree.cshtml
│   └── Shared/
│       ├── _Layout.cshtml
│       └── _ValidationScriptsPartial.cshtml
├── wwwroot/
│   └── css/
│       └── site.css
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
├── ItemProcessingApp.csproj
├── setup.sql                     # Standalone SQL script (alternative to migrations)
└── README.md
```

---

## 🛠️ Prerequisites

You’ll need:

- [.NET SDK 7+](https://dotnet.microsoft.com/download) (check with `dotnet --version`)
- A working SQL Server instance (LocalDB / Express / full SQL Server)
- Any editor/IDE that can run `dotnet` (VS Code is fine)

---

## ⚙️ Setup — Step by Step

### Step 1 — Clone / copy the project

```bash
cd your-workspace
# (place the ItemProcessingApp folder here)
cd ItemProcessingApp
```

### Step 2 — Configure the connection string

Open **`appsettings.json`** and update the `DefaultConnection` to match your SQL Server:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=ItemDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

If you are using Windows authentication, `Trusted_Connection=True` is fine.
If your SQL Server expects username/password, remove `Trusted_Connection=True` and set `User Id=...;Password=...` instead.

### Step 3 — Database setup (choose ONE option)

#### Option A — EF Migrations
By default, this project does **not** run EF migrations automatically on startup.
You can either run migrations manually, or enable the startup option.
In `appsettings.json` you’ll find:

```json
"AutoMigrateOnStartup": false
```

```bash
dotnet ef database update
```

#### Option B — Raw SQL script (recommended)
Run `setup.sql` in SSMS, Azure Data Studio, or via `sqlcmd`:

```bash
sqlcmd -S . -E -i setup.sql
```

This script creates the `ItemDB` database, the `Items` table, and it also records the EF migration in `__EFMigrationsHistory` so EF won’t try to duplicate the same migration later.

### Step 4 — Restore NuGet packages

```bash
dotnet restore
```

### Step 5 — Run the application

```bash
dotnet run
```

When it starts, it will print the URLs. Open them in your browser.
Common examples:
- `http://localhost:5000`
- `https://localhost:5001`

---

## 🔐 Login Credentials

Default credentials (configurable in `appsettings.json` under `Admin:`):

| Field | Value |
|---|---|
| Username | `admin` |
| Password | `Admin@123` |

> ⚠️ **Change these before deploying to production!**

---

## 🗄️ Database Schema

```sql
CREATE TABLE Items (
    Id       INT            PRIMARY KEY IDENTITY(1,1),
    Name     NVARCHAR(100)  NOT NULL,
    Weight   DECIMAL(18,3)  NOT NULL,          -- must be > 0 (app-enforced)
    ParentId INT            NULL,              -- NULL = root item
    FOREIGN KEY (ParentId) REFERENCES Items(Id) ON DELETE NO ACTION
);

CREATE INDEX IX_Items_ParentId ON Items (ParentId);
```

The `ParentId` self-referencing foreign key is what enables the unlimited-depth tree.

---

## 🔄 Tree Logic

```
Root A  (ParentId = NULL)
├── Child A1  (ParentId = A.Id)
│   ├── Grandchild A1-i   (ParentId = A1.Id)
│   └── Grandchild A1-ii  (ParentId = A1.Id)
└── Child A2  (ParentId = A.Id)

Root B  (ParentId = NULL)
└── Child B1  (ParentId = B.Id)
    └── Grandchild B1-i  (ParentId = B1.Id)
```

- **Circular reference prevention** — when editing or adding a child, the controller walks up the ancestry chain before saving.
- **Delete safety** — a parent cannot be deleted while it still has children.
- **Single-query load** — `Tree` action loads all items in one DB query, then builds the hierarchy in memory for efficiency.

---

## 🚀 Key Endpoints

Here are the important pages:

- `/Account/Login` (GET/POST): sign in
- `/Items` (GET): list items and search by name
- `/Items/Create` (GET/POST): create an item
- `/Items/Edit/{id}` (GET/POST): edit an item
- `/Items/Delete/{id}` (GET/POST): delete (blocked if it has children)
- `/Items/Details/{id}` (GET): view item details + direct children
- `/Items/AddChild/{id}` (GET/POST): attach an existing item under a parent
- `/Items/Process` (GET/POST): pick a parent and create 1+ output items (outputs are the parent’s children)
- `/Items/Processed` (GET): list items that already have children
- `/Items/Tree` (GET): expandable tree view (good for verifying the hierarchy)

---

## 🔧 Useful dotnet CLI Commands

```bash
# Run with hot-reload (development)
dotnet watch run

# Build only
dotnet build

# Add a new EF migration (after changing Models)
dotnet ef migrations add <MigrationName>

# Apply pending migrations
dotnet ef database update

# Revert last migration
dotnet ef database update <PreviousMigrationName>
```

---

## 🐛 Troubleshooting

If something goes wrong, these are the usual fixes:

- SQL Server connection fails:
  - check `ConnectionStrings:DefaultConnection` in `appsettings.json`
  - confirm your SQL Server instance is reachable
  - make sure you created the DB/table using `setup.sql` (or enabled migrations)

- App starts but item pages fail with migration/database errors:
  - by default `AutoMigrateOnStartup` is `false`
  - either run migrations manually or set `AutoMigrateOnStartup` to `true`

- “Cannot delete” / circular reference messages:
  - that’s expected behavior. The app prevents deleting a parent that still has children and it blocks circular parent assignments.

- Login isn’t working:
  - confirm `Admin:Username` and `Admin:Password` in `appsettings.json`

---

## 📝 Notes for Beginners

- **MVC pattern**: Models define data, Controllers handle logic, Views render HTML.
- **Tag Helpers** (`asp-for`, `asp-action`, etc.) generate correct HTML attributes automatically.
- **EF Core** maps C# classes to database tables — no raw SQL needed for CRUD.
- The `[Authorize]` attribute on `ItemsController` redirects unauthenticated users to the login page.
- `TempData["Success"]` / `TempData["Error"]` persist messages across redirects (shown in the layout).
