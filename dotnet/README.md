# Framework 4.7.2 blog

Classic ASP.NET MVC 5 (5.2.9), Razor/WebPages 3.2.9, EF 6.4.4, ASP.NET Identity 2.2.4, and OWIN 4.2.2. Uses a non-SDK C# project, `packages.config`, XML configuration, synchronous repositories/services, and server-rendered views.

> **Optional baseline setup.** To modernize this application, start with the [.NET modernization workflow](../docs/DOTNET-MODERNIZATION.md). Use this setup guide only when preparing or validating the original application.

## Prerequisites

- Windows with the .NET Framework 4.7.2 targeting/developer pack and Framework 4.7.2 or a compatible in-place newer Framework runtime.
- Visual Studio / Build Tools with full Framework MSBuild and C# tools. `dotnet build` alone is not the supported build path.
- IIS Express and SQL Server Express LocalDB (`MSSQLLocalDB`).
- Internet access for the first NuGet restore. Missing Visual Studio web targets are supplied by the pinned, project-local `MSBuild.Microsoft.VisualStudio.Web.targets` package, not a machine installation.

## Restore and build

From this folder in PowerShell:

```powershell
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Install full Framework MSBuild / Visual Studio Build Tools.' }
& $msbuild .\LegacyBlog.sln /restore /p:RestorePackagesConfig=true /p:Configuration=Release /v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Framework build failed.' }
```

`NuGet.Config` fixes the package repository at `dotnet\packages`. Do not pass a relative `RestoreRepositoryPath` that changes this location.

## First run

Choose a local-only password, not a password used elsewhere:

```powershell
$env:BLOG_ADMIN_USER = 'your-admin-name'
$secret = Read-Host 'New local admin password (12-72 UTF-8 bytes, at least 12 characters)' -AsSecureString
$env:BLOG_ADMIN_PASSWORD = [PSCredential]::new('local', $secret).GetNetworkCredential().Password
& "${env:ProgramFiles}\IIS Express\iisexpress.exe" "/path:$PWD\LegacyBlog" /port:5080 /systray:false
```

Open `http://localhost:5080`. If that port is already occupied, choose another port and pass that base URL to the smoke checks. Stop IIS Express with `Q` in its console.

Administrator usernames must have 3-64 ASCII letters, digits, periods, underscores, or hyphens. No account is hardcoded. Missing/invalid first-run credentials fail startup explicitly. Later startups do not change an existing administrator's password, even if environment values differ.

### Storage and configuration

The default connection string in `LegacyBlog\Web.config` points to the named database `LegacyBlogDotNet` on `(LocalDB)\MSSQLLocalDB`, using your Windows identity. Override it per process with `BLOG_DOTNET_CONNECTION_STRING`:

```powershell
$env:BLOG_DOTNET_CONNECTION_STRING = 'Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=MyLocalBlog;Integrated Security=True;MultipleActiveResultSets=True'
```

EF's explicit `CreateDatabaseIfNotExists` initializer creates the initial schema only. It never drops/recreates an existing database. Incompatible schema changes fail rather than silently resetting data; add an explicit EF6 migration before changing an existing schema. A stored seed marker makes sample initialization repeatable without overwriting content.

Logs: `%LOCALAPPDATA%\LegacyBlogDotNet\blog.log`. Unexpected errors are logged there and produce generic responses. Credentials are not intentionally written to logs.

## Verify

Keep IIS Express running, and set the original admin credentials in another PowerShell window:

```powershell
$env:BLOG_ADMIN_USER = 'your-admin-name'
$secret = Read-Host 'Existing local admin password' -AsSecureString
$env:BLOG_ADMIN_PASSWORD = [PSCredential]::new('local', $secret).GetNetworkCredential().Password
..\scripts\Test-Blogs.ps1 -App DotNet -DotNetBaseUrl http://localhost:5080
```

For persistence, create and publish a post, stop IIS Express, start it again with the same connection string, and verify its public URL and the original admin login. Seed records must not multiply or overwrite your edit.

### Verification performed in this workspace

- Release build succeeded against the Framework **4.7.2 reference assemblies**, with the installed in-place **Framework 4.8.1 runtime** hosting IIS Express.
- The expanded disposable regression suite passes **66 HTTP/form/restart assertions** using PowerShell 7. The original 52-assertion smoke suite was also previously verified under Windows PowerShell 5.1.
- A published post and the original administrator login survived a complete IIS Express process restart without bootstrap credentials.
- Disposable test records were cleaned up; baseline counts and seed state were checked separately.
- The desktop public layout was rendered and visually inspected using the installed Edge browser.

Run the automated build, isolated IIS Express/LocalDB setup, and full restart regression test from the repository root on Windows:

```sh
pwsh -File scripts/Test-ApplicationBaseline.ps1 -App DotNet
```

For a clean checkout with missing NuGet packages, append `-Restore`. The runner creates a uniquely named LocalDB instance and temporary database files, restarts IIS Express **without bootstrap credentials**, and removes only its own instance/files when finished. Publication timestamps, comment cascade deletion, original credentials, seed identities, and fixture isolation are checked. These regressions are wired into the [PR validation workflow](../.github/workflows/validate.yml); hosted execution still needs verification after pushing.

## Source-only security exercises

The [vulnerable C# examples](security-examples/VulnerableExamples.cs) deliberately demonstrate SQL concatenation, raw HTML output, and unsalted MD5 password hashing. They sit outside the web project, are not included by its compile globs, and have no controller or startup registration. The baseline runner verifies their training namespace is absent from the built assembly. They are for review/assessment training with synthetic data only. See the [training boundaries](../docs/MODERNIZATION.md#intentional-security-exercises-source-only).

## Troubleshooting and limitations

- Restore missing packages before building. Ensure the 4.7.2 reference assemblies are present; an installed modern .NET SDK is not a substitute.
- If LocalDB is stopped, run `SqlLocalDB start MSSQLLocalDB` under the same Windows user.
- A startup database/credential failure is not a successful empty blog. Check the local log and environment.
- Invalid CSRF tokens return 400. Invalid forms redisplay field-level validation; missing/private posts return 404.
- Content is plain text, not trusted HTML. Route IDs are explicitly bound from the URL, not from posted fields.
- These dependency lines are intentionally old and not certified production-safe. Local HTTP, one administrator, and no rate limiter/email/password reset are deliberate sample boundaries.
