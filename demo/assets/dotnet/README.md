# .NET demo examples: video, assessment, and plan

Recorded outputs from a LegacyBlog modernization run on **22 October 2026**, provided so the demo can show concrete results without waiting for a live assessment or plan generation.

> **Historical examples, not the current application's verification status.** The application in this repository remains the .NET Framework 4.7.2 baseline. These AI-generated artifacts can contain inaccuracies, refer to another source snapshot, or describe files that are not included here. Review them as examples, not as approval to execute a migration or proof that this checkout is modernized.

## Demo video

Watch the recorded demo, then explore the assessment and plan below. The published documentation includes a player; playback starts only when requested.

<video controls preload="none" playsinline aria-label="Recorded .NET modernization demo" src="video/GHC-Modernize-Demo.mp4">
Your browser does not support embedded video. Use the MP4 link below.
</video>

[Open or download the demo video (MP4, approximately 61 MiB)](video/GHC-Modernize-Demo.mp4).

## Demo walkthrough

1. Open the [assessment dashboard](assessment/reports-20261022070217/index.html), then select **LegacyBlog** to inspect the [application report](assessment/reports-20261022070217/repos/dotnet/report.html).
2. Show the findings and [codebase insights](assessment/reports-20261022070217/repos/dotnet/facts/assessment-overview.md): architecture, dependencies, API contracts, data, configuration, and workflows.
3. Open the [resulting modernization plan](dotnet-blog-upgrade/plan.md). Compare the Framework servicing option with migration to .NET 10 / ASP.NET Core MVC.
4. Review the ordered tasks, compatibility decisions, database backup/rollback, and measurable acceptance checks. Explain where human approval belongs before execution.
5. Optionally show the [recorded task metadata](dotnet-blog-upgrade/.metadata/tasks.json) and summaries below, keeping reported outcomes separate from independently verified results.

The HTML reports can also be opened directly from a local checkout. Their Mermaid diagrams load from a public CDN and need internet access; the recorded findings and plan remain available without running Modernize CLI. On GitHub, HTML links open source previews: open the downloaded files in a browser or use the published documentation site for rendered reports.

## Assessment example

Source: [Markdown report](assessment/reports-20261022070217/repos/dotnet/report.md), with the corresponding [JSON report](assessment/reports-20261022070217/repos/dotnet/report.json).

| Recorded metric | Value |
| --- | --- |
| Application | LegacyBlog, C#, .NET Framework 4.7.2, MSBuild |
| Total issue types | 9 |
| Mandatory blockers | 2 |
| Potential issues | 3 |

The mandatory findings concern local/network logging and Windows authentication. Potential findings concern local/network I/O, OS environment access, and a SQL connection. Other findings include sensitive-data patterns, System.Data.SqlClient, connection-string configuration, and static content. These are assessment categories, not a new security audit or an instruction to provision Azure.

Use the [architecture diagram](assessment/reports-20261022070217/repos/dotnet/facts/architecture-diagram.md) and [dependency map](assessment/reports-20261022070217/repos/dotnet/facts/dependency-map.md) to connect those findings to the modernization decisions.

## Plan example

The [full plan](dotnet-blog-upgrade/plan.md) keeps execution **local-only**, with no Azure services, provisioning, or deployment.

| Stage | What to discuss in the demo |
| --- | --- |
| Baseline | Freeze routes, authentication, moderation, persistent data, and original logins before changing code. |
| Upgrade comparison | Compare in-place Framework servicing with .NET 10; the comparison target is not prior approval. |
| HTTP and views | Replace System.Web, OWIN, MVC 5/Razor, and legacy project format with ASP.NET Core equivalents. |
| Data | Preserve EF/LocalDB schema and data semantics; back up and define rollback before migration. |
| Identity | Verify existing Identity v2 password hashes and preserve authorization, cookies, and antiforgery behavior. |
| Acceptance | Require build, HTTP, original-login, and restart-persistence evidence. |
| Dependencies | Review CVE findings and compatibility before accepting dependency changes. |

### Optional recorded execution outputs

- [Baseline summary](dotnet-blog-upgrade/001-setupBaseline-legacyblog-local/baseline-summary.md).
- [Upgrade readiness summary](dotnet-blog-upgrade/002-upgrade-net472-to-net10-assessment/modernization-summary.md).
- [HTTP pipeline and routing summary](dotnet-blog-upgrade/003-transform-systemweb-owin-razor-routing/modernization-summary.md).
- [Persistence and rollback summary](dotnet-blog-upgrade/004-transform-ef6-localdb-persistence/modernization-summary.md).
- [Identity compatibility summary](dotnet-blog-upgrade/005-transform-identity-login-compatibility/modernization-summary.md).
- [Dependency CVE summary](dotnet-blog-upgrade/007-security-cve-remediation/cve-fix-summary.md).

The metadata records seven successful tasks and reports 21 integration checks. That is a claim from the saved run, not a test result reproduced here. In particular, the readiness summary records a `dotnet test` command with no dedicated test project: a successful exit alone is not evidence that tests were discovered. Revalidate the approved target against a disposable copy of the original database.

Other outputs under this folder are retained as supporting snapshots; the dashboard and plan linked above are the selected examples for this walkthrough. Do not copy saved session metadata into an active modernization session or execute it blindly.

For a fresh run, follow the [.NET modernization workflow](../../../docs/DOTNET-MODERNIZATION.md) and its approval and validation gates.
