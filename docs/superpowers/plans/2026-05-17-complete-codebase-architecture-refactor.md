# Complete Codebase Architecture Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the current layered ASP.NET Core API into the documented modular-monolith architecture without breaking existing API behavior.

**Architecture:** Move from projects organized by technical layer (`Api`, `Services`, `DataAccess`, `Shared`, `Common`) to bounded-context modules (`Identity`, `FengShui`, `Community`, `Marketplace`, `Payments`, `Admin`, `Notifications`) hosted by one ASP.NET Core application. Preserve one deployable unit and one SQL Server database while introducing clear contracts, module installers, focused EF configurations, structured logging, validation, tests, and architecture boundary checks.

**Tech Stack:** .NET 8, ASP.NET Core Controllers, EF Core SQL Server, xUnit, Moq or NSubstitute, FluentAssertions, Swagger/OpenAPI, JWT Bearer, PayOS, Cloudinary, MailKit/MimeKit, Docker.

**Confidence:** high. The desired target architecture is well described in `KoiFengShuiSystem_Documentation.md`, and the current repository structure shows a clear layered starting point. The main risk is migration size, so this plan uses an incremental strangler approach rather than a big-bang rewrite.

---

## Current State Summary

The repository currently uses these projects:

- `KoiFengShuiSystem.Api`: ASP.NET Core host, controllers, JWT middleware, traffic logging middleware, startup wiring.
- `KoiFengShuiSystem.Services`: business services, service interfaces, background transaction sync, JWT/email/cloud services.
- `KoiFengShuiSystem.DataAccess`: EF Core entities, `KoiFengShuiContext`, repositories, migrations.
- `KoiFengShuiSystem.Shared`: request/response DTOs and helper settings.
- `KoiFengShuiSystem.Common`: shared constants and `CungPhiCalculator`.
- `KoiFengShuiSystem.Tests`: existing tests, including Feng Shui calculator tests.

The target architecture from documentation is a modular monolith with:

- `src/Host`
- `src/Modules/<Module>/<Module>.Api`
- `src/Modules/<Module>/<Module>.Application`
- `src/Modules/<Module>/<Module>.Domain`
- `src/Modules/<Module>/<Module>.Infrastructure`
- `src/Shared/Shared.Kernel`
- `src/Shared/Shared.Infrastructure`
- `tests/UnitTests`
- `tests/IntegrationTests`

---

## Migration Principles

- Preserve public API routes and response shapes until a deliberate API versioning phase changes them.
- Keep the database schema stable during module extraction; do not rename tables and columns as part of the first migration phases.
- Move code by bounded context, one vertical slice at a time.
- Add tests before moving behavior so refactors are protected.
- Do not introduce MediatR until module boundaries exist and there is an actual cross-module use case to route through it.
- Keep one shared EF Core `DbContext` initially; split configuration into per-entity files before considering per-module contexts.
- Use contracts in `Shared.Kernel` only when a type is truly cross-module.
- Keep DTOs near their API/application module unless shared by multiple modules.

---

## Target Project Structure

Create this structure over the migration:

```text
src/
  Host/
    Host.csproj
    Program.cs
    Middleware/
    Extensions/
  Modules/
    Identity/
      Identity.Api/
      Identity.Application/
      Identity.Domain/
      Identity.Infrastructure/
    FengShui/
      FengShui.Api/
      FengShui.Application/
      FengShui.Domain/
      FengShui.Infrastructure/
    Community/
      Community.Api/
      Community.Application/
      Community.Domain/
      Community.Infrastructure/
    Marketplace/
      Marketplace.Api/
      Marketplace.Application/
      Marketplace.Domain/
      Marketplace.Infrastructure/
    Payments/
      Payments.Api/
      Payments.Application/
      Payments.Domain/
      Payments.Infrastructure/
    Admin/
      Admin.Api/
      Admin.Application/
      Admin.Infrastructure/
    Notifications/
      Notifications.Application/
      Notifications.Infrastructure/
  Shared/
    Shared.Kernel/
    Shared.Infrastructure/
tests/
  UnitTests/
  IntegrationTests/
```

---

## Module Ownership Map

Use this map when moving current files.

| Current code | Target module | Notes |
|---|---|---|
| `AuthController`, `AccountController`, `AccountService`, `AdminAccountService`, `JwtUtils`, `Account`, `Role` | `Identity` | Auth, profile, roles, account ownership. |
| `ConsultationController`, `CompatibilityController`, `ElementController`, `ConsultationService`, `CompatibilityService`, `ElementService`, `CungPhiCalculator`, `Element`, `Direction`, `FengShuiDirection`, `FishPond`, `KoiBreed`, `ShapeCategory`, `Recommendation`, `Country` | `FengShui` | Core Feng Shui engine and recommendation domain. |
| `PostController`, `PostService`, `AdminPostService`, `AdminPostImageService`, `Post`, `PostImage`, `PostCategory`, `Follow`, post image DTOs | `Community` | User-generated content and moderation data. |
| `MarketplaceListingsController`, `MarketCategoryController`, `SubcriptionTiersController`, `MarketplaceListingService`, `MarketCategoryService`, `SubcriptionTiersService`, `MarketplaceListing`, `MarketCategory`, `SubcriptionTier`, `ListingImage` | `Marketplace` | Listing catalog, listing images, subscription tier catalog. |
| `TransactionController`, `TransactionService`, `TransactionSyncService`, `Transaction` | `Payments` | PayOS payment creation, webhook, reconciliation. |
| `DashboardController`, `FAQController`, `DashboardService`, `FAQService`, `FAQ`, `TrafficLog` | `Admin` | Admin dashboard and FAQ management. If public FAQ grows, split later. |
| `UploadImageController`, `ImageService`, `CloudService`, `Image`, `CloundSettings` | `Shared.Infrastructure` initially | Cloudinary is cross-cutting. Later expose through module-specific upload policies. |
| `EmailService`, `MailSettings`, `MailData` | `Notifications` | Email delivery and future push notification boundary. |
| `BusinessResult`, pagination helpers, common result types | `Shared.Kernel` | Cross-module primitives only. |

---

## Phase 0: Baseline, Safety, and Inventory

**Goal:** Establish a reliable baseline before structural changes.

### Task 0.1: Capture Current Build and Test Baseline

**Files:** no code changes expected.

- [ ] Run `dotnet restore KoiFengShuiSystem.sln`.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Run `dotnet test KoiFengShuiSystem.sln --no-build`.
- [ ] Record failing tests or build errors in `docs/refactor-baseline.md` with exact command output summaries.
- [ ] If the baseline fails, classify failures as `pre-existing` and do not fix unrelated failures in this phase.
- [ ] Commit only the baseline document if created: `git add docs/refactor-baseline.md && git commit -m "docs: capture refactor baseline"`.

### Task 0.2: Generate Dependency Inventory

**Files:**
- Create: `docs/architecture/current-dependencies.md`

- [ ] List all project references from every `.csproj`.
- [ ] List all controllers and injected service interfaces.
- [ ] List all service interfaces and concrete implementations.
- [ ] List all EF entities and navigation-heavy aggregate relationships.
- [ ] Create a dependency table with columns: `Source`, `Depends On`, `Reason`, `Target Phase`.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Commit: `git add docs/architecture/current-dependencies.md && git commit -m "docs: map current project dependencies"`.

### Task 0.3: Add Architecture Decision Records

**Files:**
- Create: `docs/adr/0001-modular-monolith.md`
- Create: `docs/adr/0002-shared-dbcontext-first.md`
- Create: `docs/adr/0003-preserve-api-contracts-during-refactor.md`

- [ ] Document that the architecture remains a single deployable modular monolith.
- [ ] Document that one shared `DbContext` is retained until all entities and configurations are module-owned.
- [ ] Document that API contracts are preserved during migration.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Commit: `git add docs/adr && git commit -m "docs: add architecture refactor decisions"`.

---

## Phase 1: Solution Skeleton and Shared Foundations

**Goal:** Add the new architecture shell while keeping the current application running.

### Task 1.1: Create New Solution Folders and Shared Projects

**Files:**
- Create: `src/Shared/Shared.Kernel/Shared.Kernel.csproj`
- Create: `src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj`
- Modify: `KoiFengShuiSystem.sln`

- [ ] Create `Shared.Kernel` as a `net8.0` class library with nullable and implicit usings enabled.
- [ ] Create `Shared.Infrastructure` as a `net8.0` class library referencing `Shared.Kernel`.
- [ ] Add both projects to `KoiFengShuiSystem.sln`.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Commit: `git add src/Shared KoiFengShuiSystem.sln && git commit -m "refactor: add shared architecture projects"`.

### Task 1.2: Move Cross-Cutting Primitives to Shared.Kernel

**Files:**
- Move from: `KoiFengShuiSystem.Services/ViewModel/BusinessResult.cs`
- Move from: `KoiFengShuiSystem.Shared/Helpers/PaginationFilter.cs`
- Move from: `KoiFengShuiSystem.Shared/Helpers/PaginatedList.cs`
- Target: `src/Shared/Shared.Kernel/Results/BusinessResult.cs`
- Target: `src/Shared/Shared.Kernel/Pagination/PaginationFilter.cs`
- Target: `src/Shared/Shared.Kernel/Pagination/PaginatedList.cs`

- [ ] Move the files without changing behavior.
- [ ] Update namespaces to `KoiFengShuiSystem.Shared.Kernel.Results` and `KoiFengShuiSystem.Shared.Kernel.Pagination`.
- [ ] Add references from old projects that still consume these types to `Shared.Kernel`.
- [ ] Update using statements.
- [ ] Run `dotnet test KoiFengShuiSystem.sln --no-restore`.
- [ ] Commit: `git add . && git commit -m "refactor: move shared primitives to kernel"`.

### Task 1.3: Introduce Module Installer Contract

**Files:**
- Create: `src/Shared/Shared.Kernel/Modules/IModuleInstaller.cs`
- Create: `KoiFengShuiSystem.Api/Extensions/ModuleInstallerExtensions.cs`
- Modify: `KoiFengShuiSystem.Api/Program.cs`

- [ ] Add `IModuleInstaller` with `void AddServices(IServiceCollection services, IConfiguration configuration);`.
- [ ] Add extension `AddModuleInstallersFromAssemblies` that scans assemblies for concrete `IModuleInstaller` implementations.
- [ ] Keep all current manual registrations in `Program.cs` for now.
- [ ] Add an empty installer in the current API project to validate scanning.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Commit: `git add . && git commit -m "refactor: introduce module installer contract"`.

### Task 1.4: Remove Duplicate Startup Registrations

**Files:**
- Modify: `KoiFengShuiSystem.Api/Program.cs`

- [ ] Remove the duplicate `AddControllers()` block at lines equivalent to the current second controller registration.
- [ ] Remove duplicate `ITransactionService` registration.
- [ ] Keep service lifetimes unchanged otherwise.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Run existing API smoke tests if available; otherwise run `dotnet test KoiFengShuiSystem.sln --no-build`.
- [ ] Commit: `git add KoiFengShuiSystem.Api/Program.cs && git commit -m "refactor: clean duplicate startup registrations"`.

---

## Phase 2: Testing Harness and Contract Protection

**Goal:** Lock current behavior before moving modules.

### Task 2.1: Split Test Projects

**Files:**
- Create: `tests/UnitTests/UnitTests.csproj`
- Create: `tests/IntegrationTests/IntegrationTests.csproj`
- Modify: `KoiFengShuiSystem.sln`

- [ ] Create `UnitTests` with xUnit, FluentAssertions, and references to current service/common projects plus new shared projects.
- [ ] Create `IntegrationTests` with xUnit, FluentAssertions, `Microsoft.AspNetCore.Mvc.Testing`, and references to the API project.
- [ ] Move existing `KoiFengShuiSystem.Tests/FengShui/CungPhiCalculatorTests.cs` to `tests/UnitTests/FengShui/CungPhiCalculatorTests.cs`.
- [ ] Keep `KoiFengShuiSystem.Tests` in the solution until all tests are moved, then remove it in a later cleanup task.
- [ ] Run `dotnet test KoiFengShuiSystem.sln --no-restore`.
- [ ] Commit: `git add tests KoiFengShuiSystem.sln && git commit -m "test: add unit and integration test projects"`.

### Task 2.2: Add API Contract Smoke Tests

**Files:**
- Create: `tests/IntegrationTests/ApiContractTests.cs`

- [ ] Add smoke tests for public GET endpoints that do not require external credentials: `/swagger/v1/swagger.json`, public posts feed if available, marketplace listings if available, FAQ list if available.
- [ ] Configure test environment to avoid real PayOS, Cloudinary, and email calls.
- [ ] If current startup requires PayOS keys at boot, add test-only environment variables in the test factory.
- [ ] Run `dotnet test tests/IntegrationTests/IntegrationTests.csproj --no-restore`.
- [ ] Commit: `git add tests/IntegrationTests && git commit -m "test: add api contract smoke tests"`.

### Task 2.3: Add Service Characterization Tests

**Files:**
- Create tests under `tests/UnitTests/Identity/`, `tests/UnitTests/FengShui/`, `tests/UnitTests/Community/`, `tests/UnitTests/Marketplace/`, `tests/UnitTests/Payments/`, `tests/UnitTests/Admin/`

- [ ] Add tests for Cung Phi calculation and compatibility scoring edge cases.
- [ ] Add tests for account registration/login failure paths without hitting external Google OAuth.
- [ ] Add tests for marketplace listing validation and ownership rules.
- [ ] Add tests for transaction creation request shaping using a fake PayOS abstraction if one already exists; otherwise defer PayOS abstraction to Phase 7.
- [ ] Add tests for FAQ creation and admin answer behavior.
- [ ] Run `dotnet test tests/UnitTests/UnitTests.csproj --no-restore`.
- [ ] Commit: `git add tests/UnitTests && git commit -m "test: characterize core service behavior"`.

---

## Phase 3: EF Core Infrastructure Refactor

**Goal:** Make persistence modular without changing schema.

### Task 3.1: Move DbContext to Shared.Infrastructure

**Files:**
- Move from: `KoiFengShuiSystem.DataAccess/Models/KoiFengShuiContext.cs`
- Target: `src/Shared/Shared.Infrastructure/Persistence/KoiFengShuiContext.cs`
- Modify project references as needed.

- [ ] Move the `KoiFengShuiContext` class and preserve class name.
- [ ] Update namespace to `KoiFengShuiSystem.Shared.Infrastructure.Persistence`.
- [ ] Keep all `DbSet` properties unchanged.
- [ ] Keep existing migrations in `KoiFengShuiSystem.DataAccess/Migrations` during this phase to avoid EF migration churn.
- [ ] Update `Program.cs`, repositories, and design-time factory references.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Run `dotnet test KoiFengShuiSystem.sln --no-build`.
- [ ] Commit: `git add . && git commit -m "refactor: move dbcontext to shared infrastructure"`.

### Task 3.2: Extract Entity Configurations

**Files:**
- Create: `src/Shared/Shared.Infrastructure/Persistence/Configurations/*.cs`
- Modify: `src/Shared/Shared.Infrastructure/Persistence/KoiFengShuiContext.cs`

- [ ] Create one `IEntityTypeConfiguration<TEntity>` class per entity relationship currently configured in `OnModelCreating`.
- [ ] Move relationship configuration from `OnModelCreating` into those configuration classes.
- [ ] Replace explicit configuration body with `modelBuilder.ApplyConfigurationsFromAssembly(typeof(KoiFengShuiContext).Assembly);`.
- [ ] Generate an EF migration in a temporary branch or local check to confirm no schema changes are detected.
- [ ] Delete the generated no-op migration if one was created only for verification.
- [ ] Run `dotnet test KoiFengShuiSystem.sln --no-restore`.
- [ ] Commit: `git add src/Shared/Shared.Infrastructure && git commit -m "refactor: extract ef entity configurations"`.

### Task 3.3: Introduce Persistence Registration Extension

**Files:**
- Create: `src/Shared/Shared.Infrastructure/DependencyInjection.cs`
- Modify: `KoiFengShuiSystem.Api/Program.cs`

- [ ] Add `AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)`.
- [ ] Move `AddDbContext<KoiFengShuiContext>`, memory cache, response caching, shared settings binding, and shared external clients into this extension only where they are truly shared.
- [ ] Keep module-specific services registered in `Program.cs` until their module phase.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Commit: `git add . && git commit -m "refactor: centralize shared infrastructure registration"`.

---

## Phase 4: Host Project Migration

**Goal:** Create `src/Host` and make it the composition root.

### Task 4.1: Create Host Project

**Files:**
- Create: `src/Host/Host.csproj`
- Move from: `KoiFengShuiSystem.Api/Program.cs`
- Target: `src/Host/Program.cs`
- Move from: `KoiFengShuiSystem.Api/Authorization/JwtMiddleware.cs`
- Target: `src/Host/Middleware/JwtMiddleware.cs`
- Move from: `KoiFengShuiSystem.Api/Authorization/TrafficLoggingMiddleware.cs`
- Target: `src/Host/Middleware/TrafficLoggingMiddleware.cs`
- Modify: `KoiFengShuiSystem.sln`

- [ ] Create `Host.csproj` as the ASP.NET Core web project.
- [ ] Move `Program.cs` and middleware into `src/Host`.
- [ ] Keep controllers temporarily referenced from old `KoiFengShuiSystem.Api` through project references until module API projects exist.
- [ ] Update Dockerfile to run `src/Host/Host.csproj` only after local build passes.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Run `dotnet test KoiFengShuiSystem.sln --no-build`.
- [ ] Commit: `git add src/Host KoiFengShuiSystem.sln Dockerfile && git commit -m "refactor: introduce host project"`.

### Task 4.2: Add Global Exception Middleware

**Files:**
- Create: `src/Host/Middleware/ExceptionMiddleware.cs`
- Modify: `src/Host/Program.cs`
- Test: `tests/IntegrationTests/ErrorHandlingTests.cs`

- [ ] Add an integration test proving unhandled exceptions return a consistent JSON error envelope.
- [ ] Implement `ExceptionMiddleware` using `ILogger<ExceptionMiddleware>`.
- [ ] Return `500` with a stable response body containing `message`, `traceId`, and no stack trace outside development.
- [ ] Register middleware before authentication and custom JWT middleware.
- [ ] Run `dotnet test tests/IntegrationTests/IntegrationTests.csproj --no-restore`.
- [ ] Commit: `git add src/Host tests/IntegrationTests && git commit -m "feat: add global exception middleware"`.

### Task 4.3: Add Serilog Structured Logging

**Files:**
- Modify: `src/Host/Host.csproj`
- Modify: `src/Host/Program.cs`
- Modify: `src/Host/appsettings.json`

- [ ] Add Serilog packages for ASP.NET Core and console sink.
- [ ] Replace `builder.Logging.ClearProviders()`, `AddConsole()`, and `AddDebug()` with Serilog host integration.
- [ ] Configure request logging with route, status code, elapsed time, user id if available, and correlation id.
- [ ] Ensure no sensitive values are logged.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Commit: `git add src/Host && git commit -m "chore: configure structured logging"`.

---

## Phase 5: FengShui Module Extraction

**Goal:** Extract the core domain first because it has isolated business logic and existing tests.

### Task 5.1: Create FengShui Module Projects

**Files:**
- Create: `src/Modules/FengShui/FengShui.Domain/FengShui.Domain.csproj`
- Create: `src/Modules/FengShui/FengShui.Application/FengShui.Application.csproj`
- Create: `src/Modules/FengShui/FengShui.Infrastructure/FengShui.Infrastructure.csproj`
- Create: `src/Modules/FengShui/FengShui.Api/FengShui.Api.csproj`
- Modify: `KoiFengShuiSystem.sln`

- [ ] Create module projects with correct references: Api -> Application; Infrastructure -> Application + Domain + Shared.Infrastructure; Application -> Domain + Shared.Kernel; Domain -> Shared.Kernel only if needed.
- [ ] Add projects to solution.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Commit: `git add src/Modules/FengShui KoiFengShuiSystem.sln && git commit -m "refactor: add feng shui module projects"`.

### Task 5.2: Move FengShui Domain Entities

**Files:**
- Move `Element`, `Direction`, `FengShuiDirection`, `FishPond`, `KoiBreed`, `ShapeCategory`, `Recommendation`, `Country` from current data access models to `FengShui.Domain/Entities/`.

- [ ] Move entities without changing properties or table mappings.
- [ ] Update namespaces.
- [ ] Update `KoiFengShuiContext` `DbSet` types and EF configurations.
- [ ] Keep navigation properties to cross-module entities only where required by EF; mark cross-module cleanup for later contract phase.
- [ ] Run `dotnet test tests/UnitTests/UnitTests.csproj --no-restore`.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Commit: `git add . && git commit -m "refactor: move feng shui domain entities"`.

### Task 5.3: Move FengShui Application Services

**Files:**
- Move `ConsultationService`, `CompatibilityService`, `ElementService`, `IConsultationService`, `ICompatibilityService`, `IElementService`, `CungPhiCalculator` into `FengShui.Application`.
- Move related DTOs from `KoiFengShuiSystem.Shared/Models/Request` and `Response` into `FengShui.Application` or `FengShui.Api` depending on use.

- [ ] Move calculator first and keep existing tests passing.
- [ ] Move service interfaces and concrete services.
- [ ] Update namespaces and project references.
- [ ] Register services through `FengShui.Infrastructure/FengShuiModuleInstaller.cs`.
- [ ] Replace manual service registrations in Host with module installer registration.
- [ ] Run `dotnet test tests/UnitTests/UnitTests.csproj --no-restore --filter FengShui`.
- [ ] Run `dotnet test KoiFengShuiSystem.sln --no-restore`.
- [ ] Commit: `git add . && git commit -m "refactor: move feng shui application services"`.

### Task 5.4: Move FengShui Controllers

**Files:**
- Move `ConsultationController`, `CompatibilityController`, `ElementController` into `FengShui.Api/Controllers/`.

- [ ] Move controllers without changing routes.
- [ ] Update Host to discover controllers from module API assemblies.
- [ ] Ensure Swagger still exposes the same endpoints.
- [ ] Run integration tests for Feng Shui endpoints.
- [ ] Commit: `git add . && git commit -m "refactor: move feng shui api surface"`.

---

## Phase 6: Identity Module Extraction

**Goal:** Extract authentication, accounts, JWT support, and roles.

### Task 6.1: Create Identity Module Projects

**Files:**
- Create projects under `src/Modules/Identity/`.
- Modify: `KoiFengShuiSystem.sln`

- [ ] Create `Identity.Domain`, `Identity.Application`, `Identity.Infrastructure`, and `Identity.Api`.
- [ ] Set references using the same pattern as FengShui.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Commit: `git add src/Modules/Identity KoiFengShuiSystem.sln && git commit -m "refactor: add identity module projects"`.

### Task 6.2: Move Identity Entities and Services

**Files:**
- Move `Account`, `Role` into `Identity.Domain/Entities/`.
- Move `AccountService`, `AdminAccountService`, `JwtUtils`, `IAccountService` into Identity application/infrastructure as appropriate.
- Move auth/account DTOs into Identity API/Application.

- [ ] Move entities and update EF context/configurations.
- [ ] Move service interfaces and implementations.
- [ ] Keep password hashing behavior unchanged.
- [ ] Move JWT option binding and token generation into `Identity.Infrastructure`.
- [ ] Add `IdentityModuleInstaller`.
- [ ] Remove Identity service registrations from Host.
- [ ] Run unit tests for register/login/profile behavior.
- [ ] Run full integration tests.
- [ ] Commit: `git add . && git commit -m "refactor: extract identity module"`.

### Task 6.3: Move Identity Controllers and Auth Attributes

**Files:**
- Move `AuthController`, `AccountController` into `Identity.Api`.
- Evaluate `AuthorizeAttribute` and `AllowAnonymousAttribute`: keep in Host only if global, otherwise move to Shared.Kernel or Identity.Api.

- [ ] Move controllers without changing routes.
- [ ] Confirm `[Authorize]` behavior still works for protected endpoints.
- [ ] Confirm `[AllowAnonymous]` behavior still works for login/register.
- [ ] Run auth integration tests.
- [ ] Commit: `git add . && git commit -m "refactor: move identity api surface"`.

---

## Phase 7: Community Module Extraction

**Goal:** Extract posts, follows, post images, and moderation workflow.

### Task 7.1: Create Community Module Projects

**Files:**
- Create projects under `src/Modules/Community/`.

- [ ] Create module projects and references.
- [ ] Run build.
- [ ] Commit: `git add src/Modules/Community KoiFengShuiSystem.sln && git commit -m "refactor: add community module projects"`.

### Task 7.2: Move Community Domain and Services

**Files:**
- Move `Post`, `PostCategory`, `PostImage`, `Follow`.
- Move `PostService`, `AdminPostService`, `AdminPostImageService` and interfaces.
- Move post/admin post DTOs.

- [ ] Move entities and update EF context/configurations.
- [ ] Replace direct dependencies on `Account` and `Element` internals with ids first; only introduce contracts if behavior needs external data.
- [ ] Move services and register through `CommunityModuleInstaller`.
- [ ] Run community unit tests and full build.
- [ ] Commit: `git add . && git commit -m "refactor: extract community module"`.

### Task 7.3: Move Community Controllers

**Files:**
- Move `PostController`, `AdminPostController` into `Community.Api`.

- [ ] Keep route templates unchanged.
- [ ] Confirm image upload behavior for posts still works through shared image service.
- [ ] Run integration tests for post feed and post create/edit/delete authorization.
- [ ] Commit: `git add . && git commit -m "refactor: move community api surface"`.

---

## Phase 8: Marketplace Module Extraction

**Goal:** Extract listings, listing images, market categories, and subscription tiers.

### Task 8.1: Create Marketplace Module Projects

**Files:**
- Create projects under `src/Modules/Marketplace/`.

- [ ] Create module projects and references.
- [ ] Run build.
- [ ] Commit: `git add src/Modules/Marketplace KoiFengShuiSystem.sln && git commit -m "refactor: add marketplace module projects"`.

### Task 8.2: Move Marketplace Domain and Services

**Files:**
- Move `MarketplaceListing`, `MarketCategory`, `SubcriptionTier`, `ListingImage`.
- Move listing/category/tier services and DTOs.

- [ ] Move entities and update EF configurations.
- [ ] Keep typo `SubcriptionTier` until a dedicated schema-safe rename phase; do not rename table/model now.
- [ ] Move services and register through `MarketplaceModuleInstaller`.
- [ ] Keep payment behavior referenced through an interface contract, not direct `TransactionService` access.
- [ ] Run marketplace unit tests and build.
- [ ] Commit: `git add . && git commit -m "refactor: extract marketplace module"`.

### Task 8.3: Move Marketplace Controllers

**Files:**
- Move `MarketplaceListingsController`, `MarketCategoryController`, `SubcriptionTiersController` into `Marketplace.Api`.

- [ ] Preserve existing routes and response DTOs.
- [ ] Run listing search/filter contract tests.
- [ ] Commit: `git add . && git commit -m "refactor: move marketplace api surface"`.

---

## Phase 9: Payments Module Extraction

**Goal:** Isolate PayOS and transaction reconciliation.

### Task 9.1: Create Payments Module Projects

**Files:**
- Create projects under `src/Modules/Payments/`.

- [ ] Create module projects and references.
- [ ] Run build.
- [ ] Commit: `git add src/Modules/Payments KoiFengShuiSystem.sln && git commit -m "refactor: add payments module projects"`.

### Task 9.2: Introduce PayOS Abstraction

**Files:**
- Create: `src/Modules/Payments/Payments.Application/PayOS/IPayOsClient.cs`
- Create: `src/Modules/Payments/Payments.Infrastructure/PayOS/PayOsClient.cs`
- Modify transaction service tests.

- [ ] Add an interface for the exact PayOS operations used by the application.
- [ ] Wrap `Net.payOS.PayOS` in `PayOsClient`.
- [ ] Update `TransactionService` and `TransactionSyncService` to depend on `IPayOsClient`.
- [ ] Update tests to use a fake client.
- [ ] Run payments tests and full build.
- [ ] Commit: `git add . && git commit -m "refactor: abstract payos integration"`.

### Task 9.3: Move Payments Domain, Services, and Controller

**Files:**
- Move `Transaction`, `TransactionService`, `TransactionSyncService`, `TransactionController`, transaction DTOs.

- [ ] Move transaction entity and EF configuration.
- [ ] Move services and hosted service registration to `PaymentsModuleInstaller`.
- [ ] Move controller without changing routes.
- [ ] Ensure webhook endpoint remains reachable and unauthenticated only where intended.
- [ ] Run payments integration tests.
- [ ] Commit: `git add . && git commit -m "refactor: extract payments module"`.

---

## Phase 10: Admin and Notifications Extraction

**Goal:** Extract dashboard, FAQ, traffic logs, and email delivery.

### Task 10.1: Extract Notifications Module

**Files:**
- Create projects under `src/Modules/Notifications/`.
- Move `EmailService`, `MailSettings`, `MailData`.

- [ ] Create `Notifications.Application` and `Notifications.Infrastructure`.
- [ ] Add `IEmailSender` contract.
- [ ] Move current email implementation behind `IEmailSender`.
- [ ] Update Identity password reset code to depend on `IEmailSender`.
- [ ] Run identity and notification tests.
- [ ] Commit: `git add . && git commit -m "refactor: extract notifications module"`.

### Task 10.2: Extract Admin Module

**Files:**
- Create projects under `src/Modules/Admin/`.
- Move `DashboardController`, `FAQController`, `DashboardService`, `FAQService`, `FAQ`, `TrafficLog`.

- [ ] Move entities and EF configurations.
- [ ] Move services and register through `AdminModuleInstaller`.
- [ ] Move controllers without route changes.
- [ ] Keep traffic logging middleware in Host, but write logs through an Admin or Shared contract instead of directly owning admin internals.
- [ ] Run dashboard and FAQ integration tests.
- [ ] Commit: `git add . && git commit -m "refactor: extract admin module"`.

---

## Phase 11: Shared Image Infrastructure

**Goal:** Centralize Cloudinary and image storage without making it a domain dumping ground.

### Task 11.1: Move Image Infrastructure

**Files:**
- Move `Image`, `ImageService`, `CloudService`, `UploadImageController`, upload DTOs, `CloundSettings`.

- [ ] Move `Image` entity to Shared.Infrastructure only if it remains a platform-level image table.
- [ ] Move Cloudinary implementation to `Shared.Infrastructure/Files`.
- [ ] Add `IImageStorage` or equivalent minimal contract in `Shared.Kernel` only if modules need upload capability.
- [ ] Keep `UploadImageController` in Host or create `Shared.Api` only if project conventions allow it; otherwise expose module-specific upload endpoints.
- [ ] Run image upload tests with fake Cloudinary client.
- [ ] Commit: `git add . && git commit -m "refactor: centralize image infrastructure"`.

---

## Phase 12: Boundary Enforcement and Cleanup

**Goal:** Remove old projects and enforce the new architecture.

### Task 12.1: Remove Old Layered Projects

**Files:**
- Remove old code from `KoiFengShuiSystem.Api`, `KoiFengShuiSystem.Services`, `KoiFengShuiSystem.DataAccess`, `KoiFengShuiSystem.Shared`, `KoiFengShuiSystem.Common` after all code is migrated.
- Modify: `KoiFengShuiSystem.sln`

- [ ] Verify no target project references old layered projects.
- [ ] Remove old projects from solution.
- [ ] Delete old project folders only after all tests pass.
- [ ] Run `dotnet build KoiFengShuiSystem.sln --no-restore`.
- [ ] Run `dotnet test KoiFengShuiSystem.sln --no-build`.
- [ ] Commit: `git add . && git commit -m "refactor: remove legacy layered projects"`.

### Task 12.2: Add Architecture Tests

**Files:**
- Create: `tests/UnitTests/Architecture/ModuleBoundaryTests.cs`

- [ ] Add tests that fail if a module references another module's `Domain`, `Infrastructure`, or concrete `Application` implementation directly.
- [ ] Allow references only to `Shared.Kernel`, own module projects, and explicitly approved cross-module contract assemblies.
- [ ] Add tests that verify controllers live only in `*.Api` projects.
- [ ] Add tests that verify EF entity configurations live in infrastructure projects.
- [ ] Run `dotnet test tests/UnitTests/UnitTests.csproj --filter Architecture`.
- [ ] Commit: `git add tests/UnitTests/Architecture && git commit -m "test: enforce module boundaries"`.

### Task 12.3: Add CI Quality Gates

**Files:**
- Create or modify CI workflow files if this repository uses GitHub Actions, Azure Pipelines, or another CI system.

- [ ] Add restore, build, test, architecture tests, and Docker build steps.
- [ ] Fail CI on test failure.
- [ ] Ensure secrets are not required for test startup; use fake/test configuration.
- [ ] Commit: `git add . && git commit -m "ci: add refactor quality gates"`.

---

## Phase 13: Production Hardening

**Goal:** Address technical debt called out in the documentation.

### Task 13.1: CORS Lockdown

**Files:**
- Modify: `src/Host/Program.cs`
- Modify: `src/Host/appsettings*.json`
- Test: `tests/IntegrationTests/Security/CorsTests.cs`

- [ ] Add tests proving only configured origins are allowed.
- [ ] Bind allowed origins from configuration.
- [ ] Reject wildcard origins outside development.
- [ ] Run security integration tests.
- [ ] Commit: `git add . && git commit -m "security: enforce configured cors origins"`.

### Task 13.2: Rate Limiting

**Files:**
- Modify: `src/Host/Program.cs`
- Modify: `src/Host/appsettings*.json`
- Test: `tests/IntegrationTests/Security/RateLimitingTests.cs`

- [ ] Add ASP.NET Core rate limiting middleware.
- [ ] Configure stricter limits for auth and payment endpoints.
- [ ] Configure broader limits for public read endpoints.
- [ ] Add tests for `429 Too Many Requests` behavior using test settings with low limits.
- [ ] Commit: `git add . && git commit -m "security: add api rate limiting"`.

### Task 13.3: API Versioning

**Files:**
- Modify Host and module API projects.
- Test: `tests/IntegrationTests/ApiVersioningTests.cs`

- [ ] Add ASP.NET API versioning package.
- [ ] Mark existing endpoints as `v1` without changing existing route behavior unless explicitly approved.
- [ ] Update Swagger to group by API version.
- [ ] Run contract tests.
- [ ] Commit: `git add . && git commit -m "feat: add api versioning"`.

### Task 13.4: Refresh Tokens

**Files:**
- Modify Identity domain/application/infrastructure/API.
- Create EF migration for refresh token persistence if required.

- [ ] Add refresh token entity/table under Identity.
- [ ] Add token rotation and revocation behavior.
- [ ] Add tests for refresh, reuse detection, expiration, and logout revocation.
- [ ] Run migration generation and review schema diff.
- [ ] Run full test suite.
- [ ] Commit: `git add . && git commit -m "feat: add refresh token support"`.

---

## Phase 14: Advanced Feng Shui Roadmap Enablement

**Goal:** Prepare the architecture for Ba Zi, Five Elements interactions, Zodiac, Flying Stars, and dashboard features without building all of them during the refactor.

### Task 14.1: Five Elements Interaction Model

**Files:**
- Create inside `FengShui.Domain` and `FengShui.Application`.

- [ ] Add generating and controlling cycle domain model.
- [ ] Add unit tests for all five generating relationships.
- [ ] Add unit tests for all five controlling relationships.
- [ ] Expose application service only if current compatibility/recommendation logic consumes it.
- [ ] Commit: `git add . && git commit -m "feat: model five element interactions"`.

### Task 14.2: Ba Zi Engine Planning Spike

**Files:**
- Create: `docs/architecture/bazi-engine-design.md`

- [ ] Define stem, branch, pillar, chart, day master, and element balance types.
- [ ] Decide calendar conversion source and licensing constraints.
- [ ] Define database tables only if persisted profiles are needed.
- [ ] Do not implement production Ba Zi calculations until the design doc is reviewed.
- [ ] Commit: `git add docs/architecture/bazi-engine-design.md && git commit -m "docs: design bazi engine"`.

---

## Final Verification Checklist

- [ ] `dotnet restore KoiFengShuiSystem.sln` passes.
- [ ] `dotnet build KoiFengShuiSystem.sln --no-restore` passes.
- [ ] `dotnet test KoiFengShuiSystem.sln --no-build` passes.
- [ ] Docker image builds from the new `src/Host` project.
- [ ] Swagger exposes the same public API routes unless an approved versioning change was made.
- [ ] No module references another module's internal `Domain` or `Infrastructure` project directly.
- [ ] No service depends directly on PayOS, Cloudinary, or SMTP concrete clients outside infrastructure projects.
- [ ] No production startup path requires test or development secrets.
- [ ] `KoiFengShuiSystem_Documentation.md` is updated to match the final project structure.

---

## Recommended Execution Order

Execute phases in order. Do not start Phase 5 module extraction before Phases 0 through 4 are complete and green. After Phase 5 succeeds, each module extraction can be done in this order: Identity, Community, Marketplace, Payments, Notifications, Admin, Shared Image Infrastructure.

The safest branch strategy is one branch per phase:

- `refactor/phase-0-baseline`
- `refactor/phase-1-shared-foundations`
- `refactor/phase-2-tests`
- `refactor/phase-3-persistence`
- `refactor/phase-4-host`
- `refactor/phase-5-fengshui`
- `refactor/phase-6-identity`
- `refactor/phase-7-community`
- `refactor/phase-8-marketplace`
- `refactor/phase-9-payments`
- `refactor/phase-10-admin-notifications`
- `refactor/phase-11-shared-images`
- `refactor/phase-12-cleanup-boundaries`
- `refactor/phase-13-hardening`
- `refactor/phase-14-roadmap-enablement`

Merge each phase only after build, tests, and API smoke tests pass.
