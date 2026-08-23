# Phase 5 FengShui Module Extraction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract the FengShui bounded context into module projects while preserving existing public API routes, response shapes, EF schema, and test coverage.

**Architecture:** Create `FengShui.Domain`, `FengShui.Application`, `FengShui.Infrastructure`, and `FengShui.Api` projects under `src/Modules/FengShui/`. Move the domain model and business logic incrementally, use application-level ports for persistence access, keep the shared `KoiFengShuiContext` as the single EF context, and keep EF schema stable. Because the current entities have cross-module navigation cycles, remove or convert only the cross-module navigation properties needed to break project cycles while preserving FK columns and unidirectional EF relationships.

**Tech Stack:** .NET 8, ASP.NET Core Controllers, EF Core SQL Server, xUnit, FluentAssertions, Swagger/OpenAPI, existing module installer contract, existing shared `KoiFengShuiContext`.

**Confidence:** medium-high. Current build and tests are green, and FengShui has focused tests. The main risk is entity cross-navigation between FengShui entities and `Account`, `Post`, and `MarketplaceListing`, so entity movement must be done with compile/test/schema checkpoints.

---

## Current Verified State

- `dotnet build KoiFengShuiSystem.sln --no-restore` passes with `0 Error(s)` and the same 4 package vulnerability warnings.
- `dotnet test KoiFengShuiSystem.sln --no-build` passes with `91/91` unit tests and `3/3` integration tests.
- Current working tree contains uncommitted Phase 4 work and a pre-existing `KoiFengShuiSystem_Documentation.md` modification. Do not edit, revert, or include `KoiFengShuiSystem_Documentation.md` in Phase 5 work.
- Do not commit unless explicitly requested by the user.

---

## Scope Decisions

- Preserve these API routes exactly:
  - `POST /api/Compatibility/lookup`
  - `POST /api/Consultation/fengshui`
  - `GET /api/Element/GetAll`
- Keep the existing database schema stable.
- Keep migrations in `KoiFengShuiSystem.DataAccess/Migrations`.
- Keep `KoiFengShuiContext` in `src/Shared/Shared.Infrastructure/Persistence/KoiFengShuiContext.cs`.
- Keep controllers in the old API assembly until the new FengShui API project is wired into Host, then move only the three FengShui controllers.
- Use module installer registration for FengShui services; remove FengShui manual registrations from Host after module installer verification.
- Avoid moving unrelated DTOs, services, controllers, or documentation.

---

## File Structure Map

### New Projects

- `src/Modules/FengShui/FengShui.Domain/FengShui.Domain.csproj`
  - Owns FengShui entities after Task 5.2.
  - May reference `Shared.Kernel` temporarily for cross-module entity references during transition.
- `src/Modules/FengShui/FengShui.Application/FengShui.Application.csproj`
  - Owns service contracts, request/response DTOs, calculator, business services, and persistence ports.
- `src/Modules/FengShui/FengShui.Infrastructure/FengShui.Infrastructure.csproj`
  - Owns EF-backed query/store adapters and `FengShuiModuleInstaller`.
- `src/Modules/FengShui/FengShui.Api/FengShui.Api.csproj`
  - Owns FengShui controllers after Task 5.4.

### Files To Move Or Modify

- Move entities from `src/Shared/Shared.Kernel/Models/` to `src/Modules/FengShui/FengShui.Domain/Entities/`:
  - `Element.cs`
  - `Direction.cs`
  - `FengShuiDirection.cs`
  - `FishPond.cs`
  - `KoiBreed.cs`
  - `ShapeCategory.cs`
  - `Recommendation.cs`
  - `Country.cs`
- Move calculator:
  - From `KoiFengShuiSystem.Common/FengShui/CungPhiCalculator.cs`
  - To `src/Modules/FengShui/FengShui.Application/Calculations/CungPhiCalculator.cs`
- Move DTOs:
  - `KoiFengShuiSystem.Shared/Models/Request/CompatibilityRequest.cs`
  - `KoiFengShuiSystem.Shared/Models/Request/FengShuiRequest.cs`
  - `KoiFengShuiSystem.Shared/Models/Response/CompatibilityResponse.cs`
  - `KoiFengShuiSystem.Shared/Models/Response/FengShuiResponse.cs`
- Move interfaces:
  - `KoiFengShuiSystem.Services/Services/Interface/ICompatibilityService.cs`
  - `KoiFengShuiSystem.Services/Services/Interface/IConsultationService.cs`
  - `KoiFengShuiSystem.Services/Services/Interface/IElementService.cs`
- Move services:
  - `KoiFengShuiSystem.Services/Services/Implement/CompatibilityService.cs`
  - `KoiFengShuiSystem.Services/Services/Implement/ConsultationService.cs`
  - `KoiFengShuiSystem.Services/Services/Implement/ElementService.cs`
- Move controllers:
  - `KoiFengShuiSystem.Api/Controllers/CompatibilityController.cs`
  - `KoiFengShuiSystem.Api/Controllers/ConsultationController.cs`
  - `KoiFengShuiSystem.Api/Controllers/ElementController.cs`
- Modify shared persistence:
  - `src/Shared/Shared.Infrastructure/Persistence/KoiFengShuiContext.cs`
  - `src/Shared/Shared.Infrastructure/Persistence/Configurations/*.cs`
  - `src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj`
- Modify Host:
  - `src/Host/Program.cs`
  - `src/Host/Host.csproj`
- Modify tests:
  - `tests/UnitTests/FengShui/*.cs`
  - `tests/IntegrationTests/AppBootstrapTests.cs`
  - Add `tests/IntegrationTests/FengShuiApiContractTests.cs`

---

## Task 5.0: Preflight And Boundary Audit

**Files:** No code changes expected.

- [ ] Run `git status --short`.

Expected: uncommitted Phase 4 files may appear. `KoiFengShuiSystem_Documentation.md` may appear and must not be touched.

- [ ] Run the current solution build.

```powershell
dotnet build KoiFengShuiSystem.sln --no-restore
```

Expected: exit 0, existing package vulnerability warnings only.

- [ ] Run the current solution tests.

```powershell
dotnet test KoiFengShuiSystem.sln --no-build
```

Expected: `91/91` unit tests pass and `3/3` integration tests pass.

- [ ] Audit entity cross-navigation before moving entities.

```powershell
rg "Element|Recommendation|KoiBreed|FishPond|ShapeCategory|FengShuiDirection|Direction|Country" src/Shared/Shared.Kernel/Models KoiFengShuiSystem.Services KoiFengShuiSystem.Api tests
```

Expected: identify all references that must be updated when namespaces change.

- [ ] Audit old FengShui service registrations in Host.

```powershell
rg "ICompatibilityService|IConsultationService|IElementService|CompatibilityService|ConsultationService|ElementService" src/Host/Program.cs
```

Expected: Host still manually registers the three FengShui services before module installer migration.

---

## Task 5.1: Create FengShui Module Projects

**Files:**
- Create: `src/Modules/FengShui/FengShui.Domain/FengShui.Domain.csproj`
- Create: `src/Modules/FengShui/FengShui.Application/FengShui.Application.csproj`
- Create: `src/Modules/FengShui/FengShui.Infrastructure/FengShui.Infrastructure.csproj`
- Create: `src/Modules/FengShui/FengShui.Api/FengShui.Api.csproj`
- Modify: `KoiFengShuiSystem.sln`

- [ ] Create the module directories.

```powershell
New-Item -ItemType Directory -Path "src/Modules/FengShui/FengShui.Domain" -Force
New-Item -ItemType Directory -Path "src/Modules/FengShui/FengShui.Application" -Force
New-Item -ItemType Directory -Path "src/Modules/FengShui/FengShui.Infrastructure" -Force
New-Item -ItemType Directory -Path "src/Modules/FengShui/FengShui.Api" -Force
```

- [ ] Create `FengShui.Domain.csproj`.

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

- [ ] Create `FengShui.Application.csproj`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\FengShui.Domain\FengShui.Domain.csproj" />
    <ProjectReference Include="..\..\..\Shared\Shared.Kernel\Shared.Kernel.csproj" />
  </ItemGroup>
</Project>
```

- [ ] Create `FengShui.Infrastructure.csproj`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\FengShui.Application\FengShui.Application.csproj" />
    <ProjectReference Include="..\FengShui.Domain\FengShui.Domain.csproj" />
    <ProjectReference Include="..\..\..\Shared\Shared.Infrastructure\Shared.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

- [ ] Create `FengShui.Api.csproj`.

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
    <ProjectReference Include="..\FengShui.Application\FengShui.Application.csproj" />
    <ProjectReference Include="..\FengShui.Infrastructure\FengShui.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

- [ ] Add all four projects to the solution.

```powershell
dotnet sln KoiFengShuiSystem.sln add src/Modules/FengShui/FengShui.Domain/FengShui.Domain.csproj
dotnet sln KoiFengShuiSystem.sln add src/Modules/FengShui/FengShui.Application/FengShui.Application.csproj
dotnet sln KoiFengShuiSystem.sln add src/Modules/FengShui/FengShui.Infrastructure/FengShui.Infrastructure.csproj
dotnet sln KoiFengShuiSystem.sln add src/Modules/FengShui/FengShui.Api/FengShui.Api.csproj
```

- [ ] Restore and build.

```powershell
dotnet restore KoiFengShuiSystem.sln
dotnet build KoiFengShuiSystem.sln --no-restore
```

Expected: build exit 0.

Checkpoint commit message if explicitly requested later: `refactor(fengshui): add module project skeleton`.

---

## Task 5.2: Move FengShui Domain Entities Without Schema Drift

**Files:**
- Move: `src/Shared/Shared.Kernel/Models/{Element,Direction,FengShuiDirection,FishPond,KoiBreed,ShapeCategory,Recommendation,Country}.cs`
- Target: `src/Modules/FengShui/FengShui.Domain/Entities/*.cs`
- Modify: `src/Shared/Shared.Kernel/Models/Account.cs`
- Modify: `src/Shared/Shared.Kernel/Models/MarketplaceListing.cs`
- Modify: `src/Shared/Shared.Kernel/Models/Post.cs`
- Modify: `src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj`
- Modify: `src/Shared/Shared.Infrastructure/Persistence/KoiFengShuiContext.cs`
- Modify: `src/Shared/Shared.Infrastructure/Persistence/Configurations/*.cs`
- Modify: `KoiFengShuiSystem.DataAccess/Migrations/*.cs` only for namespace imports if required by compile.

- [ ] Add a project reference from `Shared.Infrastructure` to `FengShui.Domain` so the shared context can expose FengShui `DbSet<T>` properties.

```xml
<ProjectReference Include="..\..\Modules\FengShui\FengShui.Domain\FengShui.Domain.csproj" />
```

- [ ] Move the eight FengShui entity files into `src/Modules/FengShui/FengShui.Domain/Entities/`.

- [ ] Update entity namespaces to `KoiFengShuiSystem.Modules.FengShui.Domain.Entities`.

Example for `Element.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using KoiFengShuiSystem.DataAccess.Models;

namespace KoiFengShuiSystem.Modules.FengShui.Domain.Entities;

public class Element
{
    [Key]
    public int ElementId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ElementName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LuckyNumber { get; set; } = string.Empty;

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
    public virtual ICollection<FengShuiDirection> FengShuiDirections { get; set; } = new List<FengShuiDirection>();
    public virtual ICollection<KoiBreed> KoiBreeds { get; set; } = new List<KoiBreed>();
    public virtual ICollection<MarketplaceListing> MarketplaceListings { get; set; } = new List<MarketplaceListing>();
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    public virtual ICollection<ShapeCategory> ShapeCategories { get; set; } = new List<ShapeCategory>();
}
```

- [ ] Break project cycles by removing direct FengShui navigation property dependencies from remaining `Shared.Kernel` entity classes while preserving FK id properties.

Expected edits:

```csharp
// src/Shared/Shared.Kernel/Models/Account.cs
// Keep: public int? ElementId { get; set; }
// Remove: public virtual Element? Element { get; set; }
// Remove: public virtual ICollection<Recommendation> Recommendations { get; set; }

// src/Shared/Shared.Kernel/Models/MarketplaceListing.cs
// Keep: public int? ElementId { get; set; }
// Remove: public virtual Element? Element { get; set; }

// src/Shared/Shared.Kernel/Models/Post.cs
// Keep: public int? ElementId { get; set; }
// Remove: public virtual Element? Element { get; set; }
```

- [ ] Update `KoiFengShuiContext.cs` usings.

```csharp
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.EntityFrameworkCore;
```

- [ ] Update EF configuration usings and relationship mappings.

For FengShui-owned configuration files, add:

```csharp
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
```

For cross-module relationships from non-FengShui entities, use unidirectional mappings where the non-FengShui navigation was removed.

Example for `AccountConfiguration.cs`:

```csharp
builder.HasOne<Element>()
    .WithMany(p => p.Accounts)
    .HasForeignKey(d => d.ElementId);
```

Example for `MarketplaceListingConfiguration.cs`:

```csharp
builder.HasOne<Element>()
    .WithMany(p => p.MarketplaceListings)
    .HasForeignKey(d => d.ElementId);
```

Example for `PostConfiguration.cs`:

```csharp
builder.HasOne<Element>()
    .WithMany(p => p.Posts)
    .HasForeignKey(d => d.ElementId);
```

Example for `RecommendationConfiguration.cs`:

```csharp
builder.HasOne(d => d.Account)
    .WithMany()
    .HasForeignKey(d => d.AccountId)
    .OnDelete(DeleteBehavior.Restrict);
```

- [ ] Build after entity move.

```powershell
dotnet build KoiFengShuiSystem.sln --no-restore
```

Expected: compile errors identify missed namespace imports only. Fix only FengShui-related imports/references.

- [ ] Run unit tests.

```powershell
dotnet test tests/UnitTests/UnitTests.csproj --no-build
```

Expected: all unit tests pass.

- [ ] Verify no EF schema drift with a temporary migration.

```powershell
dotnet ef migrations add VerifyFengShuiEntityMoveNoSchemaChange --project KoiFengShuiSystem.DataAccess --startup-project src/Host
```

Expected: generated migration `Up()` and `Down()` methods are empty.

- [ ] Remove the temporary migration.

```powershell
dotnet ef migrations remove --project KoiFengShuiSystem.DataAccess --startup-project src/Host
```

Expected: temporary migration removed; model snapshot remains only with legitimate namespace/context updates.

Checkpoint commit message if explicitly requested later: `refactor(fengshui): move domain entities`.

---

## Task 5.3: Move Calculator, DTOs, Service Contracts, And Application Logic

**Files:**
- Create: `src/Modules/FengShui/FengShui.Application/Calculations/CungPhiCalculator.cs`
- Create: `src/Modules/FengShui/FengShui.Application/Abstractions/IFengShuiReadStore.cs`
- Create: `src/Modules/FengShui/FengShui.Application/Requests/CompatibilityRequest.cs`
- Create: `src/Modules/FengShui/FengShui.Application/Requests/FengShuiRequest.cs`
- Create: `src/Modules/FengShui/FengShui.Application/Responses/CompatibilityResponse.cs`
- Create: `src/Modules/FengShui/FengShui.Application/Responses/FengShuiResponse.cs`
- Create: `src/Modules/FengShui/FengShui.Application/Services/ICompatibilityService.cs`
- Create: `src/Modules/FengShui/FengShui.Application/Services/IConsultationService.cs`
- Create: `src/Modules/FengShui/FengShui.Application/Services/IElementService.cs`
- Create: `src/Modules/FengShui/FengShui.Application/Services/CompatibilityService.cs`
- Create: `src/Modules/FengShui/FengShui.Application/Services/ConsultationService.cs`
- Create: `src/Modules/FengShui/FengShui.Application/Services/ElementService.cs`
- Create: `src/Modules/FengShui/FengShui.Infrastructure/Persistence/EfFengShuiReadStore.cs`
- Create: `src/Modules/FengShui/FengShui.Infrastructure/FengShuiModuleInstaller.cs`
- Modify: `src/Host/Program.cs`
- Modify: `tests/UnitTests/FengShui/*.cs`

- [ ] Move `CungPhiCalculator` and `CungPhiResult` to Application namespace.

```csharp
namespace KoiFengShuiSystem.Modules.FengShui.Application.Calculations;
```

- [ ] Move only FengShui DTOs into Application namespaces.

```csharp
namespace KoiFengShuiSystem.Modules.FengShui.Application.Requests;
namespace KoiFengShuiSystem.Modules.FengShui.Application.Responses;
```

- [ ] Create the persistence port `IFengShuiReadStore`.

```csharp
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;

public interface IFengShuiReadStore
{
    Task<Element?> GetElementByNameAsync(string elementName);
    Task<IReadOnlyList<Element>> GetAllElementsAsync();
    Task<Direction?> GetDirectionByNameAsync(string directionName);
    Task<FengShuiDirection?> GetFengShuiDirectionAsync(int directionId, int elementId);
    Task<ShapeCategory?> GetShapeByNameAndElementIdAsync(string shapeName, int elementId);
    Task<IReadOnlyList<ShapeCategory>> GetAllShapeCategoriesAsync();
    Task<IReadOnlyList<KoiBreed>> GetAllKoiBreedsAsync();
    Task<IReadOnlyList<FengShuiDirection>> GetAllFengShuiDirectionsWithDirectionAsync();
}
```

- [ ] Move the three service interfaces to Application namespaces and update DTO namespaces.

```csharp
namespace KoiFengShuiSystem.Modules.FengShui.Application.Services;
```

- [ ] Move service implementations to Application and replace `GenericRepository<T>` / `UnitOfWorkRepository` dependencies with `IFengShuiReadStore`.

Example constructor target for `ConsultationService`:

```csharp
public ConsultationService(IFengShuiReadStore readStore, ILogger<ConsultationService> logger)
{
    _readStore = readStore;
    _logger = logger;
}
```

Example replacement:

```csharp
var element = await _readStore.GetElementByNameAsync(cungPhiResult.Menh);
var allShapes = await _readStore.GetAllShapeCategoriesAsync();
var koiBreeds = await _readStore.GetAllKoiBreedsAsync();
var fengShuiDirections = await _readStore.GetAllFengShuiDirectionsWithDirectionAsync();
```

- [ ] Implement `EfFengShuiReadStore` in Infrastructure.

```csharp
using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.Modules.FengShui.Infrastructure.Persistence;

public class EfFengShuiReadStore : IFengShuiReadStore
{
    private readonly KoiFengShuiContext _context;

    public EfFengShuiReadStore(KoiFengShuiContext context)
    {
        _context = context;
    }

    public Task<Element?> GetElementByNameAsync(string elementName) =>
        _context.Elements.FirstOrDefaultAsync(e => e.ElementName == elementName);

    public async Task<IReadOnlyList<Element>> GetAllElementsAsync() =>
        await _context.Elements.AsNoTracking().ToListAsync();

    public Task<Direction?> GetDirectionByNameAsync(string directionName) =>
        _context.Directions.FirstOrDefaultAsync(d => d.DirectionName == directionName);

    public Task<FengShuiDirection?> GetFengShuiDirectionAsync(int directionId, int elementId) =>
        _context.FengShuiDirections.FirstOrDefaultAsync(f => f.DirectionId == directionId && f.ElementId == elementId);

    public Task<ShapeCategory?> GetShapeByNameAndElementIdAsync(string shapeName, int elementId) =>
        _context.ShapeCategories.FirstOrDefaultAsync(s => s.ShapeName == shapeName && s.ElementId == elementId);

    public async Task<IReadOnlyList<ShapeCategory>> GetAllShapeCategoriesAsync() =>
        await _context.ShapeCategories.AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<KoiBreed>> GetAllKoiBreedsAsync() =>
        await _context.KoiBreeds.AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<FengShuiDirection>> GetAllFengShuiDirectionsWithDirectionAsync() =>
        await _context.FengShuiDirections.Include(f => f.Direction).AsNoTracking().ToListAsync();
}
```

- [ ] Add `FengShuiModuleInstaller`.

```csharp
using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Application.Services;
using KoiFengShuiSystem.Modules.FengShui.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Kernel.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KoiFengShuiSystem.Modules.FengShui.Infrastructure;

public class FengShuiModuleInstaller : IModuleInstaller
{
    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IFengShuiReadStore, EfFengShuiReadStore>();
        services.AddScoped<ICompatibilityService, CompatibilityService>();
        services.AddScoped<IConsultationService, ConsultationService>();
        services.AddScoped<IElementService, ElementService>();
    }
}
```

- [ ] Update Host module installer scanning.

```csharp
builder.Services.AddModuleInstallersFromAssemblies(
    builder.Configuration,
    typeof(Program).Assembly,
    typeof(KoiFengShuiSystem.Modules.FengShui.Infrastructure.FengShuiModuleInstaller).Assembly);
```

- [ ] Remove manual FengShui service registrations from `src/Host/Program.cs`.

Remove these lines only after `FengShuiModuleInstaller` is active:

```csharp
builder.Services.AddScoped<ICompatibilityService, CompatibilityService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped<IElementService, ElementService>();
```

- [ ] Update unit tests to use new namespaces.

Expected namespace imports:

```csharp
using KoiFengShuiSystem.Modules.FengShui.Application.Calculations;
using KoiFengShuiSystem.Modules.FengShui.Application.Requests;
using KoiFengShuiSystem.Modules.FengShui.Application.Services;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
```

- [ ] Run FengShui tests.

```powershell
dotnet test tests/UnitTests/UnitTests.csproj --no-build --filter FengShui
```

Expected: all FengShui tests pass.

- [ ] Run all tests.

```powershell
dotnet test KoiFengShuiSystem.sln --no-build
```

Expected: all unit and integration tests pass.

Checkpoint commit message if explicitly requested later: `refactor(fengshui): move application services`.

---

## Task 5.4: Move FengShui Controllers To FengShui.Api

**Files:**
- Move: `KoiFengShuiSystem.Api/Controllers/CompatibilityController.cs`
- Move: `KoiFengShuiSystem.Api/Controllers/ConsultationController.cs`
- Move: `KoiFengShuiSystem.Api/Controllers/ElementController.cs`
- Target: `src/Modules/FengShui/FengShui.Api/Controllers/*.cs`
- Modify: `src/Host/Program.cs`
- Test: `tests/IntegrationTests/FengShuiApiContractTests.cs`

- [ ] Move the three controllers to `src/Modules/FengShui/FengShui.Api/Controllers/`.

- [ ] Update controller namespaces.

```csharp
namespace KoiFengShuiSystem.Modules.FengShui.Api.Controllers;
```

- [ ] Update controller usings to Application contracts.

```csharp
using KoiFengShuiSystem.Modules.FengShui.Application.Requests;
using KoiFengShuiSystem.Modules.FengShui.Application.Services;
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
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 32;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
```

- [ ] Add integration test `tests/IntegrationTests/FengShuiApiContractTests.cs`.

```csharp
using System.Net;
using System.Text.Json;

namespace IntegrationTests;

public class FengShuiApiContractTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public FengShuiApiContractTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_ContainsFengShuiRoutes()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/Compatibility/lookup", out _));
        Assert.True(paths.TryGetProperty("/api/Consultation/fengshui", out _));
        Assert.True(paths.TryGetProperty("/api/Element/GetAll", out _));
    }
}
```

- [ ] Build and run integration tests.

```powershell
dotnet build KoiFengShuiSystem.sln --no-restore
dotnet test tests/IntegrationTests/IntegrationTests.csproj --no-build
```

Expected: build exit 0 and all integration tests pass.

Checkpoint commit message if explicitly requested later: `refactor(fengshui): move api surface`.

---

## Task 5.5: Final Verification And Documentation

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
dotnet ef migrations add VerifyFengShuiPhase5NoSchemaChange --project KoiFengShuiSystem.DataAccess --startup-project src/Host
```

Expected: generated migration `Up()` and `Down()` methods are empty.

- [ ] Remove final temporary migration.

```powershell
dotnet ef migrations remove --project KoiFengShuiSystem.DataAccess --startup-project src/Host
```

Expected: temporary migration removed.

- [ ] Update `docs/architecture/current-dependencies.md` with:
  - new FengShui module projects
  - moved controllers/services/DTOs/entities
  - remaining temporary cross-module entity references
  - Host module installer discovery change

- [ ] Update `docs/refactor-baseline.md` with:
  - Phase 5 verification commands
  - build/test result summary
  - schema-drift result summary

- [ ] Review `git diff --stat` and confirm only intended Phase 5 files changed.

```powershell
git diff --stat
```

Expected: no edits to `KoiFengShuiSystem_Documentation.md` from Phase 5 work.

Final commit message if explicitly requested later: `refactor(fengshui): extract module boundary`.

---

## Review Checklist

- [ ] Public route templates are unchanged.
- [ ] Swagger exposes the same FengShui endpoints.
- [ ] `KoiFengShuiContext` still owns one database context.
- [ ] Existing migration files remain in `KoiFengShuiSystem.DataAccess/Migrations`.
- [ ] Temporary migration checks produce empty `Up()` and `Down()` methods.
- [ ] `Shared.Kernel` does not reference module projects.
- [ ] `FengShui.Application` does not reference `KoiFengShuiSystem.DataAccess`.
- [ ] `FengShui.Infrastructure` is the only FengShui project that directly uses EF Core/shared persistence.
- [ ] Host no longer manually registers FengShui services after `FengShuiModuleInstaller` is active.
- [ ] Tests pass: `dotnet test KoiFengShuiSystem.sln --no-build`.

---

## Execution Handoff

Recommended execution mode: **Subagent-Driven** with one fresh implementation subagent per task and a quality review after each task.

Do not commit during execution unless the user explicitly asks for commits.
