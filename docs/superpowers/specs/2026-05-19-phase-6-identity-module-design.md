# Phase 6 Identity Module Extraction Design

## Goal

Extract the Identity bounded context into module projects in smaller, verifiable slices while preserving existing API routes, JWT behavior, password/security behavior, EF schema, and test coverage.

Identity scope for this phase is limited to authentication, accounts, roles, JWT support, and account administration. Notification/email delivery remains outside Identity for now, even where Identity workflows call it.

## Current Context

Phase 5 extracted the FengShui bounded context into `src/Modules/FengShui/` and introduced module-level controllers, services, infrastructure adapters, and installer registration. The current working tree is clean. Recent commits include Phase 5 (`158f6d4`) and a follow-up Docker/documentation/error-handling commit (`4f94562`).

The master refactor plan identifies Phase 6 as Identity Module Extraction. Identity-related code currently lives across legacy projects:

- Controllers: `AuthController`, `AccountController`
- Domain models: `Account`, `Role`
- Services/helpers: `AccountService`, `AdminAccountService`, `JwtUtils`, `IJwtUtils`
- Shared dependencies: `EmailService`, `MailSettings`, request/response DTOs, `BusinessResult`, EF context/configurations

## Non-Goals

- Do not extract Notifications or email infrastructure in Phase 6.
- Do not redesign authentication flows or JWT claims.
- Do not change public API routes, request bodies, response shapes, status-code behavior, or authorization attributes intentionally.
- Do not intentionally change the database schema; use temporary migrations to prove there is no schema drift.
- Do not split `KoiFengShuiContext`; keep the shared DbContext.
- Do not introduce MediatR or a new validation framework in this phase.

## Design Approach

Use thin vertical checkpoints instead of a large one-shot extraction. Each sub-phase must compile, and behavior-moving sub-phases must run targeted tests before proceeding.

### Phase 6A: Identity Baseline and Contract Tests

Create or strengthen tests around Identity behavior before moving code:

- Register/login happy path and expected failure cases.
- JWT-protected endpoint access with valid/invalid/missing tokens.
- Profile/account read/update behavior that currently exists.
- Admin account behavior where covered by existing APIs.
- Swagger route presence for Identity endpoints after controller move.

The objective is to lock down public behavior and reduce risk during namespace/project movement.

### Phase 6B: Identity Module Skeleton

Create module projects under `src/Modules/Identity/`:

- `Identity.Domain`
- `Identity.Application`
- `Identity.Infrastructure`
- `Identity.Api`

References should mirror the FengShui pattern:

- Domain references `Shared.Kernel` only if needed during transition.
- Application references Domain and `Shared.Kernel`.
- Infrastructure references Application, Domain, and `Shared.Infrastructure`.
- Api references Application and Infrastructure.
- Host references Identity.Api/Infrastructure only for controller discovery and installer scanning.

Add an `IdentityModuleInstaller` shell, but do not remove legacy registrations until replacement registrations are active and verified.

### Phase 6C: Identity Domain Move

Move `Account` and `Role` into `Identity.Domain/Entities` and update namespaces.

Preserve existing table names, columns, keys, FK names, and relationship behavior. Any cross-module navigation cycles should be handled like Phase 5: preserve FK scalar properties and remove or convert only the navigation properties required to break project cycles.

Known cross-boundary considerations:

- `Account` currently relates to FengShui via `ElementId` and previously had FengShui navigation removed during Phase 5.
- `Account` may be referenced by community, marketplace, transactions, FAQ/admin, or image-related entities/services.
- `Role` is Identity-owned but may be queried by auth/account flows.

The DbContext remains in `Shared.Infrastructure`; it should reference Identity.Domain for `DbSet<Account>` and `DbSet<Role>`.

### Phase 6D: Identity Application Extraction

Move Identity contracts and application behavior into `Identity.Application`:

- `IAccountService`
- Account/auth-related request/response DTOs that are only Identity-owned
- `AccountService`
- `AdminAccountService` if it only manages accounts and roles

Application code should avoid direct EF/repository dependencies where feasible. Introduce application ports for persistence and external dependencies when needed, for example:

- `IIdentityReadStore`
- `IIdentityWriteStore`
- `IJwtTokenService`
- `IIdentityEmailSender` or a narrow notification port only if the existing registration/password flow requires email without pulling Notifications into Identity

If a full port split is too large, keep the first checkpoint minimal: move interfaces/DTOs first, then replace concrete repositories with ports in the next checkpoint.

### Phase 6E: Identity Infrastructure Extraction

Move Identity infrastructure concerns into `Identity.Infrastructure`:

- EF-backed identity read/write store adapters.
- JWT token generation implementation currently in `JwtUtils`.
- Option binding for `AppSettings` if JWT secret access is Identity-specific.
- Identity DI registrations through `IdentityModuleInstaller`.

Password hashing/security helpers should move only if they are Identity-specific and do not create new coupling. Shared cryptographic primitives can remain where they are temporarily if moving them would broaden scope.

Email remains external. Identity should depend on a narrow email/notification port if needed, implemented by existing legacy `EmailService` during this phase.

### Phase 6F: Identity API Move

Move controllers into `Identity.Api`:

- `AuthController`
- `AccountController`

Controller namespaces change to `KoiFengShuiSystem.Modules.Identity.Api.Controllers`. Route attributes, action names, authorization attributes, request models, and response shapes must remain unchanged.

Host controller discovery adds the Identity.Api assembly through `AddApplicationPart`. After the moved controllers are active and tests pass, remove old controller files and old Identity manual registrations from Host.

### Phase 6G: Cleanup and Boundary Verification

After behavior is module-owned:

- Remove obsolete legacy Identity registrations.
- Remove unused old Identity interfaces/implementations if no consumers remain.
- Update docs that describe current dependencies and refactor baseline.
- Run full build and tests.
- Verify no EF schema drift with a temporary migration and remove it.
- Review project references to ensure `Identity.Application` does not reference legacy DataAccess directly.

## Component Responsibilities

- `Identity.Domain`: owns `Account`, `Role`, and Identity-only domain relationships.
- `Identity.Application`: owns Identity DTOs, service contracts, account/auth use cases, and application ports.
- `Identity.Infrastructure`: owns EF-backed identity persistence, JWT implementation, option binding, and module installer registration.
- `Identity.Api`: owns Identity controllers and route surface.
- `Host`: discovers Identity controllers/installers and remains the composition root.
- `Shared.Infrastructure`: keeps the shared `KoiFengShuiContext` and EF configurations during this phase.
- `Notifications/legacy email`: remains outside Identity and is consumed through a narrow port if required.

## Data Flow

Authentication request flow after extraction:

1. `Identity.Api` controller receives the existing request DTO.
2. Controller calls an `Identity.Application` service contract.
3. Application service uses Identity persistence ports and token/email ports.
4. Infrastructure adapters use `KoiFengShuiContext`, JWT settings, and existing email implementation as needed.
5. Controller returns the same response shape/status behavior as before.

Protected request flow remains unchanged: JWT Bearer authentication and existing middleware continue to run in Host.

## Error Handling and Authorization

- Preserve existing exception handling and status-code behavior unless a test reveals current behavior is already broken.
- Preserve `[Authorize]`, `[AllowAnonymous]`, role checks, and token parsing semantics exactly.
- Keep Host middleware (`ExceptionMiddleware`, `JwtMiddleware`, `TrafficLoggingMiddleware`) in Host unless a later cleanup phase moves cross-cutting behavior.

## Testing and Verification

Every sub-phase should run the smallest useful verification set first, then the full suite at major boundaries:

- Baseline targeted auth/account tests before movement.
- Build after project skeleton and entity move.
- Unit tests after service extraction.
- Integration tests after API move.
- Full `dotnet test` after final cleanup.
- Temporary EF migration after domain/context changes and at the end to prove empty `Up()`/`Down()` methods.

## Risks and Mitigations

- **High Account fan-out:** `Account` is referenced by many modules. Mitigation: move domain first with FK preservation and compile-check all downstream references before service movement.
- **JWT behavior regression:** token generation and middleware are security-sensitive. Mitigation: add contract tests before moving JWT implementation.
- **Email coupling:** registration/password flows may require email. Mitigation: keep Notifications out of scope and bridge through a narrow port or temporary adapter.
- **Schema drift:** entity namespace/project moves can change EF snapshots. Mitigation: run temporary migration checks and require empty `Up()`/`Down()`.
- **Route ambiguity:** moving controllers without deleting old ones can duplicate routes. Mitigation: add Identity.Api application part, then remove legacy controllers in the same checkpoint after build confirmation.

## Success Criteria

- Identity module projects exist and are in the solution.
- `Account` and `Role` are owned by `Identity.Domain`.
- Identity service contracts and implementation are module-owned or explicitly bridged through temporary ports.
- `AuthController` and `AccountController` are served from `Identity.Api` with unchanged public routes.
- Host discovers Identity module installer and controllers.
- Existing public API behavior is preserved.
- Full build and tests pass.
- Temporary EF migration check confirms no schema drift.
- Legacy Identity registrations/files are removed only when no longer used.
