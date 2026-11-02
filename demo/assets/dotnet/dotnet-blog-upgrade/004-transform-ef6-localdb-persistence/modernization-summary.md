# Task 004 — EF6/LocalDB Persistence Modernization Summary

## Scope completed
- Preserved LocalDB-backed persistence behavior for posts, comments, moderation state, and seed markers while keeping runtime on the modernized EF Core path.
- Added startup compatibility guards so existing databases are validated before acceptance checks continue.
- Added explicit operational scripts for backup, rollback, and schema/data compatibility verification.

## Code changes
- `LegacyBlog/Program.cs`
  - Replaced direct startup initialization (`EnsureCreated` + `Bootstrap.Initialize`) with:
    - `LocalDbPersistenceGuard.EnsureCompatibleAndInitialize(db, connectionString)`
- `LegacyBlog/Data/LocalDbPersistenceGuard.cs` (new)
  - Detects whether the target LocalDB database already exists.
  - Ensures database creation for new installations.
  - For existing databases, validates:
    - required tables/columns (`AspNetUsers`, `Categories`, `Posts`, `Comments`, `SeedStates`)
    - required uniqueness on `Categories.Slug` and `Posts.Slug`
    - relational integrity (no orphaned `Posts.CategoryId` or `Comments.PostId`)
  - Throws explicit startup errors if compatibility issues are found so data is not silently mutated or replaced.
  - Runs `Bootstrap.Initialize` after compatibility checks.

## Backup procedure (pre-change)
1. Choose target database name (example: `LegacyBlogDotNet`) and backup path (example: `C:\backups\LegacyBlogDotNet-prechange.bak`).
2. Run:
   ```powershell
   pwsh -File .\LegacyBlog\ops\Backup-LocalDb.ps1 -DatabaseName LegacyBlogDotNet -BackupFilePath C:\backups\LegacyBlogDotNet-prechange.bak
   ```
3. Confirm script reports `Backup completed`.

## Schema/data compatibility checks
Run before acceptance validation:
```powershell
pwsh -File .\LegacyBlog\ops\Test-LocalDbSchemaCompatibility.ps1 -DatabaseName LegacyBlogDotNet
```
Expected result:
- `Schema/data compatibility check passed`

## Rollback procedure (if acceptance fails)
1. Stop running app/processes that may hold DB connections.
2. Restore from backup:
   ```powershell
   pwsh -File .\LegacyBlog\ops\Restore-LocalDb.ps1 -DatabaseName LegacyBlogDotNet -BackupFilePath C:\backups\LegacyBlogDotNet-prechange.bak
   ```
3. Re-run compatibility check:
   ```powershell
   pwsh -File .\LegacyBlog\ops\Test-LocalDbSchemaCompatibility.ps1 -DatabaseName LegacyBlogDotNet
   ```
4. Restart app with same `BLOG_DOTNET_CONNECTION_STRING`.

## Verification
- `dotnet build LegacyBlog.sln` ✅
- `dotnet test LegacyBlog.sln --no-build` ✅
- Consistency validation reported no Critical/Major issues for task 004 ✅
