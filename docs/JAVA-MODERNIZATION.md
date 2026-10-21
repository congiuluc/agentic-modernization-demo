# Java modernization

Modernize the Java blog with the **Modernize CLI**: assess the application, create a plan, approve it, execute the changes, and validate the result. The starting point is the [Java 11 Maven WAR](../java/pom.xml), using Spring MVC 5, Spring Security 5, Hibernate 5, JSP, H2, and Tomcat 9. It is **not Spring Boot**.

> **Modernization, not application startup, is the goal.** Local setup is an optional supporting step. Codespaces is an optional alternative with JDK 27 as the default toolchain and JDK 11 retained for baseline compatibility; neither changes the application's target automatically.

## Modernization workflow

### 1. Optional: set up and run the original application

Follow the [Java setup guide](../java/README.md) only if you need to install the baseline tools, launch the original WAR, or record its baseline behavior. Skip this setup step if the environment is already ready or you are only assessing and planning.

Before executing code changes, create a dedicated branch, record the baseline commit, and back up the H2 database. Baseline and regression evidence are still required before approving the resulting changes; skipping setup does not make an unexecuted check pass.

### 2. Assess with Modernize CLI

Install the shared [CLI prerequisites](MODERNIZATION.md#prerequisites) locally, or use the [optional Codespaces workspace](MODERNIZATION.md#using-github-codespaces). Follow its authentication guidance; a repository token alone is not a Copilot credential.

From the repository root in PowerShell 7:

```powershell
modernize --version
gh auth status
Set-Location java
modernize assess --format markdown --delegate local
```

Keep this terminal in the `java` folder through execution. Review reports under `.github/modernize/assessment/`.

For repeatable full-analysis coverage, use the included [Java YAML](../java/modernize.yaml) **instead of** the default assessment command:

```powershell
modernize assess --assess-config .\modernize.yaml --format markdown --delegate local
```

See [Java YAML options and limits](MODERNIZATION.md#optional-java-yaml-for-modernize-cli). This file does not select the build JDK, retarget the Java 11 WAR, or establish JDK 27 compatibility.

### 3. Generate and approve the plan

Select the report you reviewed, then generate a local plan:

```powershell
Get-ChildItem .\.github\modernize\assessment -Recurse -File | Select-Object -ExpandProperty FullName
$assessmentReport = Read-Host "Paste the full path of the assessment report file"
$goal = @'
Plan modernization of only the Java 11 Maven WAR in the current folder.
This is Spring MVC, not Spring Boot. Recommend compatible supported
JDK, Spring, Spring Security, Hibernate, H2, and JSP/Servlet container
versions. Evaluate JDK 27 compatibility and assessment-tool support
before selecting the runtime target; explain any blockers.
Resolve javax versus jakarta consistently. Preserve WAR packaging,
routes, form login, BCrypt hashes, CSRF protection, moderation,
and persistent H2 data. Include ordered tasks, data backup and rollback,
and build, HTTP, JSP-rendering, login, and restart-persistence checks.
Keep execution local. Do not introduce Spring Boot, add Azure services,
or deploy. Stop after planning for my approval; do not change
application code or install tools before approval.
'@
modernize plan create $goal --plan-name java-blog-upgrade --language java --assess-file-path $assessmentReport --delegate local
```

**Stop for approval.** Review `.github/modernize/java-blog-upgrade/plan.md` and `tasks.json`, including the [Java compatibility checklist](MODERNIZATION.md#java-planning-goal). Approve the JDK/framework/container combination, H2 migration strategy, rollback, and validation commands before executing.

### 4. Execute only the approved plan

From the same `java` folder, after approval:

```powershell
modernize plan execute --plan-name java-blog-upgrade --delegate local
```

Execution can modify code, dependencies, and commits. Inspect the complete branch history, not just uncommitted changes. Do not substitute `modernize upgrade`, which combines planning and execution.

### 5. Validate and review

Return to the repository root:

```powershell
Set-Location ..
```

Follow the [post-modernization validation steps](MODERNIZATION.md#6-validate-and-review-the-results) on the approved JDK and actual target servlet container. Verify JSP rendering, routes, administrator login, CSRF rejection, CRUD, moderation, and data persistence after restart.

The [existing baseline checks](MODERNIZATION.md#required-regression-checks) use Java 11/Tomcat 9. Cross-compiling with a newer JDK or passing those legacy checks does not prove the upgraded stack works. Adapt the validation/CI commands to the approved target, update the setup guide with verified new commands, and open a pull request for review.
