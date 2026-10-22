# .NET modernization

Modernize the ASP.NET MVC 5 blog with the **Modernize CLI**: assess the application, create a plan, approve it, execute the changes, and validate the result. The starting point is [LegacyBlog.sln](../dotnet/LegacyBlog.sln), targeting .NET Framework 4.7.2 with EF6, ASP.NET Identity, OWIN, and LocalDB.

**Intentional legacy baseline:** the application targets .NET Framework 4.7.2, not .NET 10. Modernization belongs on a separate exercise branch after assessment and plan approval; the baseline demo remains MVC 5/EF6/OWIN.

> **Modernization, not application startup, is the goal.** Local setup is an optional supporting step. Codespaces is also optional; its .NET 10 SDK does not provide Windows-only Framework MSBuild, IIS Express, or LocalDB.

## Modernization workflow

### 1. Optional: set up and run the original application

Follow the [.NET setup guide](../dotnet/README.md) on Windows to run the Framework 4.7.2 application with IIS Express, LocalDB, and seeded demo posts. Skip this setup step if the environment is already ready or you are only assessing and planning. Codespaces cannot host this legacy baseline.

Before executing code changes, create a dedicated branch, record the baseline commit, and back up the database. Baseline and regression evidence are still required before approving the resulting changes; skipping setup does not make an unexecuted check pass.

### 2. Assess with Modernize CLI

Install the shared [CLI prerequisites](MODERNIZATION.md#prerequisites) locally, or use the [optional Codespaces workspace](MODERNIZATION.md#using-github-codespaces). Follow its authentication guidance; a repository token alone is not a Copilot credential.

From the repository root in PowerShell 7:

```powershell
modernize --version
gh auth status
Set-Location dotnet
```

Then run **one** of the following assessments.

**Simple** (default issue analysis):

```powershell
modernize assess --source . --model auto
```

**Full analysis** (uses the included [.NET YAML](../dotnet/modernize.yaml): full coverage with codebase insights, cloud-readiness and framework-upgrade domains, `net10.0` comparison target):

```powershell
modernize assess --source . --assess-config .\modernize.yaml --model auto
```

Keep this terminal in the `dotnet` folder through planning and execution. Reports are written under `.github/modernize/assessment/` as HTML by default; select the HTML report from this assessment run when planning. See the [assessment configuration](MODERNIZATION.md#optional-assessment-configuration) for field details. The YAML does not retarget the project or approve an upgrade.

On Linux/Codespaces, findings that require unavailable Framework tooling remain unverified. Use Windows for the legacy build and runtime baseline.

### 3. Generate and approve the plan

Select the report you reviewed, then generate a local plan:

```powershell
Get-ChildItem .\.github\modernize\assessment -Recurse -File | Select-Object -ExpandProperty FullName
$assessmentReport = Read-Host "Paste the full path of the assessment report file"
$goal = @'
Plan modernization of only LegacyBlog.sln in the current folder.
Compare an in-place .NET Framework upgrade with migration to .NET 10
and ASP.NET Core MVC from the Framework 4.7.2 baseline. Treat net10.0
as the configured assessment comparison target, not prior approval.
Explain System.Web, OWIN, Razor, project-format,
EF6, LocalDB, and ASP.NET Identity password-hash compatibility.
Preserve routes, authorization, antiforgery protection, validation,
moderation, persistent data, and original logins.
Include ordered tasks, database backup and rollback, and measurable
build, HTTP, login, and restart-persistence acceptance checks.
Keep execution local. Do not add Azure services or deploy.
Stop after planning for my approval; do not change application code
or install tools before approval.
'@
modernize plan create $goal --plan-name dotnet-blog-upgrade --language dotnet --assess-file-path $assessmentReport --model auto
```

**Stop for approval.** Review `.github/modernize/dotnet-blog-upgrade/plan.md` and `tasks.json`, including the [.NET compatibility checklist](MODERNIZATION.md#net-planning-goal). Approve the target, data strategy, hosting changes, rollback, and validation commands before executing.

### 4. Execute only the approved plan

From the same `dotnet` folder, after approval:

```powershell
modernize plan execute --plan-name dotnet-blog-upgrade --model auto
```

Execution can modify code, dependencies, and commits. Inspect the complete branch history, not just uncommitted changes. Do not substitute `modernize upgrade`, which combines planning and execution.

### 5. Validate and review

Return to the repository root:

```powershell
Set-Location ..
```

The agent's build/CVE checks are not proof of behavioral parity. Complete the checks below against the **approved target**, using a disposable copy of the original database.

#### Build and test the approved target

For an ASP.NET Core / modern .NET SDK-style target, run these commands from the repository root. Adjust the paths if the approved plan renamed the solution or test project:

```powershell
dotnet --info
dotnet restore .\dotnet\LegacyBlog.sln
if ($LASTEXITCODE -ne 0) { throw "Restore failed." }
dotnet build .\dotnet\LegacyBlog.sln --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build failed." }
```

Run the test projects added by the approved migration plan, then the HTTP and restart checks below. The legacy baseline uses the HTTP regression runner, not an ASP.NET Core test project. Confirm any added tests were actually discovered and executed; a run with no tests is not a passing suite. Tests using another database provider do not establish SQL Server compatibility.

For the Framework baseline or a retained Framework target, use the Windows/MSBuild instructions in the [.NET setup guide](../dotnet/README.md#restore-and-build) instead. Do not treat the .NET 10 SDK as a replacement for Framework MSBuild, IIS Express, or LocalDB.

#### Start the upgraded application

1. In the terminal that will run the application, set `BLOG_DOTNET_CONNECTION_STRING` to a **disposable** SQL Server/LocalDB database copy. For example, enter the connection string for that copy at the prompt:

   ```powershell
   $env:BLOG_DOTNET_CONNECTION_STRING = Read-Host "Disposable SQL Server/LocalDB connection string"
   ```

   If the approved migration changes configuration or provider, follow that plan's explicit data-conversion and configuration steps instead. Do not point migration experiments at the only copy of the original data.
2. Preserve the original users and password hashes. A fresh database with a newly seeded administrator does not prove login/data migration compatibility.
3. Start the approved host on loopback.

For an ASP.NET Core target retaining this project path:

```powershell
dotnet run --project .\dotnet\LegacyBlog\LegacyBlog.csproj --configuration Release --no-build --no-launch-profile --urls http://127.0.0.1:5080
```

For a retained Framework target, use IIS Express on Windows according to the approved Framework-baseline instructions. LocalDB also requires Windows, even if the application is now ASP.NET Core.

Review startup/migration errors and confirm the public home page responds before running smoke checks. Keep this server terminal open.

#### Run HTTP and form checks

In another PowerShell 7 terminal at the repository root, enter the **original administrator credentials** and the upgraded application's loopback URL:

```powershell
$env:BLOG_ADMIN_USER = Read-Host "Original administrator username"
$secret = Read-Host "Original administrator password" -AsSecureString
$env:BLOG_ADMIN_PASSWORD = [PSCredential]::new("local", $secret).GetNetworkCredential().Password
$baseUrl = Read-Host "Validation base URL (for example http://localhost:5080)"
.\scripts\Test-Blogs.ps1 -App DotNet -DotNetBaseUrl $baseUrl
```

The [HTTP smoke checks](../scripts/Test-Blogs.ps1) exercise public pages, categories/pagination, private drafts, original administrator login, CRUD, publication timestamps, moderation, CSRF rejection, and invalid/not-found responses. They create and clean up uniquely named test records; use a disposable validation database and inspect any failure. A freshly seeded database does not prove migration of existing data.

#### Prove restart and data persistence

The smoke command above does **not** include a `-RestartApplication` callback, so it does not establish restart persistence by itself. Perform this separate check:

1. Using the running upgraded app, create a uniquely named post, publish it, and approve a comment. Record the content, publication timestamp, and existing seed-record counts.
2. Stop the application process completely, then restart it with the **same database and configuration**. Refreshing a browser page is not an application restart.
3. Open a fresh browser session and sign in with the original administrator account.
4. Verify the post, timestamp, approved comment, and edits survived; seed records must not multiply, reset, or overwrite changes.
5. Remove only the records you created for this check. If a repeatable restart command is part of the approved hosting setup, it can instead be supplied to the smoke script's `-RestartApplication` parameter.

#### Review evidence and CI before merging

- Compare the complete branch against the recorded baseline commit, including agent-created commits, staged changes, and uncommitted files. Confirm that Java remains unchanged.
- Record SDK/runtime/database versions, test counts, executed commands, HTTP and restart results, remaining risks, and anything not tested. A failed or unavailable check is not a pass.
- If routes, forms, configuration, or hosting intentionally changed, review the compatibility decision and update the corresponding checks; do not weaken assertions merely to make them pass.
- Update the setup guide and CI on the modernization branch for the approved target. The [local regression runner](../scripts/Test-ApplicationBaseline.ps1) enforces the Framework 4.7.2 demo baseline and runs IIS Express with disposable LocalDB; adapt its target guards and hosting only on that branch when approving a migration.
- Reconcile dependency/CVE findings with the approved plan, then open a pull request only when the agreed acceptance criteria pass. Update the [verification status](MODERNIZATION.md#verification-status) only with executed evidence.
