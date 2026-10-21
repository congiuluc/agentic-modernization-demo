# Java 11 blog

A traditional Maven WAR: Spring MVC 5.3.39, Spring Security 5.8.16, Hibernate 5.6.15.Final, Bean Validation 6.2.5.Final, H2 2.2.224, JSP/JSTL, and XML configuration. No Spring Boot, Jakarta EE 9+, or Java features newer than 11.

> **Optional baseline setup.** To modernize this application, start with the [Java modernization workflow](../docs/JAVA-MODERNIZATION.md). Use this setup guide only when preparing or validating the original application.

## Prerequisites

- JDK **11**, Maven 3.6.3+, and **Tomcat 9** (Servlet 4 / `javax.servlet`).
- [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) (`pwsh`) for the commands below on Windows, Linux, and macOS.
- Internet access for first dependency restoration.
- A writable local directory for the H2 database.

No JDK or Tomcat installer is bundled. The manual setup below uses your installed tools. The optional disposable regression runner uses Docker and can pull the Tomcat image. A newer JDK can cross-compile with `--release 11`, but that alone does not verify Java 11 runtime compatibility.

## Build with Java 11

From this folder, start `pwsh` and point `JAVA_HOME` at your installed JDK 11. These commands work in PowerShell 7 on all three operating systems:

```powershell
$env:JAVA_HOME = (Resolve-Path -LiteralPath (Read-Host 'Installed JDK 11 directory')).Path
$env:PATH = (Join-Path $env:JAVA_HOME 'bin') + [IO.Path]::PathSeparator + $env:PATH
java -version
mvn -version
mvn -B -ntp clean package
if ($LASTEXITCODE -ne 0) { throw 'Java build failed.' }
```

The compiler uses `release=11`. The result is `target/ROOT.war`. Maven accepts JDK 11 or newer for cross-compilation; use `mvn -version` to distinguish the actual build JDK. Application class-file major version should be 55.

## Local Tomcat 9 deployment

Use a dedicated Tomcat base directory rather than modifying another application's deployment:

```powershell
$env:CATALINA_HOME = (Resolve-Path -LiteralPath (Read-Host 'Installed Tomcat 9 directory')).Path
$env:CATALINA_BASE = Join-Path $HOME '.legacyblog-tomcat'
New-Item -ItemType Directory -Force -Path $env:CATALINA_BASE | Out-Null
# Perform the configuration copy only when setting up a new base.
Copy-Item (Join-Path $env:CATALINA_HOME 'conf') -Destination (Join-Path $env:CATALINA_BASE 'conf') -Recurse
foreach ($folder in @('webapps', 'logs', 'temp', 'work')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $env:CATALINA_BASE $folder) | Out-Null
}
Copy-Item ./target/ROOT.war (Join-Path $env:CATALINA_BASE 'webapps/ROOT.war')
$env:BLOG_ADMIN_USER = 'your-admin-name'
$secret = Read-Host 'New local admin password (12-72 UTF-8 bytes, at least 12 characters)' -AsSecureString
$env:BLOG_ADMIN_PASSWORD = [PSCredential]::new('local', $secret).GetNetworkCredential().Password
```

Before starting, edit the **dedicated base's** `conf/server.xml`: bind its HTTP connector to `address="127.0.0.1"` and an unused port (examples use 8080). Disable its shutdown port with `<Server port="-1" ...>` and remove/disable any unused AJP connector. Do not expose this legacy sample to the network.

```powershell
$launcher = if ($IsWindows) { 'bin/catalina.bat' } else { 'bin/catalina.sh' }
& (Join-Path $env:CATALINA_HOME $launcher) run
```

Open `http://localhost:8080`. Use Ctrl+C in that foreground console to stop it. These instructions deploy as the root web application; JSP links also honor the servlet context path.

### Configuration and data lifecycle

| Environment variable | Behavior |
|---|---|
| `BLOG_ADMIN_USER` | First-run username: 3-64 ASCII letters/digits/`._-`. |
| `BLOG_ADMIN_PASSWORD` | First-run password: at least 12 characters, at most 72 UTF-8 bytes (BCrypt limit). |
| `BLOG_JAVA_DB_PATH` | H2 file path **without** `.mv.db`; default `${user.home}/.legacyblog-java/blog`. Parent directories must be writable. |
| `BLOG_JAVA_DB_PASSWORD` | Optional H2 file database password; use the same value on every startup. The default is empty for this local-only embedded sample. |

No TCP H2 server or database console is enabled. Do not run multiple application instances against the same embedded database file.

`src/main/resources/db/schema.sql` defines the explicit initial schema with non-destructive `IF NOT EXISTS` statements. Hibernate validates it instead of using `create-drop` or silently updating existing tables. To change the schema later, author a deliberate migration before starting the changed application. Seed state is persisted; restarts do not recreate deleted sample content or overwrite edits/passwords.

Login passwords are BCrypt-hashed. Spring Security provides session fixation protection, CSRF tokens, and admin authorization. Spring's form-tag HTML escaping and JSP output encoding are enabled; post bodies and comments remain plain text.

## Verify

With Tomcat running and the original administrator credentials set in another terminal:

```powershell
../scripts/Test-Blogs.ps1 -App Java -JavaBaseUrl http://localhost:8080
```

Create and publish a post, restart Tomcat with the same H2 path, and confirm the post and original admin login survive. Check Tomcat's console/logs if deployment fails; initialization errors are not a successful empty blog.

For an automated build, isolated Tomcat/database setup, and full restart regression test, run this from the repository root with Maven and Docker available:

```sh
pwsh -File scripts/Test-ApplicationBaseline.ps1 -App Java
```

The runner creates and removes only its own container, database volume, and test data. It stops the first container and starts a new one against the same database **without bootstrap credentials**. It also verifies that the source-only [security exercises](security-examples/VulnerableExamples.java) are absent from the WAR.

## Verification performed

- Maven WAR build with the installed **JDK 21 and `--release 11`**.
- **67 passing HTTP/form/restart assertions** against **Java 11 and Tomcat 9** in a disposable Docker container, including JSP rendering.
- Unsupported HTTP methods return 405, published-post edits preserve publication timestamps, republishing changes them, and deleting a post removes pending comments.
- The published post, approved comment, original administrator credentials, and seed record identities survive a complete container replacement without bootstrap credentials.
- These HTTP regressions run in the repository's [PR validation workflow](../.github/workflows/validate.yml); `mvn test` alone does not run them. Hosted CI execution still needs to be verified after pushing.

## Source-only security exercises

The [vulnerable Java examples](security-examples/VulnerableExamples.java) deliberately demonstrate SQL concatenation, raw HTML output, and unsalted MD5 password hashing. They are outside `src/main/java`, have no Spring annotations or application entry point, and are excluded from the WAR. They are for code-review/assessment training with synthetic data only, not a runnable application feature. See the [training boundaries](../docs/MODERNIZATION.md#intentional-security-exercises-source-only) before using them.

The chosen Spring/Hibernate lines are intentionally legacy and have maintenance/security limitations. This is a modernization sample, not a production support or security certification.
