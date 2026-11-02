# Modernization Plan: LegacyBlog local modernization comparison

**Project**: LegacyBlog.sln (`LegacyBlog\LegacyBlog.csproj`)

---

## Technical Framework

- **Language**: C# 7.3 on .NET Framework 4.7.2 (baseline), target comparison `net10.0`
- **Framework**: ASP.NET MVC 5.2.9 (`System.Web` pipeline) + OWIN cookie auth
- **Build Tool**: Visual Studio/MSBuild non-SDK project (`packages.config`)
- **Database**: SQL Server Express LocalDB (`(LocalDB)\MSSQLLocalDB`, `LegacyBlogDotNet`)
- **Key Dependencies**: Entity Framework 6.4.4, ASP.NET Identity 2.2.4, OWIN 4.2.2, Razor/WebPages 3.2.9

---

## Overview

This modernization plan compares two local-only paths for `LegacyBlog.sln` from the 4.7.2 baseline: an in-place .NET Framework servicing path versus migration to `.NET 10` and ASP.NET Core MVC. The goal is to preserve existing behavior while preparing a safer long-term runtime path.

- Keep routes, authorization, antiforgery, validation, moderation, data persistence, and original logins intact.
- Preserve LocalDB-backed persistent content and administrator credentials across process restarts.
- Gate all execution behind measurable acceptance checks (build, HTTP, login, restart-persistence).

Execution is intentionally local-only. No Azure services, provisioning, or deployment are included.

---

## Migration Impact Summary

| Application | Original Service | New Azure Service | Authentication | Comments |
|-------------|------------------|-------------------|----------------|----------|
| LegacyBlog | Local IIS Express + LocalDB | None (local-only plan) | OWIN cookie / ASP.NET Identity | Compare in-place Framework servicing vs ASP.NET Core MVC on .NET 10 |

---

## Modernization options comparison (from 4.7.2 baseline)

### Option A: In-place .NET Framework path

- Keeps `System.Web` and MVC5/OWIN architecture intact.
- Lowest code churn; preserves current hosting model and most configuration surface.
- Limited strategic runway versus modern .NET because `System.Web` does not move to .NET 10.

### Option B: Migration to .NET 10 + ASP.NET Core MVC

- Replaces `System.Web` HTTP pipeline with ASP.NET Core middleware and endpoint routing.
- Replaces OWIN startup/auth registration with ASP.NET Core authentication middleware.
- Moves from legacy Razor/WebPages MVC5 view stack to ASP.NET Core Razor view engine.
- Converts non-SDK project format + `packages.config` to SDK-style project + PackageReference.
- Moves EF6 context/identity usage to a .NET 10-compatible persistence/auth strategy while preserving schema/data semantics.

### Compatibility notes required for this plan

- **System.Web**: cannot be carried to `.NET 10`; request lifecycle, error handling hooks, and module behavior must be mapped to middleware/filters.
- **OWIN**: `IAppBuilder`/`Startup` cookie config must map to ASP.NET Core authentication/cookie options with equivalent login path, cookie name, expiration, and SameSite intent.
- **Razor**: MVC5 Razor/WebPages conventions and helpers must be validated against ASP.NET Core Razor equivalents to preserve view behavior and antiforgery usage.
- **Project format**: legacy web csproj/web targets and `packages.config` require SDK-style conversion and package reference normalization.
- **EF6**: existing model, constraints, and seed/persistence behavior must be preserved; database creation/initialization behavior requires explicit parity checks.
- **LocalDB**: local SQL Server workflow remains local; connection-string semantics and restart persistence must be validated in the migrated runtime.
- **ASP.NET Identity hash compatibility**: original logins must remain valid by supporting legacy Identity v2 password hash verification during/after migration, with safe forward rehash strategy after successful login.

---

## Ordered modernization tasks (for approval before execution)

1. Capture a baseline snapshot and freeze local acceptance criteria/test data.
2. Create the side-by-side upgrade assessment between in-place Framework path and `.NET 10` migration path.
3. Plan HTTP pipeline and routing migration (`System.Web`/attribute routes to ASP.NET Core endpoint routing) with behavior parity requirements.
4. Plan authentication/authorization/antiforgery/validation parity, including Identity password-hash compatibility for original logins.
5. Plan EF6 + LocalDB data continuity, including moderation and restart persistence behavior.
6. Run measurable local acceptance checks for build, HTTP routes, login, and restart-persistence.
7. Perform dependency CVE remediation planning after migration tasks and before any future deployment stage.

Detailed execution metadata is in `.metadata\tasks.json`.

---

## Database backup and rollback plan

- Back up LocalDB database before any schema or runtime migration trial (full backup with timestamped artifact and restore script/command).
- Record rollback checkpoints per major phase (project-format conversion, auth migration, data layer migration).
- Define rollback trigger conditions: failed login compatibility, route regressions, data integrity mismatch, or restart-persistence failure.
- Rollback method: stop app, restore latest validated backup, restore prior source snapshot/tag, rerun baseline acceptance checks locally.

---

## Measurable local acceptance checks

- **Build**: solution builds successfully in legacy baseline context and in migrated context.
- **HTTP behavior**: key endpoints return expected statuses and route resolutions (public, admin, comment moderation, error path).
- **Login compatibility**: original seeded/admin credentials continue to authenticate; cookie/session behavior remains valid.
- **Restart persistence**: after creating/updating content and restarting the app process, data and original logins remain intact; seed data is not duplicated.

---

## Open Questions & Questionnaire

- [x] Q: Include environment/infrastructure provisioning? → A: No; code migration planning only.
- [x] Q: Include integration testing? → A: Yes; local acceptance verification task included.
- [x] Q: Include security/CVE remediation? → A: Yes; security task included.
- [x] Q: Deployment target? → A: No deployment; local modernization only.
- [x] Q: Include containerization? → A: No; not requested and no deployment target.
