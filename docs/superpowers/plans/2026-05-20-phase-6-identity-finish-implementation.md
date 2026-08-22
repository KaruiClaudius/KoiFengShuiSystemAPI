# Phase 6 Identity Finish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finish the remaining Phase 6 Identity extraction by removing temporary JWT wiring, removing the application-layer dependency on DataAccess/FengShui domain, moving Identity controllers into `Identity.Api`, and re-verifying build, tests, and zero schema drift.

**Architecture:** Keep the shared `KoiFengShuiContext`, preserve all public routes and JWT behavior, and finish the extraction through narrow application ports plus module installer registration. Make changes in small checkpoints so each slice can be built and tested before the next one.

**Tech Stack:** .NET 8, ASP.NET Core controllers, EF Core, xUnit, Moq, Swagger/OpenAPI, JWT Bearer, existing module installer discovery.

---

## File Structure Map

- `src/Modules/Identity/Identity.Application/Abstractions/IJwtTokenService.cs`
  - Expand the Identity JWT port to include token validation.
- `src/Modules/Identity/Identity.Application/Abstractions/IIdentityElementLookup.cs`
  - New application port for resolving Feng Shui element ids and names needed by `AccountService`.
- `src/Modules/Identity/Identity.Application/Services/AccountService.cs`
  - Remove `GenericRepository<Element>` and `FengShui.Domain` dependency.
- `src/Modules/Identity/Identity.Infrastructure/Security/JwtTokenService.cs`
  - Own JWT generation and validation behavior using `AppSettings` directly.
- `src/Modules/Identity/Identity.Infrastructure/Persistence/EfIdentityReadStore.cs`
  - Switch from `GenericRepository<T>` to `KoiFengShuiContext`.
- `src/Modules/Identity/Identity.Infrastructure/Persistence/EfIdentityWriteStore.cs`
  - Switch from `GenericRepository<T>` to `KoiFengShuiContext`.
- `src/Modules/Identity/Identity.Infrastructure/Persistence/EfIdentityElementLookup.cs`
  - New EF-backed implementation of `IIdentityElementLookup`.
- `src/Modules/Identity/Identity.Infrastructure/IdentityModuleInstaller.cs`
  - Register Identity stores, ports, and services through module installer discovery.
- `src/Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj`
  - Remove unused DataAccess reference if the final code no longer needs it.
- `src/Modules/Identity/Identity.Application/Identity.Application.csproj`
  - Remove direct `KoiFengShuiSystem.DataAccess` and `FengShui.Domain` references.
- `src/Host/Program.cs`
  - Add Identity installer assembly scan, add Identity.Api application part, remove manual Identity registrations.
- `src/Host/Middleware/JwtMiddleware.cs`
  - Replace `IJwtUtils` dependency with `IJwtTokenService`.
- `KoiFengShuiSystem.Api/Program.cs`
  - Mirror the Host DI/controller-discovery changes if this startup path is still kept buildable.
- `KoiFengShuiSystem.Api/Authorization/JwtMiddleware.cs`
  - Replace `IJwtUtils` dependency with `IJwtTokenService`.
- `src/Modules/Identity/Identity.Api/Controllers/AuthController.cs`
  - Move Identity auth API surface here.
- `src/Modules/Identity/Identity.Api/Controllers/AccountController.cs`
  - Move Identity account API surface here.
- `KoiFengShuiSystem.Api/Controllers/AuthController.cs`
  - Remove after the moved controller is active.
- `KoiFengShuiSystem.Api/Controllers/AccountController.cs`
  - Remove after the moved controller is active.
- `tests/UnitTests/Identity/AccountServiceTests.cs`
  - Replace legacy JWT and element repository setup with new ports/adapters.
- `tests/UnitTests/Identity/JwtTokenServiceTests.cs`
  - New unit tests for JWT generation and validation.
- `tests/IntegrationTests/IdentityApiContractTests.cs`
  - New Swagger route contract test for Identity endpoints.
- `docs/architecture/current-dependencies.md`
  - Update dependency inventory for the completed Identity module boundary.
- `docs/refactor-baseline.md`
  - Append Phase 6 completion verification results.

---

### Task 0: Preflight And Safety Check

**Files:** No code changes.

- [ ] **Step 1: Confirm working tree state before new edits**

Run: `git status --short`

Expected: existing Phase 6 changes are present; do not revert unrelated edits.

- [ ] **Step 2: Confirm current solution still builds before the finish work**

Run: `dotnet build KoiFengShuiSystem.sln --no-restore`

Expected: exit code `0` with existing warnings only.

- [ ] **Step 3: Confirm current tests still pass before the finish work**

Run: `dotnet test KoiFengShuiSystem.sln --no-build`

Expected: unit and integration suites pass from the current checkpoint.

---

### Task 1: Add JWT Validation To The Identity Port

**Files:**
- Modify: `src/Modules/Identity/Identity.Application/Abstractions/IJwtTokenService.cs`
- Create: `tests/UnitTests/Identity/JwtTokenServiceTests.cs`

- [ ] **Step 1: Add a failing JWT validation test file**

Create `tests/UnitTests/Identity/JwtTokenServiceTests.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;
using KoiFengShuiSystem.Shared.Helpers;
using Microsoft.Extensions.Options;

namespace UnitTests.Identity;

public class JwtTokenServiceTests
{
    private const string Secret = "test-secret-key-that-is-at-least-32-bytes-long-for-hmac";

    private static JwtTokenService CreateService()
    {
        var options = Options.Create(new AppSettings { Secret = Secret });
        return new JwtTokenService(options);
    }

    [Fact]
    public void GenerateJwtToken_IncludesExpectedClaims()
    {
        var service = CreateService();
        var account = new Account
        {
            AccountId = 123,
            Email = "identity@test.com",
            RoleId = 2
        };

        var token = service.GenerateJwtToken(account);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("123", jwt.Claims.Single(c => c.Type == "id").Value);
        Assert.Equal("identity@test.com", jwt.Claims.Single(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("2", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void ValidateJwtToken_WithValidToken_ReturnsAccountId()
    {
        var service = CreateService();
        var account = new Account
        {
            AccountId = 55,
            Email = "valid@test.com",
            RoleId = 1
        };

        var token = service.GenerateJwtToken(account);

        var accountId = service.ValidateJwtToken(token);

        Assert.Equal(55, accountId);
    }

    [Fact]
    public void ValidateJwtToken_WithInvalidToken_ReturnsNull()
    {
        var service = CreateService();

        var accountId = service.ValidateJwtToken("not-a-real-token");

        Assert.Null(accountId);
    }
}
```

- [ ] **Step 2: Run the JWT tests to verify they fail before implementation**

Run: `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~JwtTokenServiceTests"`

Expected: compile or test failure because `JwtTokenService` does not yet own `ValidateJwtToken` and may not accept `IOptions<AppSettings>` directly.

- [ ] **Step 3: Expand the application JWT port**

Replace `src/Modules/Identity/Identity.Application/Abstractions/IJwtTokenService.cs` with:

```csharp
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateJwtToken(Account account);

    int? ValidateJwtToken(string? token);
}
```

- [ ] **Step 4: Run the JWT tests again to verify the interface change alone is not enough**

Run: `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~JwtTokenServiceTests"`

Expected: failure remains until `JwtTokenService` is updated.

---

### Task 2: Move JWT Behavior Fully Into Identity.Infrastructure

**Files:**
- Modify: `src/Modules/Identity/Identity.Infrastructure/Security/JwtTokenService.cs`

- [ ] **Step 1: Replace the legacy-wrapper implementation with direct JWT logic**

Replace `src/Modules/Identity/Identity.Infrastructure/Security/JwtTokenService.cs` with:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly AppSettings _appSettings;

    public JwtTokenService(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;

        if (string.IsNullOrWhiteSpace(_appSettings.Secret))
        {
            throw new Exception("JWT secret not configured");
        }
    }

    public string GenerateJwtToken(Account account)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_appSettings.Secret!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", account.AccountId.ToString()),
                new Claim(ClaimTypes.Email, account.Email),
                new Claim(ClaimTypes.Role, account.RoleId?.ToString() ?? string.Empty)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public int? ValidateJwtToken(string? token)
    {
        if (token is null)
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_appSettings.Secret!);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            return int.Parse(jwtToken.Claims.First(claim => claim.Type == "id").Value);
        }
        catch
        {
            return null;
        }
    }
}
```

- [ ] **Step 2: Run the JWT tests to verify the new implementation passes**

Run: `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~JwtTokenServiceTests"`

Expected: all `JwtTokenServiceTests` pass.

---

### Task 3: Remove Application-Layer Element Repository Coupling

**Files:**
- Create: `src/Modules/Identity/Identity.Application/Abstractions/IIdentityElementLookup.cs`
- Modify: `src/Modules/Identity/Identity.Application/Services/AccountService.cs`
- Modify: `src/Modules/Identity/Identity.Application/Identity.Application.csproj`
- Create: `src/Modules/Identity/Identity.Infrastructure/Persistence/EfIdentityElementLookup.cs`

- [ ] **Step 1: Add a failing account-service test path by switching the test factory away from legacy JWT/repository assumptions**

In `tests/UnitTests/Identity/AccountServiceTests.cs`, replace the helper method signature and construction block:

```csharp
private static IdentityAccountService CreateService(
    KoiFengShuiContext? context = null,
    IJwtTokenService? jwtTokenService = null,
    IIdentityEmailSender? identityEmailSender = null,
    IIdentityElementLookup? elementLookup = null)
{
    var ctx = context ?? CreateContext();
    var jwt = jwtTokenService ?? Mock.Of<IJwtTokenService>(j => j.GenerateJwtToken(It.IsAny<AccountEntity>()) == "test-token");
    var email = identityEmailSender ?? new LegacyIdentityEmailSender(CreateEmailService());
    var lookup = elementLookup ?? new EfIdentityElementLookup(ctx);
    var logger = Mock.Of<ILogger<IdentityAccountService>>();

    return new IdentityAccountService(
        new EfIdentityReadStore(ctx),
        new EfIdentityWriteStore(ctx),
        jwt,
        email,
        logger,
        lookup);
}
```

Expected: this does not compile until the new port and constructor shape exist.

- [ ] **Step 2: Add the new application port**

Create `src/Modules/Identity/Identity.Application/Abstractions/IIdentityElementLookup.cs`:

```csharp
namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IIdentityElementLookup
{
    Task<int?> GetElementIdByNameAsync(string elementName);

    Task<string?> GetElementNameByIdAsync(int elementId);
}
```

- [ ] **Step 3: Remove the DataAccess and FengShui.Domain usings from AccountService and inject the new port**

Update the top of `src/Modules/Identity/Identity.Application/Services/AccountService.cs` so the using block and fields become:

```csharp
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Responses;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Modules.Identity.Application.Services;

public class AccountService : IAccountService
{
    private readonly IIdentityReadStore _readStore;
    private readonly IIdentityWriteStore _writeStore;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IIdentityEmailSender _identityEmailSender;
    private readonly ILogger<AccountService> _logger;
    private readonly IIdentityElementLookup _elementLookup;

    public AccountService(
        IIdentityReadStore readStore,
        IIdentityWriteStore writeStore,
        IJwtTokenService jwtTokenService,
        IIdentityEmailSender identityEmailSender,
        ILogger<AccountService> logger,
        IIdentityElementLookup elementLookup)
    {
        _readStore = readStore;
        _writeStore = writeStore;
        _jwtTokenService = jwtTokenService;
        _identityEmailSender = identityEmailSender;
        _logger = logger;
        _elementLookup = elementLookup;
    }
```

- [ ] **Step 4: Replace the element repository calls inside AccountService**

In `GetAccountResponseByEmailAsync`, replace the element lookup block with:

```csharp
string? elementName = null;
if (account.ElementId.HasValue)
{
    elementName = await _elementLookup.GetElementNameByIdAsync(account.ElementId.Value);
}
```

In `GetElementFromDateOfBirth`, replace the body with:

```csharp
private async Task<int> GetElementIdFromDateOfBirth(int yearOfBirth, string gender)
{
    var elementName = CalculateElement(yearOfBirth, gender);
    var elementId = await _elementLookup.GetElementIdByNameAsync(elementName);

    if (!elementId.HasValue)
    {
        _logger.LogError("Element not found for elementName: {ElementName}", elementName);
        throw new ApplicationException($"Element '{elementName}' not found in the database.");
    }

    return elementId.Value;
}
```

Then replace these three call sites:

```csharp
account.ElementId = await GetElementIdFromDateOfBirth(account.Dob.Value.Year, account.Gender);
```

```csharp
account.ElementId = await GetElementIdFromDateOfBirth(account.Dob.Value.Year, account.Gender);
```

```csharp
account.ElementId = await GetElementIdFromDateOfBirth(
    model.Dob?.Year ?? account.Dob?.Year ?? DateTime.Now.Year,
    model.Gender ?? account.Gender);
```

- [ ] **Step 5: Add the EF-backed implementation of the new port**

Create `src/Modules/Identity/Identity.Infrastructure/Persistence/EfIdentityElementLookup.cs`:

```csharp
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;

public class EfIdentityElementLookup : IIdentityElementLookup
{
    private readonly KoiFengShuiContext _context;

    public EfIdentityElementLookup(KoiFengShuiContext context)
    {
        _context = context;
    }

    public async Task<int?> GetElementIdByNameAsync(string elementName)
    {
        return await _context.Elements
            .Where(element => element.ElementName == elementName)
            .Select(element => (int?)element.ElementId)
            .FirstOrDefaultAsync();
    }

    public async Task<string?> GetElementNameByIdAsync(int elementId)
    {
        return await _context.Elements
            .Where(element => element.ElementId == elementId)
            .Select(element => element.ElementName)
            .FirstOrDefaultAsync();
    }
}
```

- [ ] **Step 6: Remove the direct application-layer project references**

Replace `src/Modules/Identity/Identity.Application/Identity.Application.csproj` with:

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
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.2" />
  </ItemGroup>
</Project>
```

- [ ] **Step 7: Run the Identity unit tests after the decoupling change**

Run: `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~AccountServiceTests"`

Expected: `AccountServiceTests` pass with the new element lookup and JWT port shapes.

---

### Task 4: Move Identity Persistence Adapters To KoiFengShuiContext And Add Installer Registration

**Files:**
- Modify: `src/Modules/Identity/Identity.Infrastructure/Persistence/EfIdentityReadStore.cs`
- Modify: `src/Modules/Identity/Identity.Infrastructure/Persistence/EfIdentityWriteStore.cs`
- Create: `src/Modules/Identity/Identity.Infrastructure/IdentityModuleInstaller.cs`
- Modify: `src/Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj`
- Modify: `src/Host/Program.cs`
- Modify: `KoiFengShuiSystem.Api/Program.cs`

- [ ] **Step 1: Replace the read store with a DbContext-based implementation**

Replace `src/Modules/Identity/Identity.Infrastructure/Persistence/EfIdentityReadStore.cs` with:

```csharp
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;

public class EfIdentityReadStore : IIdentityReadStore
{
    private readonly KoiFengShuiContext _context;

    public EfIdentityReadStore(KoiFengShuiContext context)
    {
        _context = context;
    }

    public Task<Account?> GetAccountByEmailAsync(string email)
        => _context.Accounts.FirstOrDefaultAsync(account => account.Email == email);

    public Task<Account?> GetAccountByIdAsync(int accountId)
        => _context.Accounts.FirstOrDefaultAsync(account => account.AccountId == accountId);

    public async Task<IReadOnlyList<Account>> GetAllAccountsAsync()
        => await _context.Accounts.AsNoTracking().ToListAsync();

    public Task<Role?> GetRoleByIdAsync(int roleId)
        => _context.Roles.FirstOrDefaultAsync(role => role.RoleId == roleId);
}
```

- [ ] **Step 2: Replace the write store with a DbContext-based implementation**

Replace `src/Modules/Identity/Identity.Infrastructure/Persistence/EfIdentityWriteStore.cs` with:

```csharp
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;

public class EfIdentityWriteStore : IIdentityWriteStore
{
    private readonly KoiFengShuiContext _context;

    public EfIdentityWriteStore(KoiFengShuiContext context)
    {
        _context = context;
    }

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

    public Task<int> SaveChangesAsync()
        => _context.SaveChangesAsync();
}
```

- [ ] **Step 3: Add the Identity module installer**

Create `src/Modules/Identity/Identity.Infrastructure/IdentityModuleInstaller.cs`:

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
        services.AddScoped<IIdentityElementLookup, EfIdentityElementLookup>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IIdentityEmailSender, LegacyIdentityEmailSender>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<AdminAccountService>();
    }
}
```

- [ ] **Step 4: Remove the unused direct DataAccess reference if it is no longer needed**

Replace `src/Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj` with:

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
    <ProjectReference Include="..\..\..\..\KoiFengShuiSystem.Services\KoiFengShuiSystem.BusinessLogic.csproj" />
    <ProjectReference Include="..\..\..\..\KoiFengShuiSystem.Shared\KoiFengShuiSystem.Shared.csproj" />
    <ProjectReference Include="..\..\..\Shared\Shared.Infrastructure\Shared.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Replace manual Identity registrations in Host with installer discovery and Identity.Api controller discovery**

In `src/Host/Program.cs`, update controller discovery to:

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

Then remove these manual registrations from `src/Host/Program.cs`:

```csharp
builder.Services.AddScoped<KoiFengShuiSystem.Modules.Identity.Application.Abstractions.IIdentityReadStore, KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence.EfIdentityReadStore>();
builder.Services.AddScoped<KoiFengShuiSystem.Modules.Identity.Application.Abstractions.IIdentityWriteStore, KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence.EfIdentityWriteStore>();
builder.Services.AddScoped<KoiFengShuiSystem.Modules.Identity.Application.Abstractions.IJwtTokenService, KoiFengShuiSystem.Modules.Identity.Infrastructure.Security.JwtTokenService>();
builder.Services.AddScoped<KoiFengShuiSystem.Modules.Identity.Application.Abstractions.IIdentityEmailSender, KoiFengShuiSystem.Modules.Identity.Infrastructure.Email.LegacyIdentityEmailSender>();
builder.Services.AddScoped<KoiFengShuiSystem.Modules.Identity.Application.Services.IAccountService, KoiFengShuiSystem.Modules.Identity.Application.Services.AccountService>();
builder.Services.AddScoped<KoiFengShuiSystem.Modules.Identity.Application.Services.AdminAccountService>();
builder.Services.AddScoped<IJwtUtils, JwtUtils>();
```

Then update module installer discovery to:

```csharp
builder.Services.AddModuleInstallersFromAssemblies(
    builder.Configuration,
    typeof(Program).Assembly,
    typeof(KoiFengShuiSystem.Modules.FengShui.Infrastructure.FengShuiModuleInstaller).Assembly,
    typeof(KoiFengShuiSystem.Modules.Identity.Infrastructure.IdentityModuleInstaller).Assembly);
```

- [ ] **Step 6: Mirror the Identity installer discovery cleanup in the legacy API startup**

In `KoiFengShuiSystem.Api/Program.cs`, remove the same manual Identity registrations and keep module installer discovery active for the Identity infrastructure assembly by adding the Identity installer assembly to the `AddModuleInstallersFromAssemblies` call.

- [ ] **Step 7: Run the full unit test suite after the infrastructure and DI changes**

Run: `dotnet test tests/UnitTests/UnitTests.csproj`

Expected: all unit tests pass with installer-based Identity registrations still buildable.

---

### Task 5: Replace Legacy JWT Usage In Middleware And Controllers

**Files:**
- Modify: `src/Host/Middleware/JwtMiddleware.cs`
- Modify: `KoiFengShuiSystem.Api/Authorization/JwtMiddleware.cs`
- Modify: `KoiFengShuiSystem.Api/Controllers/AuthController.cs`
- Modify: `tests/UnitTests/Identity/AccountServiceTests.cs`

- [ ] **Step 1: Update the Host JWT middleware to use the Identity JWT port**

Replace `src/Host/Middleware/JwtMiddleware.cs` with:

```csharp
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using Microsoft.Extensions.Logging;
using System.Net;

namespace KoiFengShuiSystem.Host.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtMiddleware> _logger;

    public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, IAccountService accountService, IJwtTokenService jwtTokenService)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var accountId = jwtTokenService.ValidateJwtToken(token);
                if (accountId != null)
                {
                    context.Items["Account"] = await accountService.GetByIdAsync(accountId.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating JWT token");
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Invalid token");
                return;
            }
        }

        await _next(context);
    }
}
```

- [ ] **Step 2: Update the legacy API JWT middleware to use the same port**

Replace `KoiFengShuiSystem.Api/Authorization/JwtMiddleware.cs` with the same logic, using the `KoiFengShuiSystem.Api.Authorization` namespace.

- [ ] **Step 3: Update AuthController to use IJwtTokenService instead of IJwtUtils**

In `KoiFengShuiSystem.Api/Controllers/AuthController.cs`, change the dependency field and constructor parameter from `IJwtUtils` to `IJwtTokenService`:

```csharp
private readonly IJwtTokenService _jwtTokenService;

public AuthController(
    IAccountService accountService,
    IJwtTokenService jwtTokenService,
    IHttpClientFactory httpClientFactory,
    ILogger<AuthController> logger)
{
    _accountService = accountService;
    _jwtTokenService = jwtTokenService;
    _httpClientFactory = httpClientFactory;
    _logger = logger;
}
```

Then replace the Google login token generation line with:

```csharp
var token = _jwtTokenService.GenerateJwtToken(account);
```

- [ ] **Step 4: Update the account-service unit test helpers to use IJwtTokenService**

In `tests/UnitTests/Identity/AccountServiceTests.cs`, replace the old `IJwtUtils` usages:

```csharp
var jwt = jwtTokenService ?? Mock.Of<IJwtTokenService>(j => j.GenerateJwtToken(It.IsAny<AccountEntity>()) == "test-token");
```

Replace the valid-authentication test setup with:

```csharp
var jwtMock = new Mock<IJwtTokenService>();
jwtMock.Setup(j => j.GenerateJwtToken(It.IsAny<AccountEntity>())).Returns("generated-jwt-token");
var service = CreateService(context, jwtTokenService: jwtMock.Object);
```

- [ ] **Step 5: Run Identity unit tests after removing legacy JWT usage**

Run: `dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~Identity"`

Expected: Identity unit tests pass without `IJwtUtils` in the Identity flow.

---

### Task 6: Move Identity Controllers Into Identity.Api

**Files:**
- Create: `src/Modules/Identity/Identity.Api/Controllers/AuthController.cs`
- Create: `src/Modules/Identity/Identity.Api/Controllers/AccountController.cs`
- Delete: `KoiFengShuiSystem.Api/Controllers/AuthController.cs`
- Delete: `KoiFengShuiSystem.Api/Controllers/AccountController.cs`
- Modify: `src/Modules/Identity/Identity.Api/Identity.Api.csproj`
- Modify: `src/Host/Host.csproj`
- Create: `tests/IntegrationTests/IdentityApiContractTests.cs`

- [ ] **Step 1: Ensure Identity.Api can compile moved controllers with the existing authorization attributes**

Add these project references to `src/Modules/Identity/Identity.Api/Identity.Api.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\Identity.Application\Identity.Application.csproj" />
  <ProjectReference Include="..\Identity.Infrastructure\Identity.Infrastructure.csproj" />
  <ProjectReference Include="..\Identity.Domain\Identity.Domain.csproj" />
  <ProjectReference Include="..\..\..\..\KoiFengShuiSystem.Api\KoiFengShuiSystem.Api.csproj" />
  <ProjectReference Include="..\..\..\..\KoiFengShuiSystem.Shared\KoiFengShuiSystem.Shared.csproj" />
</ItemGroup>
```

This keeps the move minimal by reusing `AuthorizeAttribute`, `AllowAnonymousAttribute`, and `GoogleUserInfo` without redesigning those cross-cutting pieces in this checkpoint.

- [ ] **Step 2: Move AuthController into Identity.Api**

Create `src/Modules/Identity/Identity.Api/Controllers/AuthController.cs` with the same action bodies and route attributes as the current controller, but using this namespace and using block:

```csharp
using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Responses;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using AccountEntity = KoiFengShuiSystem.Modules.Identity.Domain.Entities.Account;

namespace KoiFengShuiSystem.Modules.Identity.Api.Controllers;
```

Keep these route and auth attributes unchanged:

```csharp
[ApiController]
[Authorize]
[Route("api/[controller]")]
```

- [ ] **Step 3: Move AccountController into Identity.Api**

Create `src/Modules/Identity/Identity.Api/Controllers/AccountController.cs` with the current action bodies and route attributes, but using this namespace and using block:

```csharp
using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using Microsoft.AspNetCore.Mvc;
using IdentityAccountService = KoiFengShuiSystem.Modules.Identity.Application.Services.AccountService;

namespace KoiFengShuiSystem.Modules.Identity.Api.Controllers;
```

- [ ] **Step 4: Remove the old API controller files after the new ones are present**

Delete these files:

```text
KoiFengShuiSystem.Api/Controllers/AuthController.cs
KoiFengShuiSystem.Api/Controllers/AccountController.cs
```

- [ ] **Step 5: Add an integration contract test for Identity Swagger routes**

Create `tests/IntegrationTests/IdentityApiContractTests.cs`:

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

- [ ] **Step 6: Ensure Host references Identity.Api**

In `src/Host/Host.csproj`, keep or add:

```xml
<ProjectReference Include="..\Modules\Identity\Identity.Api\Identity.Api.csproj" />
```

- [ ] **Step 7: Build and run the integration tests after the controller move**

Run: `dotnet build KoiFengShuiSystem.sln --no-restore`

Expected: build succeeds with the moved controllers.

Run: `dotnet test tests/IntegrationTests/IntegrationTests.csproj --no-build`

Expected: integration tests pass, including the new Identity route contract test.

---

### Task 7: Final Verification, Schema Drift Check, And Docs

**Files:**
- Modify: `docs/architecture/current-dependencies.md`
- Modify: `docs/refactor-baseline.md`

- [ ] **Step 1: Run restore after all code changes**

Run: `dotnet restore KoiFengShuiSystem.sln`

Expected: exit code `0` with only existing package vulnerability warnings.

- [ ] **Step 2: Run the final build**

Run: `dotnet build KoiFengShuiSystem.sln --no-restore`

Expected: exit code `0`.

- [ ] **Step 3: Run the final tests**

Run: `dotnet test KoiFengShuiSystem.sln --no-build`

Expected: all unit and integration tests pass.

- [ ] **Step 4: Verify zero schema drift**

Run: `dotnet ef migrations add VerifyIdentityPhase6FinishNoSchemaChange --project KoiFengShuiSystem.DataAccess --startup-project src/Host`

Expected: generated migration `Up()` and `Down()` methods are empty.

- [ ] **Step 5: Remove the temporary verification migration**

Run: `dotnet ef migrations remove --project KoiFengShuiSystem.DataAccess --startup-project src/Host`

Expected: temporary migration is removed cleanly.

- [ ] **Step 6: Update the dependency inventory document**

Append this section to `docs/architecture/current-dependencies.md`:

```md
## Phase 6 Identity Finish Notes

- `Account` and `Role` are now owned by `src/Modules/Identity/Identity.Domain/Entities/`.
- Identity request/response DTOs and `IAccountService` now live in `src/Modules/Identity/Identity.Application/`.
- `Identity.Application` no longer references `KoiFengShuiSystem.DataAccess` or `FengShui.Domain`; Feng Shui element reads are bridged through `IIdentityElementLookup`.
- Identity persistence, JWT behavior, and legacy email bridging now live in `src/Modules/Identity/Identity.Infrastructure/`.
- `IdentityModuleInstaller` registers Identity services through module installer discovery.
- `AuthController` and `AccountController` are now served from `src/Modules/Identity/Identity.Api/Controllers/`.
- Host controller discovery includes the Identity.Api assembly.
- Temporary boundary remains: `LegacyIdentityEmailSender` still adapts the existing `EmailService` until Notifications extraction.
```

- [ ] **Step 7: Update the refactor baseline document**

Append this section to `docs/refactor-baseline.md`:

```md
## Phase 6 Identity Finish Verification

| Command | Exit status | Outcome |
| --- | ---: | --- |
| `dotnet restore KoiFengShuiSystem.sln` | 0 | Restore succeeded with the same existing vulnerability warnings. |
| `dotnet build KoiFengShuiSystem.sln --no-restore` | 0 | Build succeeded after completing the Identity module finish work. |
| `dotnet test KoiFengShuiSystem.sln --no-build` | 0 | Unit and integration tests passed after Identity controller move and JWT boundary cleanup. |
| `dotnet ef migrations add VerifyIdentityPhase6FinishNoSchemaChange --project KoiFengShuiSystem.DataAccess --startup-project src/Host` | 0 | Verification migration generated empty `Up()` and `Down()` methods. |
| `dotnet ef migrations remove --project KoiFengShuiSystem.DataAccess --startup-project src/Host` | 0 | Temporary verification migration removed cleanly. |
```

- [ ] **Step 8: Review the final diff footprint**

Run: `git diff --stat`

Expected: only intended Identity Phase 6 finish files changed, and `KoiFengShuiSystem_Documentation.md` is not modified.

---

## Self-Review Checklist

- Identity JWT generation and validation are covered by Tasks 1 and 2.
- Identity application decoupling from `DataAccess` and `FengShui.Domain` is covered by Task 3.
- Installer-based DI registration and store cleanup are covered by Task 4.
- Middleware and controller migration off `IJwtUtils` are covered by Task 5.
- Controller move and route contract verification are covered by Task 6.
- Build, tests, schema drift, and docs are covered by Task 7.
- No placeholder markers remain in this plan.

Suggested commit messages if the user later asks for commits:

- `refactor(identity): move jwt boundary into module`
- `refactor(identity): remove application data coupling`
- `refactor(identity): move api surface`
- `docs(identity): record phase 6 finish verification`
