# Baseline Summary — 001-setupBaseline-legacyblog-local

## Scope

- Migration scope: LegacyBlog (.NET Framework 4.7.2, System.Web/OWIN/MVC5) -> .NET 10 ASP.NET Core MVC
- Integration-test environment mode: `mock`
- In-scope module decision:
  - `LegacyBlog` -> **in scope** (owns HTTP routes, auth, antiforgery, moderation, and LocalDB persistence behavior)

## Entry-point inventory (module: LegacyBlog)

1. `GET /` and `GET /categories/{slug}` (`PublicController.Index`)
2. `POST /account/login` (`AccountController.Login`)
3. `POST /posts/{slug}/comments` (`PublicController.Comment`)
4. `POST /admin/comments/{id}/approve` (`AdminController.ApproveComment`)
5. `POST /admin/posts/create` (`AdminController.SavePost`)
6. `Application_Start` (`Global.asax.cs`) for DB init/bootstrap/restart behavior

## Existing integration test inventory

- Existing IT/E2E test files under module test roots: **none found**.
- Existing conventions inferred from app behavior and forms:
  - server-rendered MVC views
  - antiforgery required on state-changing POST endpoints
  - auth via OWIN cookie `LegacyBlogDotNet`
  - errors rendered via shared error view with status-specific message

## Test case counts (new vs existing-covered)

| Category | Existing-covered | New |
|---|---:|---:|
| happy-path | 0 | 6 |
| boundary | 0 | 3 |
| special-input | 0 | 6 |
| failure | 0 | 6 |
| **Total** | **0** | **21** |

## Baseline build + unit-test result

- Build: **pass**
  - Restore used `nuget.exe restore LegacyBlog.sln -ConfigFile NuGet.Config -PackagesDirectory .\\packages`
  - Build used MSBuild 16.11 and produced `LegacyBlog/bin/LegacyBlog.dll`
- Unit tests: **pass**
  - No unit test projects discovered in solution scope (`0`), therefore no failures

## `testdata/` fixtures produced

- `testdata/configuration/source-snapshot.md`
- `testdata/configuration/startup-env-valid.json`
- `testdata/configuration/startup-env-invalid.json`
- `testdata/seed-data/bootstrap-baseline.md`
- `testdata/seed-data/restart-persistence-seed.md`
- `testdata/inputs/*.json` (login/comment/admin post payloads)
- `testdata/expectations/*.md|*.txt` (HTTP/build/startup/restart expected outcomes)

## Infra decision table (Step 5)

- File: `LegacyBlog/test/test-cases/infra-decision-table.md`
- Mode rule applied: `mock` -> every dependency decision set to `mock`
- Dependencies recorded:
  - LocalDB persistence
  - startup env credential bootstrap inputs
  - local filesystem log sink

## Freeze gate / checklist

- `LegacyBlog/test/test-cases/test-cases.md`: required fields checklist completed
- Coverage gate satisfied for inventoried entry points (happy + failure coverage)
- Baseline bundle declared **FROZEN**:
  - `LegacyBlog/test/test-cases/test-cases.md`
  - `LegacyBlog/test/test-cases/infra-decision-table.md`
  - `LegacyBlog/test/test-cases/testdata/**`

