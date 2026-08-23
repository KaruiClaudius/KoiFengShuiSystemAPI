# Phase 6 Identity Finish Design

## Goal

Finish the remaining Phase 6 Identity bounded-context extraction in small, verifiable slices while preserving public API behavior, JWT semantics, email behavior, EF schema, and existing test results.

## Current Context

Phase 6 is already in progress. The module projects exist, `Account` and `Role` now live in `Identity.Domain`, Identity DTOs and account services live in `Identity.Application`, and EF-backed read/write adapters plus temporary JWT/email adapters exist in `Identity.Infrastructure`.

The remaining work is not a new phase. It is the completion pass for Phase 6, focused on removing temporary legacy wiring and moving the Identity API surface into the Identity module.

## Non-Goals

- Do not start Phase 7 or extract another bounded context.
- Do not redesign auth flows, JWT claims, password behavior, Google login behavior, or email templates.
- Do not change public Identity routes, request/response shapes, status-code behavior, or authorization attributes intentionally.
- Do not change the database schema.
- Do not split `KoiFengShuiContext`.
- Do not modify `KoiFengShuiSystem_Documentation.md`.
- Do not commit unless explicitly requested.

## Approved Approach

Use a conservative checkpointed finish:

1. Complete Identity infrastructure ownership for DI and JWT behavior.
2. Remove the temporary direct application dependency on legacy DataAccess/FengShui domain through a narrow port.
3. Move Identity controllers to `Identity.Api` while preserving the exact API surface.
4. Run full verification and a final schema-drift check.

## Slice 1: Identity Infrastructure Boundary

Add `IdentityModuleInstaller` in `Identity.Infrastructure` and register Identity-owned services there:

- `IIdentityReadStore` -> `EfIdentityReadStore`
- `IIdentityWriteStore` -> `EfIdentityWriteStore`
- `IJwtTokenService` -> `JwtTokenService`
- `IIdentityEmailSender` -> `LegacyIdentityEmailSender`
- `IAccountService` -> `AccountService`
- `AdminAccountService`

Host and the legacy API startup should discover this installer before manual Identity registrations are removed. Manual registrations can be deleted only after build/test verification confirms the installer is active.

Move JWT token generation and validation into `Identity.Infrastructure.Security.JwtTokenService`. Extend the existing `IJwtTokenService` application port with validation so `AuthController` and both JWT middlewares no longer depend on legacy `IJwtUtils`.

`JwtTokenService` must preserve the existing behavior:

- `id` claim contains `AccountId`.
- `ClaimTypes.Email` contains account email.
- `ClaimTypes.Role` contains role id.
- Tokens expire after seven days.
- Validation uses the same `AppSettings:Secret` and zero clock skew.
- Invalid tokens return `null` rather than throwing to middleware callers.

## Slice 2: Application Dependency Cleanup

`Identity.Application.Services.AccountService` currently depends on `GenericRepository<Element>` and `FengShui.Domain.Entities.Element` for element lookup. Replace that with a narrow Identity application port, for example `IIdentityElementLookup`, with methods that return only the data AccountService needs.

Implement the port in `Identity.Infrastructure` using the shared EF context. This keeps the Cung Phi calculation behavior unchanged while removing these application-layer references:

- `KoiFengShuiSystem.DataAccess`
- `FengShui.Domain`

This slice intentionally keeps the duplicate calculation in `AccountService` for now, with a documented follow-up to replace it through a shared FengShui calculator later.

## Slice 3: Identity API Move

Move the two Identity controllers into `src/Modules/Identity/Identity.Api/Controllers`:

- `AuthController`
- `AccountController`

The controller namespace changes to `KoiFengShuiSystem.Modules.Identity.Api.Controllers`, but the API contract must stay unchanged:

- `[Route("api/[controller]")]` remains.
- `[Authorize]` and `[AllowAnonymous]` behavior remains.
- Existing action names, HTTP verbs, and route templates remain.
- Existing request and response DTOs remain from `Identity.Application`.

Host controller discovery should add the `Identity.Api` assembly. The old controller files in `KoiFengShuiSystem.Api` should be removed in the same checkpoint to avoid duplicate routes.

## Slice 4: Final Verification And Documentation

Run verification sequentially to avoid stale `--no-build` results:

1. `dotnet restore KoiFengShuiSystem.sln`
2. `dotnet build KoiFengShuiSystem.sln --no-restore`
3. `dotnet test KoiFengShuiSystem.sln --no-build`
4. Temporary EF migration check for no schema drift.
5. Remove the temporary migration.

Update only architecture/refactor documentation that tracks the current module boundary. Do not edit `KoiFengShuiSystem_Documentation.md`.

## Success Criteria

- Identity services are registered through `IdentityModuleInstaller`.
- Legacy `IJwtUtils` is no longer needed by Identity controllers or middleware.
- `Identity.Application` no longer references `KoiFengShuiSystem.DataAccess` or `FengShui.Domain`.
- `AuthController` and `AccountController` are served from `Identity.Api` with unchanged routes.
- No duplicate Identity routes exist.
- Full build and test suite pass.
- Temporary EF migration check confirms no schema drift.
- `KoiFengShuiSystem_Documentation.md` remains untouched.

## Confidence

High. The remaining work follows the existing Phase 6 design and the current code already has most ports/adapters in place. The main risks are duplicate controller discovery, missing DI registrations after installer migration, and accidental JWT behavior changes.
