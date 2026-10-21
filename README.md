# Modernize legacy applications with the Modernize CLI

A hands-on workspace for modernizing existing .NET and Java applications with the **[GitHub Copilot modernization agent](https://learn.microsoft.com/en-us/azure/developer/github-copilot-app-modernization/modernization-agent/overview)** and its **Modernize CLI**.

The goal is **Assess -> Plan -> Approve -> Execute -> Validate**, not simply to launch the sample blogs. The legacy applications are the starting point; the result is a reviewed modernization branch with evidence that existing behavior and data still work.

> **Practice repository, not a completed migration or production template.** Azure migration and deployment are optional, separately approved steps. Running the original app is only a supporting activity for baseline and regression validation.

## Optional: open in Codespaces

> **Codespaces is optional, not a requirement.** Use it as an alternative to your local environment. The same Modernize CLI workflow works locally with the documented prerequisites.

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/congiuluc/agentic-modernization-demo)

The [development container](.devcontainer/devcontainer.json) provides **Modernize CLI, .NET 10 SDK, and JDK 27 (default)**, plus .NET 8, JDK 11 for baseline compatibility, Maven, PowerShell 7, GitHub CLI, and Docker. The SDK's `10.0` setting follows .NET 10 servicing releases, not future major versions; no preview SDK is requested. Rebuild the container to refresh the toolchain.

Wait for setup to finish, then follow the CLI workflow below in the default PowerShell terminal. Setup does **not** authenticate, assess, execute plans, launch the apps, or expose application ports automatically.

**Optional local demo:** the .NET application intentionally targets **.NET Framework 4.7.2** with MVC 5, EF6, IIS Express, and LocalDB, and requires **Windows**. Codespaces can be used for assessment/planning, but cannot host this legacy .NET baseline. Java uses local H2. Both seed demo content and require administrator credentials on first startup. Preinstalled .NET 10 and JDK 27 are toolchain options, not approved application upgrades. See [Codespaces authentication, validation, and limitations](docs/MODERNIZATION.md#using-github-codespaces) before starting.

## Modernize an application

Install the [Modernize CLI and prerequisites](docs/MODERNIZATION.md#prerequisites) locally, or use the optional Codespaces environment above, then follow the commands below. No IDE extension or menu navigation is required.

Work on a dedicated branch for **one application**. Before making changes, record its [baseline](docs/MODERNIZATION.md#2-establish-a-baseline) and back up its data. Keep the other application unchanged.

### 1. Assess

From the repository root in **PowerShell 7**, authenticate, choose one application, and assess it:

```powershell
modernize --version
gh auth status
Set-Location java
modernize assess --format markdown --delegate local
```

For .NET, use `Set-Location dotnet` instead. The Framework 4.7.2 build/runtime baseline requires **Windows**. Keep the same terminal and application folder through planning and execution.

If GitHub CLI is not authenticated locally, run `gh auth login`. In Codespaces, repository access is usually already configured, but the automatic token is not a Copilot credential: follow the [Codespaces authentication steps](docs/MODERNIZATION.md#using-github-codespaces).

Review the findings under `.github/modernize/assessment/`. To customize coverage or targets, use the [optional YAML configuration](docs/MODERNIZATION.md#optional-assessment-configuration) and replace the assessment command with:

```powershell
modernize assess --assess-config .\modernize.yaml --format markdown --delegate local
```

The file is optional and must exist in the selected application folder for this example. `--delegate local` runs the agent on your machine; AI analysis still uses Copilot services and can consume requests.

### Optional Java YAML for Modernize CLI

The repository includes [java/modernize.yaml](java/modernize.yaml), a minimal, optional assessment configuration:

```yaml
analysisCoverage: full
```

`analysisCoverage: full` requests issue analysis plus codebase insights, including architecture, API contracts, configuration, workflows, dependencies, and data model. Java-specific assessment domains and target-runtime settings remain at the CLI's defaults.

From the `java` folder, use it **instead of** the default assessment command:

```powershell
modernize assess --assess-config .\modernize.yaml --format markdown --delegate local
```

This file does not select the installed JDK, change the Java 11 application target, or authorize an upgrade. Codespaces defaults to JDK 27; assess its suitability explicitly in the planning goal and approve the plan before execution. See [Java YAML details and repository-root usage](docs/MODERNIZATION.md#optional-java-yaml-for-modernize-cli).

### 2. Create and approve the plan

In the same terminal and application folder, select the assessment report you reviewed:

```powershell
$assessmentReport = Read-Host "Paste the full path of the assessment report file"
```

Choose **one** sample prompt below for your application. These prompts request a plan, not immediate code changes.

**.NET prompt** - from the `dotnet` folder:

```powershell
$goal = @'
Plan modernization of only LegacyBlog.sln in the current folder.
Compare an in-place .NET Framework upgrade with migration to a supported
.NET LTS and ASP.NET Core MVC from the Framework 4.7.2 baseline.
Explain System.Web, OWIN, Razor, EF6,
LocalDB, and ASP.NET Identity password-hash compatibility.
Preserve routes, authorization, antiforgery protection, persistent data,
original logins, and moderation. Include ordered tasks, rollback steps,
and build, HTTP, and restart-persistence acceptance checks.
Do not add Azure services or deploy. Stop after planning for my approval;
do not change application code or install tools before approval.
'@
```

**Java prompt** - from the `java` folder:

```powershell
$goal = @'
Plan modernization of only the Java 11 Maven WAR in the current folder.
This is Spring MVC, not Spring Boot. Recommend compatible supported JDK,
Spring, Spring Security, Hibernate, H2, and JSP/Servlet container versions.
Address javax versus jakarta and preserve WAR packaging, routes, form
login, BCrypt hashes, CSRF protection, moderation, and persistent H2 data.
Include ordered tasks, data backup and rollback steps, and build, HTTP,
JSP-rendering, and restart-persistence acceptance checks.
Do not introduce Spring Boot, add Azure services, or deploy.
Stop after planning for my approval; do not change application code
or install tools before approval.
'@
```

Generate the plan using the selected prompt and assessment report:

```powershell
modernize plan create $goal --assess-file-path $assessmentReport --plan-name blog-upgrade --delegate local
```

**Stop and review** `.github/modernize/blog-upgrade/plan.md` and `tasks.json`. Approve target versions, compatibility choices, ordered tasks, data/rollback strategy, and success criteria before continuing.

See the full [.NET](docs/MODERNIZATION.md#net-planning-goal) and [Java](docs/MODERNIZATION.md#java-planning-goal) planning checklists for additional review points.

### 3. Execute the approved plan

Only after approval, from the same application folder:

```powershell
modernize plan execute --plan-name blog-upgrade --delegate local
```

Execution changes code and dependencies and can create commits. Review the complete branch history, not just uncommitted files. Do not use `modernize upgrade` as a shortcut here: it combines planning and execution.

### 4. Validate the modernization

Follow the [post-modernization validation steps](docs/MODERNIZATION.md#6-validate-and-review-the-results) using the approved target toolchain. Verify builds, routes, forms, permissions, original logins, and data persistence after restart. Open a pull request only when the agreed criteria pass.

The [existing regression workflow](.github/workflows/validate.yml) protects the legacy baseline; if modernization changes the toolchain or host, update the relevant validation commands and CI as part of the approved plan. Do not mistake a successful assessment or an unchanged legacy CI run for proof that the upgraded application works.

## Applications to modernize

Two independent blogs with equivalent publishing, authentication, and comment-moderation behavior:

| Application | Starting stack | Decisions to address in the plan |
| --- | --- | --- |
| .NET | Framework 4.7.2, ASP.NET MVC 5, EF 6, LocalDB | Framework upgrade versus ASP.NET Core migration; System.Web, Identity, database, and hosting compatibility. |
| Java | Java 11, Spring MVC 5, Hibernate 5, H2, Maven WAR | Compatible JDK/framework/container versions; javax versus jakarta, WAR packaging, and persistent data. This is not Spring Boot. |

## Supporting documentation and automation

| Purpose | Reference |
| --- | --- |
| Follow the full modernization exercise | [Modernization guide](docs/MODERNIZATION.md) |
| Modernize one application with the CLI | [.NET modernization](docs/DOTNET-MODERNIZATION.md) or [Java modernization](docs/JAVA-MODERNIZATION.md) |
| Present the CLI workflow | [14-slide HTML presentation](docs/presentation.html), viewable offline with PDF printing |
| Understand YAML fields and CLI options | [Assessment configuration](docs/MODERNIZATION.md#optional-assessment-configuration) |
| Optionally prepare a local validation environment | [.NET setup](dotnet/README.md) or [Java setup](java/README.md), linked as step 1 of each modernization workflow |
| Use local databases with demo data | [.NET LocalDB configuration](dotnet/README.md#storage-and-configuration) or [Java H2 defaults](java/README.md#configuration-and-data-lifecycle); both seed categories, posts, drafts, and comments on first startup |
| Check behavior and restart persistence | [Required regression checks](docs/MODERNIZATION.md#required-regression-checks) |
| Review what has actually been tested | [Verification status](docs/MODERNIZATION.md#verification-status); paid assessments and hosted CI remain unverified |
| Practice isolated source-only security review | [Training boundaries](docs/MODERNIZATION.md#intentional-security-exercises-source-only); do not wire the fixtures into the apps |

**Optional assessment automation:** the [GitHub workflow](.github/workflows/modernization-assessment.yml) and [Azure pipeline](.azdo/modernization-assessment.yml) assess only; they do not execute plans or upgrade applications. They require `MODERNIZE_GH_TOKEN`; automatic runs also require `MODERNIZATION_ASSESSMENT_ENABLED=true`. See [credential and pipeline setup](docs/MODERNIZATION.md#optional-automated-assessment).

## Documentation website (GitHub Pages)

[Open the documentation](https://congiuluc.github.io/agentic-modernization-demo/).
