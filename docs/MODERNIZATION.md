<!-- repository-header:start -->
# Modernization guide

<p><strong>LEGACY CODE. CLEAR DECISIONS. PROVEN CHANGES.</strong></p>

A practical field guide for the **[GitHub Copilot modernization agent](https://learn.microsoft.com/en-us/azure/developer/github-copilot-app-modernization/modernization-agent/overview)**. Two legacy blog applications. One repeatable **Assess -> Plan -> Execute** workflow. Regression evidence before every merge.

[Start here](#start-here) &middot; [Follow the workflow](#modernization-workflow) &middot; [View the presentation](presentation.html) &middot; [Automate assessment](#optional-automated-assessment) &middot; [Run regression checks](#required-regression-checks)

| Two real applications | Three explicit stages | One evidence bar |
| --- | --- | --- |
| .NET Framework 4.7.2 + Java 11 | Assess, plan, execute | Build, HTTP behavior, and restart persistence |
<!-- repository-header:end -->

## Start here

Choose your path through the repository:

| Your goal | Start with |
| --- | --- |
| Understand the agent and this repository | [14-slide HTML presentation](presentation.html) |
| Assess and modernize one application | [Guided modernization workflow](#modernization-workflow) |
| Build and run the original applications | [.NET setup](../dotnet/README.md) or [Java setup](../java/README.md) |
| Run assessments automatically | [GitHub Actions and Azure DevOps templates](#optional-automated-assessment) |
| Verify changes before merging | [Required regression checks](#required-regression-checks) |
| Practice finding security issues safely | [Isolated source-only exercises](#intentional-security-exercises-source-only) |

> **Practice workspace, not a completed migration.** The [verification status](#verification-status) records what has actually been checked. Runtime upgrades come first; Azure migration and deployment are optional, separately approved work.

The applications are independent, feature-equivalent blogs: ASP.NET MVC 5 on .NET Framework 4.7.2 and Spring MVC on Java 11. The workflow uses the **Modernize CLI**, not the IDE extension's chat commands. This guide and the setup guides are also published through [GitHub Pages](#documentation-website-github-pages).

<details>
<summary><strong>Presenting this repository</strong></summary>
<p>Download the self-contained HTML deck linked above and open it in a browser. It needs no server, dependencies, or internet connection to view the slides. On GitHub Pages, select <strong>Presentation</strong>.</p>
<p>Use the chapter bar to switch between overview, workflow, automation, and demo. Use arrow keys or Space to navigate, Home/End to jump, <strong>Slides</strong> for the contents, and <strong>Print / PDF</strong> to print all 14 slides. Light/dark themes, fullscreen, mobile layouts, and direct links such as <code>#slide-8</code> are supported.</p>
</details>

## Prerequisites

- A GitHub account with Copilot access under a Free, Pro, Pro+, Business, or Enterprise plan.
- [GitHub CLI](https://cli.github.com/) (`gh`) version **2.45.0 or later** for authentication.
- The **Modernize CLI**, installed as described in step 1: Windows x64/ARM64, Linux x64/ARM64 with glibc 2.27 or later, or macOS on Apple Silicon/Intel.
- Git, dependency-feed access, and the build/runtime tools described in the [Framework setup](../dotnet/README.md) and [Java setup](../java/README.md).
- Source and proposed target SDKs/JDKs as required by the selected upgrade. Keep the Framework MSBuild tools installed until the legacy .NET baseline no longer needs them.
- [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) (`pwsh`) on any platform to run the HTTP smoke checks.

VS Code is optional for reviewing and editing the generated plan; its modernization extension is not required for this CLI workflow. Use the current [agent quickstart](https://learn.microsoft.com/en-us/azure/developer/github-copilot-app-modernization/modernization-agent/quickstart) if installation or menu options change. Cloud assessment or deployment steps can have additional prerequisites; do not provision Azure resources as part of the local upgrade exercise.

**Platform boundary:** the Modernize CLI supports all three operating systems, but the existing .NET Framework/MSBuild, IIS Express, and SQL Server LocalDB baseline requires **Windows**. Use Windows for its local build, execution, and validation. Linux/macOS can run the agent, but cannot complete that legacy application's local build/runtime checks. Java builds and hosting can use the appropriate JDK, Maven, and Tomcat on any supported OS; the Java setup guide uses cross-platform PowerShell 7 commands and selects the appropriate Tomcat launcher.

**Shell conventions:** shared `sh` command blocks below use commands that also work in Windows PowerShell. Run `powershell` blocks in PowerShell 7 on any OS, except the Windows-only installer. Run `bash` blocks in Bash/Zsh on Linux/macOS. Application-folder and smoke-check examples start at the repository root.

## Modernization workflow

### 1. Install and authenticate the agent

**Windows (PowerShell):** install using Winget:

```powershell
winget install GitHub.Copilot.modernization.agent
```

If Winget is unavailable, use the MSI installation option in the official quickstart.

**Linux/macOS (Bash/Zsh):** with [Homebrew](https://brew.sh/) installed and on `PATH`, use:

```bash
brew tap microsoft/modernize https://github.com/microsoft/modernize-cli
brew install modernize
```

If Homebrew is unavailable, use the Linux/macOS shell-script installation option in the official quickstart, following your organization's policy for reviewing and running downloaded scripts.

**All platforms:** open a **new terminal** so that `modernize` is available on `PATH`, then verify the tools and authenticate:

```sh
modernize --version
gh --version
gh auth login
```

Follow your organization's software-installation and GitHub authentication policies.

### 2. Establish a baseline

1. Review local changes and commit only intended source and documentation before starting. Do not include credentials, databases, restored packages, or build outputs.
2. Create a dedicated branch for one application, for example `modernize/dotnet-blog` or `modernize/java-blog`. Keep the other application unchanged.
3. Follow that application's setup guide to restore, build, and run the original version.
4. Run the [HTTP smoke checks](../scripts/Test-Blogs.ps1) and the restart/persistence check documented in the setup guide. Record actual results and unavailable tools; do not treat an unexecuted check as passing.
5. Record the baseline commit for later comparison, and back up local databases before any schema or authentication migration. Use a disposable copy for upgrade experiments.

### 3. Assess one application

From the repository root, choose **one** application folder and launch the agent. For the .NET application:

```sh
cd dotnet
modernize
```

For a separate Java run, start from the repository root instead:

```sh
cd java
modernize
```

In the interactive menu:

1. Select **Assess**.
2. Select **Current folder**, or **Manual input** to specify the intended application path. Verify that the assessment scope is only the selected application, not both implementations.
3. Select the relevant assessment domains offered for the application, such as runtime/framework upgrade and, optionally, **Cloud Readiness**.
4. Review **Analysis Coverage**, **Target Runtime**, and the other assessment settings before choosing **Continue**. Do not accept a target version without checking compatibility.
5. Select **Assess locally** for this exercise, rather than **Delegate to Cloud Agents**.
6. Accept the default output path or choose a custom path, then run the assessment and review its findings.

By default, assessment results are saved under `.github/modernize/assessment/` in the selected project directory. Findings identify outdated dependencies, deprecated APIs, compatibility issues, and migration opportunities; they are not evidence that an upgrade has succeeded.

### 4. Generate and approve a plan

After assessment:

1. Select **Create modernization plan**.
2. Select **Keep the plan local**.
3. Use the newly generated assessment report as input.
4. Enter a descriptive plan name, such as `dotnet-blog-upgrade` or `java-blog-upgrade`.
5. **Replace the default goal**, which includes migration to Azure, with an application-specific local upgrade goal below.
6. Answer the agent's clarification questions and review the generated `.github/modernize/{plan-name}/plan.md`. Edit the plan if needed.

The plan must specify target versions, ordered tasks, compatibility decisions, data/rollback strategy, and measurable success criteria. Do not proceed to execution until these decisions are approved.

#### .NET planning goal

The [dotnet](../dotnet) application contains [LegacyBlog.sln](../dotnet/LegacyBlog.sln). It is ASP.NET MVC 5 on .NET Framework 4.7.2, not ASP.NET Core. Enter the following as the modernization goal:

```text
Plan the modernization of only dotnet/LegacyBlog.sln. Compare an in-place
.NET Framework upgrade with migration to a currently supported .NET LTS
and ASP.NET Core MVC. Recommend targets and explain compatibility, data,
authentication, hosting, and testing risks. Preserve existing routes and
behavior. Keep execution local; do not add Azure services or deploy.
Stop after planning; do not change application code or install tools
until I approve the target and plan.
```

Review these repository-specific concerns before approving a plan:

- Moving from `System.Web`, `Global.asax`, MVC 5, and OWIN to ASP.NET Core requires application changes, not just a target-framework edit.
- Decide how to migrate `packages.config`, the classic project format, startup, configuration, Razor views, and hosting together.
- Explicitly plan EF6/database compatibility, schema changes, and ASP.NET Identity password-hash compatibility. Existing users must not be silently reset.
- Preserve LocalDB support for local development unless a different database is explicitly approved. Do not silently replace a persistent database with an in-memory provider.
- Retain all public and administrator routes, authorization, antiforgery protection, validation, moderation, pagination, and repeatable seed behavior.

#### Java planning goal

The [java](../java) application contains [pom.xml](../java/pom.xml). It is a Java 11 Maven WAR with Spring MVC 5, Spring Security 5, Hibernate 5, JSP, H2, and Tomcat 9. It is **not Spring Boot**. Enter the following as the modernization goal:

```text
Plan the modernization of only the Java 11 Maven WAR under java.
This application uses Spring MVC, not Spring Boot. Recommend a
compatible JDK, Spring Framework, Spring Security, Hibernate, JSP/Servlet
container, and H2 combination. Preserve WAR packaging, routes, form
login, CSRF protection, moderation, and persistent data. Keep execution
local; do not introduce Spring Boot, add Azure services, or deploy.
Stop after planning; do not change application code or install tools
until I approve the target and plan.
```

Review the generated plan before confirming execution:

- Separate a JDK-only upgrade from framework, persistence, and servlet-container changes.
- Check `javax.*` versus `jakarta.*` compatibility across Spring, Hibernate, validation, Servlet/JSP APIs, JSTL, and the target container. Tomcat 9 is not a Jakarta Servlet container.
- Keep Maven's compiler release, the actual build JDK, and the deployed runtime aligned with the approved target.
- Preserve H2 file compatibility or document a backed-up data migration. Do not point an incompatible new database engine at the only copy of existing data.
- Keep the existing BCrypt login hashes and verify authentication after restart.
- Review dependency/CVE remediation changes and rerun validation after them; a successful dependency scan does not establish behavioral parity.

### 5. Execute the approved plan

After reviewing and approving the plan, select **Execute modernization plan** and confirm execution. The agent runs the ordered tasks, applies code and dependency changes, validates builds, scans for and addresses CVEs, and creates commits.

Monitor progress and inspect the resulting changes. Because execution can change code and commit it, use the dedicated branch and disposable database copy established earlier. Do not execute a plan that still contains unapproved deployment or infrastructure tasks.

### 6. Validate and review the results

The agent's build and dependency checks do not replace the application's behavioral and persistence checks.

For both applications:

1. Restore dependencies and build with the approved toolchain. Use the existing setup commands for the legacy baseline; use and document the new build commands if the project format changes.
2. Start the upgraded application on loopback with the existing environment-variable configuration, or document any approved configuration changes.
3. Follow [Smoke checks](#smoke-checks) to start PowerShell 7 and set the original administrator credentials without saving real values in files. From the repository root in that shell, run the matching command:

   ```powershell
   ./scripts/Test-Blogs.ps1 -App DotNet -DotNetBaseUrl http://localhost:5080
   ./scripts/Test-Blogs.ps1 -App Java -JavaBaseUrl http://localhost:8080
   ```

   These paths work in PowerShell 7 on Windows, Linux, and macOS. Run only the command for the application being upgraded. The legacy .NET server must run on Windows; from another machine, supply its explicitly configured reachable test URL. Adjust the URL if the approved hosting configuration uses another port.

4. Verify public pages, category filters, pagination, private drafts, administrator login/CRUD, comment moderation, CSRF rejection, and invalid/not-found responses.
5. Create and publish a post, restart the application, and verify the post and original administrator login survive. Seed records must not multiply or overwrite edits.
6. For Java, run on the actual target JDK and servlet container, including JSP rendering and HTTP smoke checks. Cross-compilation alone is insufficient.
7. Review the complete branch changes against the recorded baseline commit, including commits created by the agent, not just uncommitted changes. Confirm that the other application remains unchanged.
8. Record tool versions, executed checks, results, remaining risks, and unverified behavior. Update the relevant setup guide and the [verification status](#verification-status) only with evidence.
9. If the results meet the approved success criteria, open a pull request for team review before merging.

If a planned change intentionally affects routes, HTML forms, or configuration, review the compatibility decision and update the associated tests and documentation. Do not weaken assertions merely to make an upgrade appear successful.

Azure migration, containerization, infrastructure generation, and deployment are optional follow-up work. Obtain an explicit hosting choice and approval before creating resources or changing the local-only operating model.

## Optional automated assessment

You can run the **assessment stage only** in GitHub Actions or Azure DevOps, without automatically generating/executing modernization plans, opening issues or PRs, changing application code, or deploying resources. Both integrations call the same [assessment runner](../scripts/Invoke-ModernizationAssessment.ps1):

```sh
modernize assess --source APP_PATH --output-path REPORT_DIRECTORY --format markdown --delegate local --no-tty
```

Replace `APP_PATH` and `REPORT_DIRECTORY` with actual paths, quoting paths that contain spaces. The templates supply absolute paths automatically. `local` means the CI agent runs the assessment rather than delegating to a Copilot cloud agent; it does **not** mean offline analysis. AI assessment can send code context to GitHub Copilot and consume Copilot requests. Obtain organization approval for code access, credentials, and usage before enabling it.

### Credentials and prerequisites

- Create an organization-approved token owned by an account with Copilot access. For a fine-grained GitHub PAT, grant the **Copilot Requests** account permission and read access to the necessary repositories; authorize organization access as required. See [Copilot CLI authentication](https://docs.github.com/en/copilot/how-tos/copilot-cli/set-up-copilot-cli/authenticate-copilot-cli) for currently supported token types.
- Store it as the secret **`MODERNIZE_GH_TOKEN`** in the selected CI platform. The templates expose it only to the assessment step as `GH_TOKEN` and `COPILOT_GITHUB_TOKEN`. Do not put a token value in YAML or commit it.
- The automatic GitHub Actions `GITHUB_TOKEN` and Azure DevOps `System.AccessToken` are **not** substitutes for a Copilot-enabled GitHub credential. Azure Repos checkouts still need GitHub authentication for Copilot.
- Runners need PowerShell 7, GitHub CLI **2.45.0+**, and the application's assessment/build prerequisites. The templates select a .NET SDK or JDK as appropriate. The shared runner provisions the pinned **Modernize 1.0.76** release when that version is unavailable and verifies its archive against the release's SHA-256 checksums.
- This repository's .NET Framework app uses a **Windows** runner; Java uses **Linux**. The shared runner supports Windows, Linux, and macOS, but this does not remove the legacy .NET Windows requirement.

### GitHub Actions

The [Assess applications workflow](../.github/workflows/modernization-assessment.yml) calls the [reusable assessment workflow](../.github/workflows/modernization-assessment-template.yml) independently for each application.

1. Add the `MODERNIZE_GH_TOKEN` repository or organization secret.
2. To assess manually, open **Actions -> Assess applications -> Run workflow** and select `dotnet`, `java`, or `all`.
3. To opt into automatic assessment, add the repository variable **`MODERNIZATION_ASSESSMENT_ENABLED`** with the exact value **`true`**.
4. With that variable enabled, changes to the configured application/automation paths on `main` trigger assessment, and both apps are assessed weekly on **Monday at 06:00 UTC**. Scheduled runs use the default branch; keep it aligned with `main` or adjust the workflow.
5. Download the `modernization-assessment-dotnet` and/or `modernization-assessment-java` artifacts from the workflow run.

Without the variable, push/scheduled assessment jobs are skipped. Manual dispatch is an explicit opt-in and still requires the secret. No pull-request event is configured, and the reusable job also rejects pull-request events: do not expose the assessment credential to untrusted fork or PR code.

To call the reusable workflow from another workflow in the same repository:

```yaml
jobs:
  assess-java:
    uses: ./.github/workflows/modernization-assessment-template.yml
    with:
      application-path: java
      language: java
      runner: ubuntu-latest
      artifact-name: modernization-assessment-java
    secrets:
      copilot-token: ${{ secrets.MODERNIZE_GH_TOKEN }}
```

The reusable workflow also accepts `enabled`, `cli-version`, and `assessment-config`. `language` must be `dotnet` or `java`; choose a compatible runner for the app.

### Azure DevOps Pipelines

The [Azure pipeline](../.azdo/modernization-assessment.yml) uses a [reusable job template](../.azdo/templates/modernization-assessment.yml).

1. Create an Azure DevOps pipeline using the existing YAML file `.azdo/modernization-assessment.yml`. For an existing pipeline, update its YAML path to this location in Azure DevOps.
2. Add **`MODERNIZE_GH_TOKEN`** as a **secret** pipeline variable, or supply it from an authorized variable group.
3. For a manual assessment, select **Run pipeline**, enable **Enable this assessment run**, and choose `dotnet`, `java`, or `all`.
4. To opt into automatic runs, add the pipeline variable **`MODERNIZATION_ASSESSMENT_ENABLED=true`**. Matching `main` commits and the Monday **06:00 UTC** schedule then assess both apps. Without this variable or the manual checkbox, the jobs are skipped.
5. Download each job's `modernization-assessment-*` pipeline artifact. Configure retention through your Azure DevOps pipeline/project policies.

The template can also be included in an existing pipeline:

```yaml
jobs:
  - template: .azdo/templates/modernization-assessment.yml
    parameters:
      jobName: assessJava
      applicationPath: java
      language: java
      vmImage: ubuntu-latest
      enabled: true
```

Template paths are relative to the including YAML file; the example assumes a pipeline file at the repository root. The template also accepts `cliVersion` and `assessmentConfig`. The example enables this job on non-PR runs of the calling pipeline, so use only trusted branch triggers and authorize its secret accordingly. The job rejects the `PullRequest` build reason even if an Azure Repos branch policy queues the pipeline. Azure DevOps Server users should replace the Azure DevOps Services `PublishPipelineArtifact@1` task with their supported artifact-publishing equivalent.

### Reusing and customizing the templates

For another repository, copy the shared assessment script and the chosen CI templates, preserving their relative paths, then replace the application paths and runner/toolchain settings. The templates check out the **calling repository**; referencing a remote YAML template alone does not copy the script into that checkout.

For explicit assessment domains, target versions, or analysis coverage, supply a reviewed YAML configuration supported by the pinned Modernize version through `assessment-config` (GitHub) or `assessmentConfig` (Azure). Keep authored configuration outside ignored generated directories, for example under `ci/assessment/`. If no configuration is supplied, the CLI uses its default/auto-discovered settings; do not assume identical findings across CLI upgrades.

The shared runner rejects missing credentials, paths outside the checkout, incompatible tool versions, stale report directories, nonzero CLI exit codes, and missing/empty Markdown reports. It also rejects symbolic links and junctions in input paths, their ancestors below the checkout root, and the application tree, even when a link points inside the repository. This deliberately strict policy prevents linked paths from escaping the selected assessment scope; use an ordinary checkout with real directories/files.

Reports go to the agent's temporary directory and are uploaded as run-scoped CI artifacts, subject to repository/pipeline permissions, not committed or published to GitHub Pages. Partial output, when present, is preserved when an assessment fails. GitHub artifact retention is seven days; review access and retention because reports can contain code snippets, configuration findings, and machine-specific details. Artifacts in a public repository should not be treated as confidential storage.

Findings do not automatically fail a build by severity: execution/report-generation failures do. This integration is not a migration approval gate and does not replace the [behavioral and persistence checks](#smoke-checks).

The wrapper can be checked without live credentials, network calls, or a paid assessment:

```sh
pwsh -File scripts/Test-ModernizationAssessment.ps1
```

## Required regression checks

The credential-free [Validate repository workflow](../.github/workflows/validate.yml) runs on every pull request, on `main` pushes, and on manual dispatch. It does not need the Copilot token or the optional assessment opt-in variable.

- Offline assessment-wrapper checks run on Windows, Linux, and macOS, including linked-input rejection.
- The .NET application builds and runs on Windows with full Framework MSBuild, IIS Express, and an isolated LocalDB instance.
- The Java application builds on Linux with JDK 11 and runs in an isolated Java 11/Tomcat 9 container.
- Both applications undergo form/HTTP tests, publication-timestamp checks, comment cascade-deletion checks, and a full restart against the same database without bootstrap credentials.
- Both built artifacts are checked to ensure the intentionally vulnerable training examples are not shipped.
- Documentation builds on Linux without publishing or requesting Pages deployment permissions.

Configure a GitHub branch ruleset to require these checks before merging; a workflow file alone does not enforce branch protection. No paid assessment is run by this workflow. On-premises Azure DevOps users can call the same regression script from a suitably provisioned trusted agent independently of the optional assessment pipeline.

To run the baseline checks locally from the repository root:

```sh
pwsh -File scripts/Test-ApplicationBaseline.ps1 -App Java
```

Java requires Maven, a JDK, and a running Docker engine with Linux containers. On Windows with the tools listed in the Framework setup guide:

```powershell
pwsh -File scripts/Test-ApplicationBaseline.ps1 -App DotNet
```

Append `-Restore` to the .NET command when packages need restoring. The runner generates disposable credentials, uses an unused loopback port, and cleans up its own processes, container/volume or LocalDB instance, and temporary database files. It does not use the developer's blog database or existing administrator credentials.

## Intentional security exercises (source-only)

This repository includes **deliberately vulnerable code** for security-review and modernization-assessment training:

| Exercise | C# source | Java source | Intended remediation |
| --- | --- | --- | --- |
| SQL injection (CWE-89) | [ReadTitle](../dotnet/security-examples/VulnerableExamples.cs) | [readTitle](../java/security-examples/VulnerableExamples.java) | Parameterize SQL queries. |
| Unencoded HTML output (CWE-79) | [WriteHeading](../dotnet/security-examples/VulnerableExamples.cs) | [writeHeading](../java/security-examples/VulnerableExamples.java) | Encode untrusted text for the HTML context. |
| Unsalted MD5 password hashing (CWE-759/CWE-328) | [HashSyntheticPassword](../dotnet/security-examples/VulnerableExamples.cs) | [hashSyntheticPassword](../java/security-examples/VulnerableExamples.java) | Use the authentication framework's salted adaptive password hashing. |

**Isolation is intentional:** the C# file is outside the web project and the Java file is outside Maven's source roots. Neither has a controller/servlet registration, route, or startup call. Normal builds exclude them; regression checks verify their absence from the application assembly/WAR. Do not move them into application source roots or wire them to the running blogs.

Use synthetic inputs only. There are no real credentials, real customer data, runnable lab endpoints, or exploit scripts. The normal applications retain their parameterized/ORM queries, encoded output, CSRF protection, and framework password hashing.

Source scanners may flag these fixtures as intentional findings; scanners restricted to compiled project files may not inspect them at all. Select the files explicitly in a source-review exercise rather than weakening the normal builds to force detection. Treat fixture findings separately from unintended issues in the applications; do not suppress the same rules repository-wide.

## Generated files and Git policy

The root [.gitignore](../.gitignore) excludes generated modernization output at both repository and application-folder scope:

| Location | Handling |
| --- | --- |
| `.github/modernize/assessment/` | Ignore the Modernize CLI's generated assessment results. |
| `.github/modernize/{plan-name}/` | Ignore generated plans and session state; covered by `.github/modernize/*`, which also covers the existing Java upgrade workspace. |
| `.github/modernize/playbook/` | Exception: keep authored modernization policies trackable. |
| `.github/appmod/` | Ignore output from other modernization workflows, including IDE-generated migration plans and progress. |
| `.github/upgrades/` | Ignore output from the separate upgrade workflow, including assessments, plans, and task progress. |
| Maintained documentation | Keep the repository README, this guide, and the application setup guides trackable. |

These patterns also match the same locations under [dotnet](../dotnet) or [java](../java). Generated paths vary by workflow and extension version; review any new untracked output before committing. Avoid blanket rules for all Markdown files or the entire `.github` folder.

This repository deliberately keeps generated state local. Ignored files are not backed up by Git or available in another clone; retain them locally if you want to resume a session. Summarize approved decisions and verified results in maintained documentation. The agent's source-code commits do not automatically make ignored reports part of the branch history.

Ignore rules do not untrack files already committed and do not delete local reports. If a reviewed report must be shared, explicitly force-add only that file after checking for credentials, connection strings, customer data, and machine-specific details. Generated source-code changes outside the ignored folders still need normal review and commits.

## Sample applications

The two blog applications use deliberately older enterprise stacks:

| | .NET | Java |
|---|---|---|
| Target | .NET Framework 4.7.2 | Java 11 |
| Web | ASP.NET MVC 5 / Razor / System.Web | Spring MVC 5 / JSP / Servlet 4 |
| Data | Entity Framework 6 / SQL Server LocalDB | Hibernate 5 / file-based H2 |
| Host | IIS Express on Windows | Tomcat 9 |
| Build | Classic MSBuild solution / packages.config | Maven WAR |

See [Framework setup](../dotnet/README.md) and [Java setup](../java/README.md).

These are modernization practice applications, **not production templates**. Their legacy dependency lines have maintenance/security limitations. No cloud services, containers, SPA, rich-text editor, uploads, public registration, or email service are included.

### Shared behavior

- Public published posts, category filters, ten-post pagination, and approved comments.
- Administrator login, post CRUD, draft/publish transitions, category management, and comment moderation.
- Plain-text content is HTML-encoded. Slugs are explicit, unique, lowercase words separated by hyphens.
- Guest comments require approval. Draft posts cannot be read or commented on publicly.
- A category assigned to a post cannot be deleted. Deleting a post deletes its comments.
- UTC publication dates are preserved during published-post edits; unpublishing clears the date.
- Repeatable first-run seed: two categories, twelve published posts, two drafts, an approved comment and a pending comment. Restart never overwrites edits or recreates deleted seed records.
- First-run administrator credentials come from `BLOG_ADMIN_USER` and `BLOG_ADMIN_PASSWORD` (minimum 12 characters). Passwords are hashed by the authentication framework. Existing passwords are not reset on restart.

### Public routes

| Method | Path |
|---|---|
| GET | `/`, `/?page=2` |
| GET | `/posts/{slug}` |
| GET | `/categories/{slug}` |
| POST | `/posts/{slug}/comments` |
| GET / POST | `/account/login` |
| POST | `/account/logout` |
| GET | `/admin`, `/admin/posts`, `/admin/categories`, `/admin/comments` |

Administrator mutation routes are linked from the corresponding HTML forms. Mutations require authentication where applicable and a CSRF token. Invalid forms show errors without changing data. Invalid page numbers return 400; missing resources and beyond-last pages return 404. Page one of an empty category returns an empty state.

### Smoke checks

Start the application to test. For the existing .NET application, its server must run on Windows; the Java server can run on any supported OS.

From the repository root, start PowerShell 7 using the same command in Windows PowerShell, Bash, or Zsh:

```sh
pwsh
```

Then run the following **inside PowerShell 7**, on Windows, Linux, or macOS. Enter the credentials used when the database was first initialized:

```powershell
$env:BLOG_ADMIN_USER = Read-Host 'Existing administrator username'
$secret = Read-Host 'Existing administrator password' -AsSecureString
$env:BLOG_ADMIN_PASSWORD = [PSCredential]::new('local', $secret).GetNetworkCredential().Password
```

Choose **one** command for the running application, or `All` only when both servers are available:

```powershell
./scripts/Test-Blogs.ps1 -App DotNet -DotNetBaseUrl http://localhost:5080
./scripts/Test-Blogs.ps1 -App Java -JavaBaseUrl http://localhost:8080
./scripts/Test-Blogs.ps1 -App All
```

These defaults assume the server is on the same machine as the checks. For a remote test server, replace the base URL with an explicitly configured reachable endpoint. Keep the loopback-only baseline unchanged unless remote testing is deliberately configured and secured; do not expose these legacy samples publicly.

The checks use real HTML forms/cookies/CSRF tokens, create uniquely named records, and remove only those records. A failed assertion exits nonzero. Restart/persistence checks are described in the per-app guides.

Bind the servers to loopback. Local HTTP cookies are intentionally usable without TLS; this is not a production hosting configuration. Do not commit real credentials, database files, or logs.

### Verification status

- **Framework:** 4.7.2-targeted Release build on the installed Framework 4.8.1 runtime and **66 passing HTTP/form/restart assertions** using disposable IIS Express/LocalDB resources.
- **Java:** Maven WAR build using the installed JDK 21 with `--release 11`, followed by **67 passing HTTP/form/restart assertions** on actual **Java 11/Tomcat 9** in Docker, including JSP rendering.
- **Automation:** **31 offline assessment-wrapper checks** pass on Windows and in a native Linux filesystem; CI also runs them on macOS. Built artifacts exclude the vulnerable training namespace/classes. The linked-path tests require a writable checkout.
- **Not verified locally:** hosted GitHub Actions/Azure DevOps runs, paid live assessments, and native macOS execution. These checks do not certify the intentionally legacy dependency lines as production-safe.

## Documentation website (GitHub Pages)

The [Publish documentation workflow](../.github/workflows/pages.yml) builds and deploys a Jekyll site with three guides and a presentation:

- This guide as the site's main modernization page.
- The [Framework setup](../dotnet/README.md).
- The [Java setup](../java/README.md).
- The self-contained [HTML presentation](presentation.html), published at the site's `presentation/` path.

The [documentation build script](../scripts/Build-Docs.ps1) generates the site source from those files, so documentation changes only need to be made once. Links between the guides stay within the site, while source-file links point to the corresponding GitHub commit. Repository-specific Pages URL prefixes are handled automatically.

### Enable publishing

1. Push the repository, including the workflow, to GitHub on the `main` branch.
2. Open **Settings -> Pages -> Build and deployment**, and select **GitHub Actions** as the source.
3. Open **Actions -> Publish documentation -> Run workflow** and select `main`. If the first automatic run occurred before Pages was configured, rerun it.
4. Open the site URL shown in the deployment summary or the `github-pages` environment.

Subsequent documentation changes pushed to `main` trigger publication automatically. Review the repository's Pages availability, site visibility, and environment protection settings before publishing; a private repository does not necessarily imply a private Pages site.

The site uses a custom [documentation layout](_layouts/docs.html), [stylesheet](assets/site.css), and [progressive-enhancement script](assets/site.js). It includes a responsive navigation bar, light/dark themes, an active table of contents, keyboard focus/skip links, and copy controls for code examples. The presentation shares the same visual language while keeping all its styles and scripts inside a single offline-capable file.

The site title and description are configured in [_config.yml](_config.yml). The build stages only the three maintained guides, the presentation, the layout/assets, and site configuration; application source, build outputs, databases, and modernization session state are not copied. The generated site source is recreated on every build to avoid retaining stale assets. Generated source and HTML are ignored by Git; authored layouts, assets, and the presentation remain tracked.

The workflow builds on a GitHub-hosted Linux runner using PowerShell 7 and the official Jekyll/Pages actions. No local Ruby installation is required to publish the site.

## References

- [GitHub Copilot modernization agent overview](https://learn.microsoft.com/en-us/azure/developer/github-copilot-app-modernization/modernization-agent/overview)
- [Install and use the modernization agent](https://learn.microsoft.com/en-us/azure/developer/github-copilot-app-modernization/modernization-agent/quickstart)
- [Modernize CLI commands](https://learn.microsoft.com/en-us/azure/developer/github-copilot-app-modernization/modernization-agent/cli-commands)
- [Batch and non-interactive assessment](https://learn.microsoft.com/en-us/azure/developer/github-copilot-app-modernization/modernization-agent/batch-assess)
- [GitHub Pages custom workflows](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages)
