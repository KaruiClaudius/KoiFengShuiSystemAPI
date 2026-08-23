# Phase 6 Identity Module Extraction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract the Identity bounded context into module projects in smaller, verifiable slices while preserving existing API routes, JWT behavior, password/security behavior, EF schema, and test coverage.

**Architecture:** Create `Identity.Domain`, `Identity.Application`, `Identity.Infrastructure`, and `Identity.Api` projects under `src/Modules/Identity/`. Move `Account` and `Role` entities, `AccountService`, `AdminAccountService`, `JwtUtils`, auth controllers, and related DTOs incrementally. Use application-level ports for persistence access, keep the shared `KoiFengShuiContext` as the single EF context, and keep EF schema stable. Because `Account` has cross-module navigation cycles, remove or convert only the cross-module navigation properties needed to break project cycles while preserving FK columns and unidirectional EF relationships.

**Tech Stack:** .NET 8, ASP.NET Core Controllers, EF Core SQL Server, xUnit, FluentAssertions, Swagger/OpenAPI, JWT Bearer, existing module installer contract, existing shared `KoiFengShuiContext`.

**Confidence:** medium-high. Phase 5 established the extraction pattern (entities → services → infrastructure → controllers → installer). Identity is more complex because `Account` is referenced by many other modules and services, and JWT behavior is security-sensitive. The main risk is downstream compile errors from namespace changes and cross-module references.

---

## Current Verified State

- `dotnet build KoiFengShuiSystem.sln --no-restore` passes with `0 Error(s)`.
- `dotnet test KoiFengShuiSystem.sln --no-build` passes with `91/91` unit tests and `4/4` integration tests.
- Current working tree is clean after Phase 5 commit (`158f6d4`) and follow-up commit (`4f94562`).
- Do not commit unless explicitly requested by the user.
- Do not edit or revert `KoiFengShuiSystem_Documentation.md`.

---

## Scope Decisions

- Preserve these API routes exactly:
  - `POST /api/Auth/SignIn`
  - `POST /api/Auth/SignUp`
  - `POST /api/Auth/ForgotPassword`
  - `POST /api/Auth/google-login`
  - `GET /api/Account`
  - `GET /api/Account/{id}`
  - `PUT /api/Account/{id}`
  - `DELETE /api/Account/{id}`
  - `GET /api/Account/email/{email}`
  - `PUT /api/Account/{id}/change-password`
  - `POST /api/Account/UpdateWalletAfterPosted`
- Keep the existing database schema stable.
- Keep migrations in `KoiFengShuiSystem.DataAccess/Migrations`.
- Keep `KoiFengShuiContext` in `src/Shared/Shared.Infrastructure/Persistence/KoiFengShuiContext.cs`.
- Keep controllers in the old API assembly until the new Identity API project is wired into Host, then move only the two Identity controllers.
- Use module installer registration for Identity services; remove Identity manual registrations from Host after module installer verification.
- Avoid moving unrelated DTOs, services, controllers, or documentation.
- Email/Notifications remains outside Identity scope; bridge through existing `EmailService` temporarily.

---

## File Structure Map

### New Projects

- `src/Modules/Identity/Identity.Domain/Identity.Domain.csproj`
  - Owns `Account` and `Role` entities after Task 6.2.
  - May reference `Shared.Kernel` temporarily for cross-module entity references during transition.
- `src/Modules/Identity/Identity.Application/Identity.Application.csproj`
  - Owns service contracts, request/response DTOs, business services, and persistence ports.
- `src/Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj`
  - Owns EF-backed query/store adapters, JWT implementation, and `IdentityModuleInstaller`.
- `src/Modules/Identity/Identity.Api/Identity.Api.csproj`
  - Owns Identity controllers after Task 6.5.

### Files To Move Or Modify

- Move entities from `src/Shared/Shared.Kernel/Models/` to `src/Modules/Identity/Identity.Domain/Entities/`:
  - `Account.cs`
  - `Role.cs`
- Move services:
  - `KoiFengShuiSystem.Services/Services/Interface/IAccountService.cs`
  - `KoiFengShuiSystem.Services/Services/Implement/AccountService.cs`
  - `KoiFengShuiSystem.Services/Services/Implement/AdminAccountService.cs`
- Move JWT utilities:
  - `KoiFengShuiSystem.Shared/Helpers/JwtUtils.cs`
  - `KoiFengShuiSystem.Shared/Helpers/IJwtUtils.cs`
- Move controllers:
  - `KoiFengShuiSystem.Api/Controllers/AuthController.cs`
  - `KoiFengShuiSystem.Api/Controllers/AccountController.cs`
- Move DTOs (Identity-owned only):
  - `KoiFengShuiSystem.Shared/Models/Request/AuthenticateRequest.cs`
  - `KoiFengShuiSystem.Shared/Models/Request/RegisterRequest.cs`
  - `KoiFengShuiSystem.Shared/Models/Request/ForgotPasswordRequest.cs`
  - `KoiFengShuiSystem.Shared/Models/Request/UpdateRequest.cs`
  - `KoiFengShuiSystem.Shared/Models/Request/GoogleLoginRequest.cs`
  - `KoiFengShuiSystem.Shared/Models/Request/ChangePasswordRequest.cs`
  - `KoiFengShuiSystem.Shared/Models/Response/AuthenticateResponse.cs`
  - `KoiFengShuiSystem.Shared/Models/Response/AccountResponse.cs`
  - `KoiFengShuiSystem.Shared/Models/Response/AuthenticationResult.cs`
- Modify shared persistence:
  - `src/Shared/Shared.Infrastructure/Persistence/KoiFengShuiContext.cs`
  - `src/Shared/Shared.Infrastructure/Persistence/Configurations/AccountConfiguration.cs`
  - `src/Shared/Shared.Infrastructure/Persistence/Configurations/RoleConfiguration.cs` (if exists)
  - `src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj`
- Modify Host:
  - `src/Host/Program.cs`
  - `src/Host/Host.csproj`
- Modify tests:
  - `tests/UnitTests/Identity/*.cs`
  - `tests/IntegrationTests/AppBootstrapTests.cs`
  - Add `tests/IntegrationTests/IdentityApiContractTests.cs`

---

## Task 6.0: Preflight And Boundary Audit

**Files:** No code changes expected.

- [ ] Run `git status --short`.

Expected: clean working tree.

- [ ] Run the current solution build.

```powershell
dotnet build KoiFengShuiSystem.sln --no-restore
```

Expected: exit 0, existing package vulnerability warnings only.

- [ ] Run the current solution tests.

```powershell
dotnet test KoiFengShuiSystem.sln --no-build
```

Expected: `91/91` unit tests pass and `4/4` integration tests pass.

- [ ] Audit entity cross-navigation before moving entities.

```powershell
rg "Account|Role" src/Shared/Shared.Kernel/Models KoiFengShuiSystem.Services KoiFengShuiSystem.Api tests
```

Expected: identify all references that must be updated when namespaces change. Note: `Account` has navigation collections to `FAQ`, `Follow`, `MarketplaceListing`, `Post`, `TrafficLog`, `Transaction` from other modules. These create cross-module cycles that must be broken.

- [ ] Audit old Identity service registrations in Host.

```powershell
rg "IAccountService|AccountService|AdminAccountService|IJwtUtils|JwtUtils" src/Host/Program.cs
```

Expected: Host still manually registers Identity services before module installer migration.

- [ ] Audit `Account` cross-module navigation properties.

```powershell
rg "ICollection<Account>|Account\?|Account " src/Shared/Shared.Kernel/Models src/Shared/Shared.Infrastructure/Persistence/Configurations
```

Expected: identify which other entities reference `Account` via navigation properties. These will need unidirectional mapping treatment similar to Phase 5.

---

## Task 6.1: Create Identity Module Projects

**Files:**
- Create: `src/Modules/Identity/Identity.Domain/Identity.Domain.csproj`
- Create: `src/Modules/Identity/Identity.Application/Identity.Application.csproj`
- Create: `src/Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj`
- Create: `src/Modules/Identity/Identity.Api/Identity.Api.csproj`
- Modify: `KoiFengShuiSystem.sln`

- [ ] Create the module directories.

```powershell
New-Item -ItemType Directory -Path "src/Modules/Identity/Identity.Domain" -Force
New-Item -ItemType Directory -Path "src/Modules/Identity/Identity.Application" -Force
New-Item -ItemType Directory -Path "src/Modules/Identity/Identity.Infrastructure" -Force
New-Item -ItemType Directory -Path "src/Modules/Identity/Identity.Api" -Force
```

- [ ] Create `Identity.Domain.csproj`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Shared\Shared.Kernel\Shared.Kernel.csproj" />
  </ItemGroup>
</Project>
```

- [ ] Create `Identity.Application.csproj`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Identity.Domain\Identity.Domain.csproj" />
    <ProjectReference Include="..\..\..\Shared\Shared.Kernel\Shared.Kernel.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.2" />
  </ItemGroup>
</Project>
```

- [ ] Create `Identity.Infrastructure.csproj`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Identity.Application\Identity.Application.csproj" />
    <ProjectReference Include="..\Identity.Domain\Identity.Domain.csproj" />
    <ProjectReference Include="..\..\..\Shared\Shared.Infrastructure\Shared.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

- [ ] Create `Identity.Api.csproj`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Identity.Application\Identity.Application.csproj" />
    <ProjectReference Include="..\Identity.Infrastructure\Identity.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

- [ ] Add all four projects to the solution.

```powershell
dotnet sln KoiFengShuiSystem.sln add src/Modules/Identity/Identity.Domain/Identity.Domain.csproj
dotnet sln KoiFengShuiSystem.sln add src/Modules/Identity/Identity.Application/Identity.Application.csproj
dotnet sln KoiFengShuiSystem.sln add src/Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj
dotnet sln KoiFengShuiSystem.sln add src/Modules/Identity/Identity.Api/Identity.Api.csproj
```

- [ ] Restore and build.

```powershell
dotnet restore KoiFengShuiSystem.sln
dotnet build KoiFengShuiSystem.sln --no-restore
```

Expected: build exit 0.

Checkpoint commit message if explicitly requested later: `refactor(identity): add module project skeleton`.

---

## Task 6.2: Move Identity Domain Entities Without Schema Drift

**Files:**
- Move: `src/Shared/Shared.Kernel/Models/Account.cs`
- Move: `src/Shared/Shared.Kernel/Models/Role.cs`
- Target: `src/Modules/Identity/Identity.Domain/Entities/*.cs`
- Modify: `src/Shared/Shared.Kernel/Models/*.cs` (all files that reference `Account` or `Role` navigation properties)
- Modify: `src/Shared/Shared.Infrastructure/Persistence/KoiFengShuiContext.cs`
- Modify: `src/Shared/Shared.Infrastructure/Persistence/Configurations/AccountConfiguration.cs`
- Modify: `src/Shared/Shared.Infrastructure/Persistence/Configurations/RoleConfiguration.cs` (if exists)

- [ ] Add a project reference from `Shared.Infrastructure` to `Identity.Domain` so the shared context can expose Identity `DbSet<T>` properties.

```xml
<ProjectReference Include="..\..\Modules\Identity\Identity.Domain\Identity.Domain.csproj" />
```

- [ ] Move `Account.cs` and `Role.cs` into `src/Modules/Identity/Identity.Domain/Entities/`.

- [ ] Update entity namespaces to `KoiFengShuiSystem.Modules.Identity.Domain.Entities`.

Example for `Account.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoiFengShuiSystem.Modules.Identity.Domain.Entities;

public class Account
{
    [Key]
    public int AccountId { get; set; }

    [Required]
    [MaxLength(50)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Password { get; set; }

    public DateTime? Dob { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(10)]
    public string? Gender { get; set; }

    public int? ElementId { get; set; }

    public int? RoleId { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime UpdateAt { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal? Wallet { get; set; }

    public virtual Role? Role { get; set; }
}
```

Note: Remove all cross-module navigation collections (`FAQs`, `Follows`, `MarketplaceListings`, `Posts`, `TrafficLogs`, `Transactions`) from `Account` to break project cycles. FK columns on those entities must be preserved.

- [ ] Break project cycles by removing direct Identity navigation property dependencies from remaining `Shared.Kernel` entity classes while preserving FK id properties.

Expected edits for each entity that references `Account`:

```csharp
// src/Shared/Shared.Kernel/Models/FAQ.cs
// Keep: public int? AccountId { get; set; }
// Remove: public virtual Account? Account { get; set; }

// src/Shared/Shared.Kernel/Models/Follow.cs
// Keep: FK properties
// Remove: public virtual Account? Account { get; set; }

// src/Shared/Shared.Kernel/Models/MarketplaceListing.cs
// Keep: public int? AccountId { get; set; }
// Remove: public virtual Account? Account { get; set; }

// src/Shared/Shared.Kernel/Models/Post.cs
// Keep: public int? AccountId { get; set; }
// Remove: public virtual Account? Account { get; set; }

// src/Shared/Shared.Kernel/Models/TrafficLog.cs
// Keep: FK properties
// Remove: public virtual Account? Account { get; set; }

// src/Shared/Shared.Kernel/Models/Transaction.cs
// Keep: FK properties
// Remove: public virtual Account? Account { get; set; }
```

- [ ] Update `KoiFengShuiContext.cs` usings.

```csharp
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
```

- [ ] Update EF configuration usings and relationship mappings.

For `AccountConfiguration.cs`:
- Update namespace imports
- Change `HasOne(a => a.Role)` to unidirectional if `Role` nav was removed from other side
- Remove `HasMany(a => a.FAQs)`, `HasMany(a => a.Follows)`, `HasMany(a => a.MarketplaceListings)`, `HasMany(a => a.Posts)`, `HasMany(a => a.TrafficLogs)`, `HasMany(a => a.Transactions)` — these relationships will be configured from the other side using unidirectional `HasOne<Account>()`

For each entity configuration file that previously configured a relationship to `Account`:
- Use unidirectional mappings: `builder.HasOne<Account>().WithMany().HasForeignKey(d => d.AccountId)`

- [ ] Build after entity move.

```powershell
dotnet build KoiFengShuiSystem.sln --no-restore
```

Expected: compile errors identify missed namespace imports only. Fix only Identity-related imports/references.

- [ ] Update downstream services that reference `Account` type.

Services that use `Account` directly will need updated imports:
- `AccountService.cs` — update `using KoiFengShuiSystem.DataAccess.Models;` to new namespace for `Account`
- `AdminAccountService.cs` — same
- `PostService.cs`, `MarketplaceListingService.cs`, `FAQService.cs`, `AdminPostService.cs`, `TransactionService.cs`, `DashboardService.cs` — update imports for `Account` type
- All test files that reference `Account` — update imports

- [ ] Run unit tests.

```powershell
dotnet test tests/UnitTests/UnitTests.csproj --no-build
```

Expected: all unit tests pass.

- [ ] Verify no EF schema drift with a temporary migration.

```powershell
dotnet ef migrations add VerifyIdentityEntityMoveNoSchemaChange --project KoiFengShuiSystem.DataAccess --startup-project src/Host
```

Expected: generated migration `Up()` and `Down()` methods are empty.

- [ ] Remove the temporary migration.

```powershell
dotnet ef migrations remove --project KoiFengShuiSystem.DataAccess --startup-project src/Host
```

Expected: temporary migration removed; model snapshot remains only with legitimate namespace/context updates.

Checkpoint commit message if explicitly requested later: `refactor(identity): move domain entities`.

---

## Task 6.3: Move Calculator, DTOs, Service Contracts, And Application Logic

**Files:**
- Create: `src/Modules/Identity/Identity.Application/Requests/AuthenticateRequest.cs`
- Create: `src/Modules/Identity/Identity.Application/Requests/RegisterRequest.cs`
- Create: `src/Modules/Identity/Identity.Application/Requests/ForgotPasswordRequest.cs`
- Create: `src/Modules/Identity/Identity.Application/Requests/UpdateRequest.cs`
- Create: `src/Modules/Identity/Identity.Application/Requests/GoogleLoginRequest.cs`
- Create: `src/Modules/Identity/Identity.Application/Requests/ChangePasswordRequest.cs`
- Create: `src/Modules/Identity/Identity.Application/Responses/AuthenticateResponse.cs`
- Create: `src/Modules/Identity/Identity.Application/Responses/AccountResponse.cs`
- Create: `src/Modules/Identity/Identity.Application/Responses/AuthenticationResult.cs`
- Create: `src/Modules/Identity/Identity.Application/Services/IAccountService.cs`
- Create: `src/Modules/Identity/Identity.Application/Services/AccountService.cs`
- Create: `src/Modules/Identity/Identity.Application/Services/AdminAccountService.cs`
- Create: `src/Modules/Identity/Identity.Application/Abstractions/IIdentityReadStore.cs`
- Create: `src/Modules/Identity/Identity.Application/Abstractions/IIdentityWriteStore.cs`
- Create: `src/Modules/Identity/Identity.Application/Abstractions/IJwtTokenService.cs`
- Create: `src/Modules/Identity/Identity.Application/Abstractions/IIdentityEmailSender.cs`
- Modify: `src/Host/Program.cs`
- Modify: `tests/UnitTests/Identity/*.cs`

- [ ] Move Identity DTOs into Application namespaces.

```csharp
namespace KoiFengShuiSystem.Modules.Identity.Application.Requests;
namespace KoiFengShuiSystem.Modules.Identity.Application.Responses;
```

- [ ] Create the persistence port `IIdentityReadStore`.

```csharp
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IIdentityReadStore
{
    Task<Account?> GetAccountByEmailAsync(string email);
    Task<Account?> GetAccountByIdAsync(int accountId);
    Task<IReadOnlyList<Account>> GetAllAccountsAsync();
    Task<Role?> GetRoleByIdAsync(int roleId);
}
```

- [ ] Create the persistence port `IIdentityWriteStore`.

```csharp
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IIdentityWriteStore
{
    Task<Account> CreateAccountAsync(Account account);
    Task UpdateAccountAsync(Account account);
    Task DeleteAccountAsync(Account account);
    Task<int> SaveChangesAsync();
}
```

- [ ] Create the JWT port `IJwtTokenService`.

```csharp
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateJwtToken(Account account);
}
```

- [ ] Create the email port `IIdentityEmailSender`.

```csharp
namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IIdentityEmailSender
{
    Task<bool> SendPasswordResetEmailAsync(string email, string fullName, string newPassword);
    Task<bool> SendDefaultPasswordAsync(string email, string fullName, string defaultPassword);
}
```

- [ ] Move the service interface to Application namespace.

```csharp
namespace KoiFengShuiSystem.Modules.Identity.Application.Services;
```

- [ ] Move `AccountService` to Application and replace `GenericRepository<T>` / `UnitOfWorkRepository` dependencies with `IIdentityReadStore`, `IIdentityWriteStore`, `IJwtTokenService`, and `IIdentityEmailSender`.

Note: `AccountService` contains a duplicate `CalculateElement` method (Cung Phi calculation). This duplicates `CungPhiCalculator` from FengShui.Application. Replace with a call to the FengShui calculator or extract a shared element calculation utility. For Phase 6, keep the duplicate but add a TODO comment for later cleanup.

- [ ] Move `AdminAccountService` to Application. It depends on `IAccountService` and `IConfiguration`.

- [ ] Update controllers in the old API project to use new namespaces temporarily (they will be moved in Task 6.5).

- [ ] Update test files to use new namespaces.

- [ ] Run Identity tests.

```powershell
dotnet test tests/UnitTests/UnitTests.csproj --no-build --filter "FullyQualifiedName~Identity"
```

Expected: all Identity tests pass.

- [ ] Run all tests.

```powershell
dotnet test KoiFengShuiSystem.sln --no-build
```

Expected: all unit and integration tests pass.

Checkpoint commit message if explicitly requested later: `refactor(identity): move application services`.

---

## Task 6.4: Identity Infrastructure Extraction

**Files:**
- Create: `src/Modules/Identity/Identity.Infrastructure/Persistence/EfIdentityReadStore.cs`
- Create: `src/Modules/Identity/Identity.Infrastructure/Persistence/EfIdentityWriteStore.cs`
- Create: `src/Modules/Identity/Identity.Infrastructure/Security/JwtTokenService.cs`
- Create: `src/Modules/Identity/Identity.Infrastructure/Email/LegacyIdentityEmailSender.cs`
- Create: `src/Modules/Identity/Identity.Infrastructure/IdentityModuleInstaller.cs`
- Modify: `src/Host/Program.cs`
- Modify: `src/Host/Host.csproj`

- [ ] Implement `EfIdentityReadStore` in Infrastructure.

```csharp
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;

public class EfIdentityReadStore : IIdentityReadStore
{
    private readonly KoiFengShuiContext _context;

    public EfIdentityReadStore(KoiFengShuiContext context) => _context = context;

    public Task<Account?> GetAccountByEmailAsync(string email) =>
        _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);

    public Task<Account?> GetAccountByIdAsync(int accountId) =>
        _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId);

    public async Task<IReadOnlyList<Account>> GetAllAccountsAsync() =>
        await _context.Accounts.AsNoTracking().ToListAsync();

    public Task<Role?> GetRoleByIdAsync(int roleId) =>
        _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);
}
```

- [ ] Implement `EfIdentityWriteStore` in Infrastructure.

```csharp
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;

public class EfIdentityWriteStore : IIdentityWriteStore
{
    private readonly KoiFengShuiContext _context;

    public EfIdentityWriteStore(KoiFengShuiContext context) => _context = context;

    public async Task<Account> CreateAccountAsync(Account account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAccountAsync(Account account)
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAccountAsync(Account account)
    {
        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
    }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
```

- [ ] Implement `JwtTokenService` in Infrastructure.

Move the JWT generation logic from `JwtUtils`/`IJwtUtils` into this service. Update namespace to `KoiFengShuiSystem.Modules.Identity.Infrastructure.Security`.

- [ ] Implement `LegacyIdentityEmailSender` in Infrastructure.

Bridge to the existing `EmailService` in `KoiFengShuiSystem.BusinessLogic.Services.Implement`. This is a temporary adapter until Notifications module is extracted.

- [ ] Add `IdentityModuleInstaller`.

```csharp
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Email;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;
using KoiFengShuiSystem.Shared.Kernel.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure;

public class IdentityModuleInstaller : IModuleInstaller
{
    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IIdentityReadStore, EfIdentityReadStore>();
        services.AddScoped<IIdentityWriteStore, EfIdentityWriteStore>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IIdentityEmailSender, LegacyIdentityEmailSender>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<AdminAccountService>();
    }
}
```

- [ ] Update Host module installer scanning.

```csharp
builder.Services.AddModuleInstallersFromAssemblies(
    builder.Configuration,
    typeof(Program).Assembly,
    typeof(KoiFengShuiSystem.Modules.FengShui.Infrastructure.FengShuiModuleInstaller).Assembly,
    typeof(KoiFengShuiSystem.Modules.Identity.Infrastructure.IdentityModuleInstaller).Assembly);
```

- [ ] Add Host project references to Identity modules.

```xml
<ProjectReference Include="..\Modules\Identity\Identity.Application\Identity.Application.csproj" />
<ProjectReference Include="..\Modules\Identity\Identity.Infrastructure\Identity.Infrastructure.csproj" />
```

- [ ] Remove manual Identity service registrations from `src/Host/Program.cs` only after `IdentityModuleInstaller` is active and verified.

Remove these lines:
```csharp
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<AdminAccountService>();
builder.Services.AddScoped<IJwtUtils, JwtUtils>();
```

- [ ] Run all tests.

```powershell
dotnet test KoiFengShuiSystem.sln --no-build
```

Expected: all unit and integration tests pass.

Checkpoint commit message if explicitly requested later: `refactor(identity): extract infrastructure`.

---

## Task 6.5: Move Identity Controllers To Identity.Api

**Files:**
- Move: `KoiFengShuiSystem.Api/Controllers/AuthController.cs`
- Move: `KoiFengShuiSystem.Api/Controllers/AccountController.cs`
- Target: `src/Modules/Identity/Identity.Api/Controllers/*.cs`
- Modify: `src/Host/Program.cs`
- Test: `tests/IntegrationTests/IdentityApiContractTests.cs`

- [ ] Move the two controllers to `src/Modules/Identity/Identity.Api/Controllers/`.

- [ ] Update controller namespaces.

```csharp
namespace KoiFengShuiSystem.Modules.Identity.Api.Controllers;
```

- [ ] Update controller usings to Application contracts.

```csharp
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
```

- [ ] Preserve controller route attributes exactly.

```csharp
[ApiController]
[Route("api/[controller]")]
```

- [ ] Update Host controller discovery.

```csharp
builder.Services.AddControllers()
    .AddApplicationPart(typeof(KoiFengShuiSystem.Api.Controllers.AuthController).Assembly)
    .AddApplicationPart(typeof(KoiFengShuiSystem.Modules.FengShui.Api.Controllers.CompatibilityController).Assembly)
    .AddApplicationPart(typeof(KoiFengShuiSystem.Modules.Identity.Api.Controllers.AuthController).Assembly)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 32;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
```

Note: Remove old `AuthController` and `AccountController` from `KoiFengShuiSystem.Api` after the new controllers are active to avoid route duplication.

- [ ] Add integration test `tests/IntegrationTests/IdentityApiContractTests.cs`.

```csharp
using System.Net;
using System.Text.Json;

namespace IntegrationTests;

public class IdentityApiContractTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public IdentityApiContractTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_ContainsIdentityRoutes()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/Auth/SignIn", out _));
        Assert.True(paths.TryGetProperty("/api/Auth/SignUp", out _));
        Assert.True(paths.TryGetProperty("/api/Auth/ForgotPassword", out _));
        Assert.True(paths.TryGetProperty("/api/Auth/google-login", out _));
        Assert.True(paths.TryGetProperty("/api/Account", out _));
    }
}
```

- [ ] Build and run integration tests.

```powershell
dotnet build KoiFengShuiSystem.sln --no-restore
dotnet test tests/IntegrationTests/IntegrationTests.csproj --no-build
```

Expected: build exit 0 and all integration tests pass.

Checkpoint commit message if explicitly requested later: `refactor(identity): move api surface`.

---

## Task 6.6: Final Verification And Documentation

**Files:**
- Modify: `docs/architecture/current-dependencies.md`
- Modify: `docs/refactor-baseline.md`
- Do not modify: `KoiFengShuiSystem_Documentation.md`

- [ ] Run restore.

```powershell
dotnet restore KoiFengShuiSystem.sln
```

Expected: exit 0, existing package vulnerability warnings only.

- [ ] Run build.

```powershell
dotnet build KoiFengShuiSystem.sln --no-restore
```

Expected: exit 0.

- [ ] Run tests.

```powershell
dotnet test KoiFengShuiSystem.sln --no-build
```

Expected: all unit and integration tests pass.

- [ ] Verify no schema drift one final time.

```powershell
dotnet ef migrations add VerifyIdentityPhase6NoSchemaChange --project KoiFengShuiSystem.DataAccess --startup-project src/Host
```

Expected: generated migration `Up()` and `Down()` methods are empty.

- [ ] Remove final temporary migration.

```powershell
dotnet ef migrations remove --project KoiFengShuiSystem.DataAccess --startup-project src/Host
```

Expected: temporary migration removed.

- [ ] Update `docs/architecture/current-dependencies.md` with:
  - new Identity module projects
  - moved controllers/services/DTOs/entities
  - remaining temporary cross-module entity references
  - Host module installer discovery change

- [ ] Update `docs/refactor-baseline.md` with:
  - Phase 6 verification commands
  - build/test result summary
  - schema-drift result summary

- [ ] Review `git diff --stat` and confirm only intended Phase 6 files changed.

```powershell
git diff --stat
```

Expected: no edits to `KoiFengShuiSystem_Documentation.md` from Phase 6 work.

Final commit message if explicitly requested later: `refactor(identity): extract module boundary`.

---

## Review Checklist

- [ ] Public route templates are unchanged.
- [ ] Swagger exposes the same Identity endpoints.
- [ ] `KoiFengShuiContext` still owns one database context.
- [ ] Existing migration files remain in `KoiFengShuiSystem.DataAccess/Migrations`.
- [ ] Temporary migration checks produce empty `Up()` and `Down()` methods.
- [ ] `Shared.Kernel` does not reference module projects.
- [ ] `Identity.Application` does not reference `KoiFengShuiSystem.DataAccess`.
- [ ] `Identity.Infrastructure` is the only Identity project that directly uses EF Core/shared persistence.
- [ ] Host no longer manually registers Identity services after `IdentityModuleInstaller` is active.
- [ ] Tests pass: `dotnet test KoiFengShuiSystem.sln --no-build`.
- [ ] `[Authorize]` and `[AllowAnonymous]` behavior unchanged for all Identity endpoints.
- [ ] JWT token generation produces same format/claims as before.
- [ ] Email sending for password reset and Google login still works through temporary adapter.

---

## Execution Handoff

Recommended execution mode: **Subagent-Driven** with one fresh implementation subagent per task and a quality review after each task.

Do not commit during execution unless the user explicitly asks for commits.
