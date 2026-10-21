# .NET blog: local setup and demo data

The current working application uses **.NET 10, ASP.NET Core MVC, and EF Core**. It creates a **persistent SQLite database with demo content by default** on Windows, Linux, or macOS. No SQL Server installation is needed for this local mode.

> **Optional local setup.** The main exercise is the [.NET modernization workflow](../docs/DOTNET-MODERNIZATION.md). The historical starting point was .NET Framework 4.7.2/MVC 5; Framework MSBuild/IIS Express instructions apply only to that original revision, not this SDK-style working application.

## Prerequisites

- .NET **10 SDK** and internet access for the first NuGet restore.
- [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) (`pwsh`) for these commands and the HTTP checks.
- A writable directory for the local SQLite database.
- Optional: a SQL Server connection, or Windows LocalDB, if explicitly selected instead of SQLite.

The optional Codespace already supplies the SDK and PowerShell. Administrator credentials remain required on first startup; no account/password is hardcoded.

## Restore and build

From this folder in PowerShell 7:

```powershell
dotnet restore .\LegacyBlog.sln
if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
dotnet build .\LegacyBlog.sln --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
dotnet test .\LegacyBlog.Tests\LegacyBlog.Tests.csproj --configuration Release --no-build
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
```

## First run

Choose a local-only password, not a password used elsewhere:

```powershell
$env:BLOG_ADMIN_USER = Read-Host 'New local administrator username'
$secret = Read-Host 'New local admin password (12-72 UTF-8 bytes, at least 12 characters)' -AsSecureString
$env:BLOG_ADMIN_PASSWORD = [PSCredential]::new('local', $secret).GetNetworkCredential().Password
dotnet run --project .\LegacyBlog\LegacyBlog.csproj --configuration Release --no-build --no-launch-profile --urls http://127.0.0.1:5080
```

Open `http://localhost:5080`. If that port is occupied, choose another loopback port and pass that URL to the smoke checks. Stop the app with Ctrl+C.

Administrator usernames must have 3-64 ASCII letters, digits, periods, underscores, or hyphens. No account is hardcoded. Missing/invalid first-run credentials fail startup explicitly. Later startups do not change an existing administrator's password, even if environment values differ.

### Default data

On the first startup of an empty database, the existing bootstrap creates:

- **2 categories:** Engineering and Journal.
- **12 published posts** and **2 private drafts**.
- **2 comments:** one approved and one pending moderation.
- The administrator specified by your environment variables.

The stored `sample-v1` marker prevents duplicate/repeated sample seeding. Restarts retain edits, deletions, original login hashes, and publication timestamps. No prebuilt database or credentials are committed.

### Storage and configuration

Without an explicit SQL Server connection, the file is `LegacyBlog/.local/blog.db` relative to this folder. The path is based on the app's content root, not your terminal's current directory. The `.local` directory and SQLite sidecar files within it are ignored by Git.

| Configuration | Behavior |
| --- | --- |
| `BLOG_DOTNET_DB_PATH` | Optional SQLite file path. Relative paths resolve from the app content root. Missing parent directories are created. Defaults to `.local/blog.db`. |
| `BLOG_DOTNET_CONNECTION_STRING` | Explicit SQL Server connection; takes priority over SQLite and the named connection below. |
| `ConnectionStrings:Blog` | A named .NET configuration entry selecting SQL Server. Its standard environment-variable form is `ConnectionStrings__Blog`. |
| `BLOG_ADMIN_USER` / `BLOG_ADMIN_PASSWORD` | First-run administrator credentials only. They do not reset an existing account. |

For a different persistent SQLite location, set a file path before starting:

```powershell
$env:BLOG_DOTNET_DB_PATH = Join-Path (Join-Path $HOME '.legacyblog-dotnet') 'blog.db'
```

To use Windows LocalDB instead, install/start it and set the connection explicitly:

```powershell
$env:BLOG_DOTNET_CONNECTION_STRING = 'Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=LegacyBlogDotNet;Integrated Security=True;MultipleActiveResultSets=True'
```

SQL Server connection failures never fall back to a fresh SQLite database. Blank explicit connection/path settings fail startup. To return to SQLite, remove the SQL Server override from your process/configuration rather than setting it to an empty string.

Switching providers does **not** migrate existing data. A newly seeded SQLite file is a convenient demo, not proof of SQL Server migration compatibility. Back up existing data and use the approved migration plan for any conversion.

EF Core `EnsureCreated` creates an initial schema only; it does not drop or migrate an existing database. Stop the application before backing up its SQLite files. To deliberately start a new demo, choose a new database path rather than deleting an existing database. Do not run multiple instances against the same file.

Startup/request diagnostics are written through ASP.NET Core logging to the application console. Treat logs as local diagnostics and never save real passwords or connection strings in source control.

## Verify

Keep the app running, and set the original admin credentials in another PowerShell 7 terminal from this folder:

```powershell
$env:BLOG_ADMIN_USER = Read-Host 'Existing local administrator username'
$secret = Read-Host 'Existing local admin password' -AsSecureString
$env:BLOG_ADMIN_PASSWORD = [PSCredential]::new('local', $secret).GetNetworkCredential().Password
..\scripts\Test-Blogs.ps1 -App DotNet -DotNetBaseUrl http://localhost:5080
```

For persistence, create/publish a post, stop the app, start it again with the same database, and verify the public URL and original admin login. Seed records must not multiply or overwrite edits.

### Disposable regression checks

From the repository root:

```sh
pwsh -File scripts/Test-ApplicationBaseline.ps1 -App DotNet
```

Append `-Restore` when packages need restoring. The runner builds/tests the .NET application, starts Kestrel with a temporary SQLite file and disposable test credentials, restarts the process **without bootstrap credentials**, and removes only its own resources afterward. It does not use your normal blog database.

The suite checks HTTP/forms, route-ID overposting, publication timestamps, comment deletion, original logins, seed identities, and restart persistence. The [PR validation workflow](../.github/workflows/validate.yml) provisions .NET 10 and uses the same runner. Hosted CI and native Linux/macOS runs remain unverified locally.

SQL Server provider-selection tests do not establish live SQL Server compatibility. The opt-in SQL Server test requires `BLOG_TEST_SQLSERVER_CONNECTION_STRING` and an appropriately isolated SQL Server instance.

## Source-only security exercises

The [vulnerable C# examples](security-examples/VulnerableExamples.cs) deliberately demonstrate SQL concatenation, raw HTML output, and unsalted MD5 password hashing. They sit outside the web project, are not included by its compile globs, and have no controller or startup registration. The baseline runner verifies their training namespace is absent from the built assembly. They are for review/assessment training with synthetic data only. See the [training boundaries](../docs/MODERNIZATION.md#intentional-security-exercises-source-only).

## Troubleshooting and limitations

- Restore missing packages and ensure .NET 10 is selected by `dotnet --version`.
- SQLite requires a writable database directory. If you explicitly chose LocalDB, run `SqlLocalDB start MSSQLLocalDB` under the same Windows user.
- A startup database/credential failure is not a successful empty blog. Check the application console and configured environment.
- Invalid CSRF tokens return 400. Invalid forms redisplay field-level validation; missing/private posts return 404.
- Content is plain text, not trusted HTML. Route IDs are explicitly bound from the URL, not from posted fields.
- This is not a production template. Local HTTP, one administrator, and no rate limiter/email/password reset are deliberate sample boundaries.
