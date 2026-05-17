# Refactor Baseline

Captured on 2026-05-17 for branch `refactor/phase-1`.

## Commands

| Command | Exit status | Outcome |
| --- | ---: | --- |
| `dotnet restore KoiFengShuiSystem.sln` | 0 | Restore succeeded. Reported pre-existing package vulnerability warnings: `AutoMapper` 14.0.0 NU1903, `MailKit` 4.8.0 NU1902, `MimeKit` 4.11.0 NU1902. |
| `dotnet build KoiFengShuiSystem.sln --no-restore` | 0 | Build succeeded. Confirmation run reported `106 Warning(s), 0 Error(s)`. |
| `dotnet test KoiFengShuiSystem.sln --no-build` | 0 | Solution-level test command exited successfully but produced no test result details. This did not exercise `KoiFengShuiSystem.Tests` because `KoiFengShuiSystem.Tests/KoiFengShuiSystem.Tests.csproj` is not included in `KoiFengShuiSystem.sln`. |
| `dotnet test KoiFengShuiSystem.Tests\KoiFengShuiSystem.Tests.csproj` | 1 | Direct test-project command discovered the test assembly but aborted before running tests because the machine lacks `Microsoft.NETCore.App` version `8.0.0` x64. |
| `dotnet build KoiFengShuiSystem.sln --no-restore` (verification run) | 0 | Build succeeded. `4 Warning(s), 0 Error(s)` — warnings are pre-existing package vulnerabilities only. |
| `dotnet test KoiFengShuiSystem.sln --no-build` (verification run) | 0 | Test run discovered both new test projects. **UnitTests (tests/UnitTests):** 89 Passed, 2 Failed — same pre-existing CompatibilityService scoring bugs. **IntegrationTests (tests/IntegrationTests):** 1 Passed, 0 Failed — Swagger bootstrap test. |
| `dotnet build KoiFengShuiSystem.sln --no-restore` (Phase 3 verification) | 0 | Build succeeded. `4 Warning(s), 0 Error(s)` — same pre-existing package vulnerabilities only. |
| `dotnet test KoiFengShuiSystem.sln --no-build` (Phase 3 verification) | 0 | **91/91 Passed, 0 Failed** — UnitTests. **1/1 Passed, 0 Failed** — IntegrationTests. Both CompatibilityService scoring bugs are now fixed. |

## Baseline Issues

- ~~Pre-existing solution coverage issue: `KoiFengShuiSystem.Tests` exists but is not included in `KoiFengShuiSystem.sln`, so `dotnet test KoiFengShuiSystem.sln --no-build` does not run the test project.~~ **RESOLVED.** The old `KoiFengShuiSystem.Tests` project has been removed from the repository. Two new test projects are now included in the solution:
  - `tests/UnitTests/UnitTests.csproj` — unit tests (net8.0)
  - `tests/IntegrationTests/IntegrationTests.csproj` — integration tests (net8.0)
- ~~Pre-existing environment/runtime issue: direct execution of `KoiFengShuiSystem.Tests` aborts because `Microsoft.NETCore.App 8.0.0` x64 is missing. Installed x64 runtimes reported by the testhost error were `3.1.32`, `6.0.16`, and `10.0.8`.~~ **RESOLVED (obsolete).** The removed project targeted `net8.0`; replacement projects also target `net8.0` but a compatible runtime is available via the SDK, so they run successfully.
- Pre-existing package vulnerability warnings were reported during restore/build for `AutoMapper` (NU1903, high), `MailKit` (NU1902, moderate), and `MimeKit` (NU1902, moderate). Still present as of verification run.
- Pre-existing compiler warnings remain in the build baseline; build still exits 0. No new warnings introduced by the new test projects.
- ~~**Known test gaps:** 2 of 91 unit tests fail (pre-existing CompatibilityService scoring bugs — `AssessCompatibility_ValidRequest_ReturnsResponseWithScores` expects 100 but gets 0; `AssessCompatibility_FullyCompatible_ReturnsPerfectOverallScore` expects 100 but gets 75). These are pre-existing service-level bugs, not test harness issues.~~ **RESOLVED (Phase 3).** The CompatibilityService color-scoring bug was fixed by switching from full-string matching to word-based color matching. All 91 unit tests now pass.

## Phase 3 Completion

**Task 3.1 (Move DbContext):** `KoiFengShuiContext` moved from `KoiFengShuiSystem.DataAccess/Models/` to `src/Shared/Shared.Infrastructure/Persistence/KoiFengShuiContext.cs`. All 22 entity models moved to `src/Shared/Shared.Kernel/Models/` with namespace preserved (`KoiFengShuiSystem.DataAccess.Models`) to minimize consumer churn. `PaginatedList` moved to `Shared.Infrastructure/Persistence/` to remove EF Core dependency from `Shared.Kernel`.

**Task 3.2 (Extract Entity Configurations):** All 14 relationship configurations extracted from `OnModelCreating` into individual `IEntityTypeConfiguration<T>` classes under `src/Shared/Shared.Infrastructure/Persistence/Configurations/`. `OnModelCreating` now uses `ApplyConfigurationsFromAssembly`.

**Task 3.3 (DI Extension):** `AddSharedInfrastructure` extension method added in `src/Shared/Shared.Infrastructure/DependencyInjection.cs`, registered in `Program.cs`.

**Bug fix (discovered during Phase 3):** `CompatibilityService` color scoring was comparing full cleaned strings against word-split color lists. Fixed by splitting cleaned colors into words and checking word-level membership against `recommendedColors` and `elementColors`.
