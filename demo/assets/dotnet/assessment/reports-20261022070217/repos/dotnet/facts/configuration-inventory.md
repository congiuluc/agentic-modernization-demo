# Configuration & Externalized Settings Inventory

The project uses XML-first configuration centered on `Web.config`, plus environment-variable overrides for credentials and connection strings. It has a small profile surface (Debug/Release build configuration, no separate runtime profile files) and handles sensitive values primarily through per-process environment variables.

## Configuration Sources

| Source | Type | Path/Location | Notes |
|---|---|---|---|
| Application config | .NET XML config | `LegacyBlog\Web.config` | Primary application/runtime settings (`appSettings`, `connectionStrings`, `system.web`, `entityFramework`). |
| View-engine config | .NET XML config | `LegacyBlog\Views\Web.config` | Razor/view-specific namespaces and handler settings. |
| Package source config | NuGet config | `NuGet.Config` | Sets package repository path and NuGet source. |
| Build/dependency manifest | MSBuild project | `LegacyBlog\LegacyBlog.csproj` | Build configuration defaults/conditions and assembly references. |
| Package lock-style manifest | NuGet packages list | `LegacyBlog\packages.config` | Pins package versions for restore. |
| Assessment/tooling config | YAML | `modernize.yaml` | Modernization assessment configuration (`analysisCoverage`, target runtime for modernization process). |
| Environment variable source | Process environment | `BLOG_DOTNET_CONNECTION_STRING`, `BLOG_ADMIN_USER`, `BLOG_ADMIN_PASSWORD` | Used at runtime by `BlogContext` and bootstrap initialization logic. |

## Build Profiles

| Profile | Activation | Purpose | Key Dependencies/Plugins |
|---|---|---|---|
| Debug (default) | Automatic when `$(Configuration)` is empty (`Debug`) | Local development build defaults with full debug symbols. | `DefineConstants=TRACE`, `DebugType=full`, `DebugSymbols=true`. |
| Release | Manual (`/p:Configuration=Release` or IDE selection) | Optimized build output for release scenarios. | `Optimize=true`, `DebugType=pdbonly`. |
| AnyCPU (default platform) | Automatic when `$(Platform)` is empty (`AnyCPU`) | Default platform target. | MSBuild property in project file. |

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---|---|---|---|
| Default runtime | Implicit (no `appsettings.{Environment}.json` or Web.* transform files found) | `LegacyBlog\Web.config` | App settings, connection string, system.web and web server behavior. |
| Process-level override mode | Set environment variables before process start | `LegacyBlog\Web.config` + process environment | `BLOG_DOTNET_CONNECTION_STRING` overrides `name=Blog`; `BLOG_ADMIN_USER`/`BLOG_ADMIN_PASSWORD` provide bootstrap admin credentials when user store is empty. |

## Properties Inventory

### LegacyBlog

| Property Key | Default | Profiles | Source |
|---|---|---|---|
| `webpages:Version` | `3.0.0.0` | Default runtime | `Web.config` (`appSettings`) |
| `webpages:Enabled` | `false` | Default runtime | `Web.config` (`appSettings`) |
| `owin:AppStartup` | `LegacyBlog.App_Start.Startup` | Default runtime | `Web.config` (`appSettings`) |
| `connectionStrings:Blog` | `Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=LegacyBlogDotNet;Integrated Security=True;MultipleActiveResultSets=True` | Default runtime | `Web.config` (`connectionStrings`) |
| `BLOG_DOTNET_CONNECTION_STRING` | unset (falls back to `name=Blog`) | Process-level override mode | Environment variable (`BlogContext` constructor) |
| `compilation:debug` | `false` | Default runtime | `Web.config` (`system.web/compilation`) |
| `compilation:targetFramework` | `4.7.2` | Default runtime | `Web.config` (`system.web/compilation`) |
| `httpRuntime:targetFramework` | `4.7.2` | Default runtime | `Web.config` (`system.web/httpRuntime`) |
| `httpRuntime:maxRequestLength` | `1024` | Default runtime | `Web.config` (`system.web/httpRuntime`) |
| `httpRuntime:enableVersionHeader` | `false` | Default runtime | `Web.config` (`system.web/httpRuntime`) |
| `authentication:mode` | `None` | Default runtime | `Web.config` (`system.web/authentication`) |
| `customErrors:mode` | `On` | Default runtime | `Web.config` (`system.web/customErrors`) |
| `customErrors:defaultRedirect` | `~/error` | Default runtime | `Web.config` (`system.web/customErrors`) |
| `httpCookies:httpOnlyCookies` | `true` | Default runtime | `Web.config` (`system.web/httpCookies`) |
| `httpCookies:sameSite` | `Lax` | Default runtime | `Web.config` (`system.web/httpCookies`) |
| `system.webServer:validation:validateIntegratedModeConfiguration` | `false` | Default runtime | `Web.config` (`system.webServer/validation`) |
| `system.webServer:httpErrors:existingResponse` | `PassThrough` | Default runtime | `Web.config` (`system.webServer/httpErrors`) |
| `customHeaders:X-Content-Type-Options` | `nosniff` | Default runtime | `Web.config` (`system.webServer/httpProtocol`) |
| `customHeaders:X-Frame-Options` | `DENY` | Default runtime | `Web.config` (`system.webServer/httpProtocol`) |
| `entityFramework:provider:invariantName` | `System.Data.SqlClient` | Default runtime | `Web.config` (`entityFramework/providers`) |
| `BLOG_ADMIN_USER` | unset (required on first startup when no users exist) | Process-level override mode | Environment variable (`Bootstrap.Initialize`) |
| `BLOG_ADMIN_PASSWORD` | unset (required on first startup when no users exist) | Process-level override mode | Environment variable (`Bootstrap.Initialize`) |
| `NuGet:repositoryPath` | `packages` | Build/restore context | `NuGet.Config` |
| `NuGet:packageSource:nuget.org` | `https://api.nuget.org/v3/index.json` | Build/restore context | `NuGet.Config` |

## Startup Parameters & Resource Requirements

| Service | JVM/Runtime Options | Memory | Instance Count |
|---|---|---|---|
| LegacyBlog (ASP.NET MVC 5 on IIS Express/full IIS host) | No JVM options (not applicable). Runtime behavior is controlled by `Web.config` and optional environment variables. | Not explicitly configured in repository artifacts. | Not explicitly configured in repository artifacts. |
| SQL Server LocalDB (`MSSQLLocalDB`) | External dependency started/managed outside app config. | Not explicitly configured in repository artifacts. | Typically single local instance for development (not pinned in repo config). |

## Startup Dependency Chain

1. SQL Server LocalDB (`MSSQLLocalDB`) → must be available before the web app initializes `BlogContext` (`CreateDatabaseIfNotExists` + `db.Database.Initialize(false)`).
2. LegacyBlog web host process (IIS Express/full IIS) → reads `Web.config`, then optionally resolves `BLOG_DOTNET_CONNECTION_STRING`.
3. Bootstrap initialization (`Bootstrap.Initialize`) → when user store is empty, requires `BLOG_ADMIN_USER` and `BLOG_ADMIN_PASSWORD` to pass validation before startup can complete.
4. App request handling begins → after initialization/seed checks commit successfully.

Readiness/wait mechanism: no Compose/Kubernetes readiness probes found; startup failure is explicit through thrown exceptions during bootstrap/database initialization.

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage (masked) |
|---|---|---|
| `BLOG_ADMIN_PASSWORD` | Admin credential secret | Environment variable (`[MASKED]`) |
| `BLOG_ADMIN_USER` | Admin identity (sensitive account bootstrap input) | Environment variable (`[MASKED]`) |
| `BLOG_DOTNET_CONNECTION_STRING` | Database connection string (may contain credentials in non-default usage) | Environment variable (`[MASKED]`) |
| `connectionStrings:Blog` | Database connection string (default uses integrated security) | `Web.config` (`Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=LegacyBlogDotNet;Integrated Security=True;...`) |

### Secrets Provisioning Workflow

Secrets originate from per-process environment variables set before application startup. The web host process reads them directly through `Environment.GetEnvironmentVariable(...)`, and bootstrap logic enforces required admin credential presence/shape on first-run initialization. No external secret manager (Key Vault, Vault, AWS Secrets Manager) or managed identity/RBAC-backed secret retrieval workflow is defined in repository configuration.

## Feature Flags

| Flag Name | Default | Controlled By |
|---|---|---|
| `webpages:Enabled` | `false` | `Web.config` (`appSettings`) |

No dedicated feature-flag framework configuration (e.g., .NET FeatureManagement, LaunchDarkly, Unleash) was found.

## Framework & Runtime Versions

| Component | Version | Source |
|---|---|---|
| .NET Framework target | `v4.7.2` | `LegacyBlog.csproj` (`TargetFrameworkVersion`) |
| C# language version | `7.3` | `LegacyBlog.csproj` (`LangVersion`) |
| ASP.NET MVC | `5.2.9` | `packages.config` / `README.md` |
| Razor/WebPages | `3.2.9` | `packages.config` / `README.md` |
| Entity Framework | `6.4.4` | `packages.config` / `README.md` |
| ASP.NET Identity | `2.2.4` | `packages.config` / `README.md` |
| OWIN components | `4.2.2` (`Microsoft.Owin*`), `1.0` (`Owin`) | `packages.config` |
| Web infrastructure package | `2.0.0` | `packages.config` |
| MSBuild web targets package | `14.0.0.3` | `packages.config` |
| MSBuild project tools version | `15.0` | `LegacyBlog.csproj` (`ToolsVersion`) |
| NuGet feed endpoint | `https://api.nuget.org/v3/index.json` | `NuGet.Config` |

