# Agentic Modernization with GitHub Copilot

A hands-on workspace for the **[GitHub Copilot modernization agent](https://learn.microsoft.com/en-us/azure/developer/github-copilot-app-modernization/modernization-agent/overview)**: assess legacy applications, approve a plan, execute changes, and prove behavior still works.

> **Practice repository, not a completed migration or production template.** Azure migration and deployment are optional, separately approved steps.

## Start here

| Your goal | Documentation |
| --- | --- |
| Understand the agent and this repo | [14-slide HTML presentation](docs/presentation.html) |
| Follow the full modernization workflow | [Modernization guide](docs/MODERNIZATION.md) |
| Build and run an application | [.NET setup](dotnet/README.md) or [Java setup](java/README.md) |
| Automate assessments | [GitHub Actions / Azure DevOps guide](docs/MODERNIZATION.md#optional-automated-assessment) |

The presentation works offline in a browser and supports keyboard navigation, themes, fullscreen, and PDF printing.

## Sample applications

Two independent blogs with equivalent publishing, authentication, and comment-moderation behavior:

| | .NET | Java |
| --- | --- | --- |
| Stack | Framework 4.7.2, ASP.NET MVC 5, EF 6 | Java 11, Spring MVC 5, Hibernate 5 |
| Data | SQL Server LocalDB | File-based H2 |
| Build / host | MSBuild / IIS Express (**Windows**) | Maven WAR / Tomcat 9 |

## Modernization workflow

1. Build and test the original application; create a branch and back up its data.
2. Run **Assess** for one application using the Modernize CLI.
3. Review target versions, compatibility, data handling, and validation criteria.
4. Approve the plan before **Execute**, then rerun regression checks.

After installing the [prerequisites](docs/MODERNIZATION.md#prerequisites), start the Java assessment from the repository root:

```sh
gh auth login
cd java
modernize
```

Use `cd dotnet` instead for .NET. Its legacy build/runtime requires Windows. See the [full workflow and example goals](docs/MODERNIZATION.md#modernization-workflow).

## Optional automated assessment

Use the [GitHub workflow](.github/workflows/modernization-assessment.yml) or [Azure pipeline](.azdo/modernization-assessment.yml), with reusable templates alongside them.

Set the **`MODERNIZE_GH_TOKEN`** secret for a manual run. Automatic `main` push and weekly assessments require **`MODERNIZATION_ASSESSMENT_ENABLED=true`**. These jobs assess only: no upgrades, commits, or deployments.

See [credential requirements, schedules, artifacts, and template reuse](docs/MODERNIZATION.md#optional-automated-assessment).

## Required regression checks

The [PR validation workflow](.github/workflows/validate.yml) runs without Copilot credentials. It checks builds, HTTP/forms, publication dates, comment deletion, restart persistence, and documentation.

Run locally from the repository root:

```sh
pwsh -File scripts/Test-ApplicationBaseline.ps1 -App Java
```

Java requires Maven, a JDK, and Docker. On Windows with MSBuild, IIS Express, and LocalDB, use `-App DotNet`; append `-Restore` if packages need restoring. [Full validation details](docs/MODERNIZATION.md#required-regression-checks).

## Intentional security exercises (source-only)

The [C#](dotnet/security-examples/VulnerableExamples.cs) and [Java](java/security-examples/VulnerableExamples.java) training examples deliberately contain SQL concatenation, unencoded HTML, and weak password hashing.

They are **excluded from normal builds and routes**. Use synthetic data only; do not wire them into the applications. Read the [training boundaries](docs/MODERNIZATION.md#intentional-security-exercises-source-only).

## Verification status

- **.NET:** 66 passing HTTP/form/restart assertions.
- **Java:** 67 passing assertions on Java 11 / Tomcat 9.
- **Assessment wrapper:** 31 offline checks on Windows and Linux.
- Hosted CI, paid assessments, and native macOS execution remain unverified. See [verification details](docs/MODERNIZATION.md#verification-status).

## Documentation website (GitHub Pages)

The full modernization guide, app setup guides, and presentation are published by the [Pages workflow](.github/workflows/pages.yml). Select **Settings -> Pages -> GitHub Actions**, then run **Publish documentation** on `main`.

See [publishing and site configuration](docs/MODERNIZATION.md#documentation-website-github-pages). Generated reports and session state remain local and ignored by Git.
