# Java modernization

Modernize the Java blog with the **Modernize CLI**: assess the application, create a plan, approve it, execute the changes, and validate the result. The starting point is the [Java 11 Maven WAR](../java/pom.xml), using Spring MVC 5, Spring Security 5, Hibernate 5, JSP, H2, and Tomcat 9. It is **not Spring Boot**.

> **Modernization, not application startup, is the goal.** Local setup is an optional supporting step. Codespaces is an optional alternative with JDK 27 as the default toolchain and JDK 11 retained for baseline compatibility; neither changes the application's target automatically.

## Modernization workflow

### 1. Optional: set up and run the original application

Follow the [Java setup guide](../java/README.md) only if you need to install the baseline tools, launch the original WAR, or record its baseline behavior. Skip this setup step if the environment is already ready or you are only assessing and planning.

Java already uses a local H2 file by default and automatically seeds two categories, twelve published posts, two drafts, and two comments. Set the required administrator environment variables on first startup; no prebuilt database or hardcoded credentials are needed.

Before executing code changes, create a dedicated branch, record the baseline commit, and back up the H2 database. Baseline and regression evidence are still required before approving the resulting changes; skipping setup does not make an unexecuted check pass.

### 2. Assess with Modernize CLI

Install the shared [CLI prerequisites](MODERNIZATION.md#prerequisites) locally, or use the [optional Codespaces workspace](MODERNIZATION.md#using-github-codespaces). Follow its authentication guidance; a repository token alone is not a Copilot credential.

From the repository root in PowerShell 7:

```powershell
modernize --version
gh auth status
Set-Location java
```

Then run **one** of the following assessments.

**Simple** (default issue analysis):

```powershell
modernize assess --source . --model auto
```

**Full analysis** (uses the included [Java YAML](../java/modernize.yaml): full coverage with codebase insights):

```powershell
modernize assess --source . --assess-config .\modernize.yaml --model auto
```

Keep this terminal in the `java` folder through execution. Review reports under `.github/modernize/assessment/`. See [Java YAML options and limits](MODERNIZATION.md#optional-java-yaml-for-modernize-cli). This file does not select the build JDK, retarget the Java 11 WAR, or establish JDK 27 compatibility.

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
modernize plan create $goal --plan-name java-blog-upgrade --language java --assess-file-path $assessmentReport --model auto
```

**Stop for approval.** Review `.github/modernize/java-blog-upgrade/plan.md` and `tasks.json`, including the [Java compatibility checklist](MODERNIZATION.md#java-planning-goal). Approve the JDK/framework/container combination, H2 migration strategy, rollback, and validation commands before executing.

### 4. Execute only the approved plan

From the same `java` folder, after approval:

```powershell
modernize plan execute --plan-name java-blog-upgrade --model auto
```

Execution can modify code, dependencies, and commits. Inspect the complete branch history, not just uncommitted changes. Do not substitute `modernize upgrade`, which combines planning and execution.

### 5. Validate and review

Return to the repository root:

```powershell
Set-Location ..
```

The agent's build/CVE checks are not proof of behavioral parity. Complete the checks below against the **approved JDK/framework/container combination**, using a disposable copy of the original H2 data.

#### Build and test the approved target

Select the approved JDK through `JAVA_HOME`/`PATH` or SDKMAN, then run from the repository root:

```powershell
java -version
mvn -version
mvn -B -ntp -f .\java\pom.xml clean verify
if ($LASTEXITCODE -ne 0) { throw "Maven build or verification failed." }
```

Check Maven's reported JVM, the approved compiler release, and the test reports. Confirm that the expected tests actually ran; a build with no tests does not prove behavioral parity. Codespaces defaults to JDK 27, but that is not approval of JDK 27 as the application runtime.

Cross-compiling the WAR for Java 11 on a newer JDK does not establish compatibility with that newer runtime. Record the actual build JDK separately from the runtime used below.

#### Deploy to the approved runtime

1. Deploy the generated `java/target/ROOT.war` to the **approved** servlet/JSP container, running on the **approved runtime JDK**, and bind HTTP to loopback.
2. Keep Spring, Hibernate, validation, Servlet/JSP APIs, and JSTL consistently on the approved `javax.*` or `jakarta.*` stack. Do not deploy a Jakarta migration to Tomcat 9 just because the old baseline used it.
3. Set `BLOG_JAVA_DB_PATH` to the disposable persisted H2 copy, without the `.mv.db` suffix, and preserve `BLOG_JAVA_DB_PASSWORD` if one was set. Follow the approved data migration; do not open the only existing database copy with an incompatible H2 version.
4. Preserve the original BCrypt users and seed state. Starting with a fresh database and newly seeded credentials does not prove migration compatibility.
5. Check startup/migration logs, the public home page, and JSP rendering before smoke checks. Keep the server running. Use the [Java setup guide](../java/README.md#local-tomcat-9-deployment) only for the unchanged Java 11/Tomcat 9 baseline, not as an implicit target-stack choice.

#### Run HTTP and form checks

In another PowerShell 7 terminal at the repository root, enter the **original administrator credentials** and the upgraded application's loopback URL:

```powershell
$env:BLOG_ADMIN_USER = Read-Host "Original administrator username"
$secret = Read-Host "Original administrator password" -AsSecureString
$env:BLOG_ADMIN_PASSWORD = [PSCredential]::new("local", $secret).GetNetworkCredential().Password
$baseUrl = Read-Host "Validation base URL (for example http://localhost:8080)"
.\scripts\Test-Blogs.ps1 -App Java -JavaBaseUrl $baseUrl
```

The [HTTP smoke checks](../scripts/Test-Blogs.ps1) exercise JSP-backed public pages, categories/pagination, private drafts, original administrator login, CRUD, publication timestamps, moderation, CSRF rejection, and invalid/not-found responses. They create and clean up uniquely named test records; use a disposable validation database and inspect any failure.

#### Prove restart and data persistence

The smoke command above does **not** include a `-RestartApplication` callback, so it does not establish restart persistence by itself. Perform this separate check:

1. Create a uniquely named post, publish it, and approve a comment. Record the content, publication timestamp, and existing seed-record counts.
2. Stop the servlet-container process completely and start it again with the **same persisted database and configuration**. For containerized hosting, replace the application container while retaining the data volume as specified in the plan.
3. Open a fresh browser session and sign in with the original administrator account.
4. Verify that JSP pages render, the post/timestamp/comment and edits survived, and seed records were not duplicated, reset, or overwritten.
5. Remove only the records you created for this check. A verified hosting restart command can instead be supplied to the smoke script's `-RestartApplication` parameter.

#### Review evidence and CI before merging

- Compare the complete branch against the recorded baseline commit, including agent-created commits, staged changes, and uncommitted files. Confirm that .NET remains unchanged.
- Record build/runtime JDKs, container/framework/H2 versions, test counts, executed commands, HTTP/JSP and restart results, remaining risks, and anything not tested.
- Update the setup guide, tests, and CI for the approved runtime. The [existing baseline runner](MODERNIZATION.md#required-regression-checks) uses Java 11/Tomcat 9 and cannot certify a different JDK/container.
- Review intentional route/form/configuration changes and dependency/CVE remediation against the plan; do not weaken assertions just to make them pass.
- Open a pull request only when the agreed acceptance criteria pass. Update the [verification status](MODERNIZATION.md#verification-status) only with executed evidence; unavailable checks must remain explicitly unverified.
