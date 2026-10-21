# .NET modernization

Modernize the ASP.NET MVC 5 blog with the **Modernize CLI**: assess the application, create a plan, approve it, execute the changes, and validate the result. The starting point is [LegacyBlog.sln](../dotnet/LegacyBlog.sln), targeting .NET Framework 4.7.2 with EF6, ASP.NET Identity, OWIN, and LocalDB.

> **Modernization, not application startup, is the goal.** Local setup is an optional supporting step. Codespaces is also optional; its .NET 10 SDK does not provide Windows-only Framework MSBuild, IIS Express, or LocalDB.

## Modernization workflow

### 1. Optional: set up and run the original application

Follow the [.NET setup guide](../dotnet/README.md) only if you need to install the legacy tools, launch the original blog, or record its baseline behavior. Skip this setup step if the environment is already ready or you are only assessing and planning.

Before executing code changes, create a dedicated branch, record the baseline commit, and back up the database. Baseline and regression evidence are still required before approving the resulting changes; skipping setup does not make an unexecuted check pass.

### 2. Assess with Modernize CLI

Install the shared [CLI prerequisites](MODERNIZATION.md#prerequisites) locally, or use the [optional Codespaces workspace](MODERNIZATION.md#using-github-codespaces). Follow its authentication guidance; a repository token alone is not a Copilot credential.

From the repository root in PowerShell 7:

```powershell
modernize --version
gh auth status
Set-Location dotnet
modernize assess --format markdown --delegate local
```

Keep this terminal in the `dotnet` folder through execution. Review reports under `.github/modernize/assessment/`. To customize coverage or the .NET 10 comparison target, use the [optional assessment YAML](MODERNIZATION.md#optional-assessment-configuration) instead of the default assessment command.

On Linux/Codespaces, findings that require unavailable Framework tooling remain unverified. Use Windows for the legacy build and runtime baseline.

### 3. Generate and approve the plan

Select the report you reviewed, then generate a local plan:

```powershell
Get-ChildItem .\.github\modernize\assessment -Recurse -File | Select-Object -ExpandProperty FullName
$assessmentReport = Read-Host "Paste the full path of the assessment report file"
$goal = @'
Plan modernization of only LegacyBlog.sln in the current folder.
Compare an in-place .NET Framework upgrade with migration to .NET 10
and ASP.NET Core MVC. Explain System.Web, OWIN, Razor, project-format,
EF6, LocalDB, and ASP.NET Identity password-hash compatibility.
Preserve routes, authorization, antiforgery protection, validation,
moderation, persistent data, and original logins.
Include ordered tasks, database backup and rollback, and measurable
build, HTTP, login, and restart-persistence acceptance checks.
Keep execution local. Do not add Azure services or deploy.
Stop after planning for my approval; do not change application code
or install tools before approval.
'@
modernize plan create $goal --plan-name dotnet-blog-upgrade --language dotnet --assess-file-path $assessmentReport --delegate local
```

**Stop for approval.** Review `.github/modernize/dotnet-blog-upgrade/plan.md` and `tasks.json`, including the [.NET compatibility checklist](MODERNIZATION.md#net-planning-goal). Approve the target, data strategy, hosting changes, rollback, and validation commands before executing.

### 4. Execute only the approved plan

From the same `dotnet` folder, after approval:

```powershell
modernize plan execute --plan-name dotnet-blog-upgrade --delegate local
```

Execution can modify code, dependencies, and commits. Inspect the complete branch history, not just uncommitted changes. Do not substitute `modernize upgrade`, which combines planning and execution.

### 5. Validate and review

Return to the repository root:

```powershell
Set-Location ..
```

Follow the [post-modernization validation steps](MODERNIZATION.md#6-validate-and-review-the-results) with the approved target toolchain. Verify routes, administrator login, authorization, CSRF rejection, CRUD, moderation, and data persistence after restart.

The [existing baseline checks](MODERNIZATION.md#required-regression-checks) validate the Framework application on Windows. If the plan migrates to ASP.NET Core, update the build, hosting, and CI commands accordingly rather than treating legacy CI as target-runtime evidence. Update the setup guide with the verified new commands and open a pull request for review.
