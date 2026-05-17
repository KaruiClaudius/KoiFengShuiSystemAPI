# Current Dependency Inventory

Generated for Task 0.2 of the architecture refactor plan. This inventory reflects the current layered solution before module extraction.

## Solution Caveat

`KoiFengShuiSystem.Tests/KoiFengShuiSystem.Tests.csproj` exists and references production projects, but `KoiFengShuiSystem.sln` currently includes only `Api`, `BusinessLogic`, `DataAccess`, `Shared`, and `Common`. Builds of the solution do not build the test project until it is added to the solution.

## Project References

| Project | Project References |
|---|---|
| `KoiFengShuiSystem.Api` | `KoiFengShuiSystem.Services/KoiFengShuiSystem.BusinessLogic.csproj`; `KoiFengShuiSystem.Shared/KoiFengShuiSystem.Shared.csproj` |
| `KoiFengShuiSystem.Services` (`KoiFengShuiSystem.BusinessLogic.csproj`) | `KoiFengShuiSystem.Common/KoiFengShuiSystem.Common.csproj`; `KoiFengShuiSystem.DataAccess/KoiFengShuiSystem.DataAccess.csproj`; `KoiFengShuiSystem.Shared/KoiFengShuiSystem.Shared.csproj` |
| `KoiFengShuiSystem.DataAccess` | None |
| `KoiFengShuiSystem.Shared` | `KoiFengShuiSystem.DataAccess/KoiFengShuiSystem.DataAccess.csproj` |
| `KoiFengShuiSystem.Common` | `KoiFengShuiSystem.DataAccess/KoiFengShuiSystem.DataAccess.csproj` |
| `KoiFengShuiSystem.Tests` | `KoiFengShuiSystem.Common/KoiFengShuiSystem.Common.csproj`; `KoiFengShuiSystem.Services/KoiFengShuiSystem.BusinessLogic.csproj`; `KoiFengShuiSystem.Shared/KoiFengShuiSystem.Shared.csproj` |

Temporary exception: `Shared.Kernel` currently references EF Core only to preserve `PaginatedList<T>.CreateAsync(IQueryable<T>)` behavior; revisit when EF-specific pagination moves to infrastructure or query handlers.

The project references above under-report source-level couplings. `KoiFengShuiSystem.Api` compiles through transitive references and directly imports types from projects it does not reference in its `.csproj`, including `KoiFengShuiSystem.Common` and `KoiFengShuiSystem.DataAccess`. It also imports concrete service implementation types through `Program.cs` and selected controllers. These couplings should be treated as real dependencies during module extraction, even when the `.csproj` only lists `Services` and `Shared`.

Examples:

| API source | Direct source-level dependency | Why it matters |
|---|---|---|
| `KoiFengShuiSystem.Api/Controllers/UploadImageController.cs` | `KoiFengShuiSystem.Common.Const` | API controller reaches into shared/common constants through a transitive project reference. |
| `KoiFengShuiSystem.Api/Controllers/MarketplaceListingsController.cs` | `GenericRepository<Account>` from `KoiFengShuiSystem.DataAccess.Base` and `KoiFengShuiSystem.DataAccess.Models` | API controller bypasses service/application boundaries and directly depends on EF data-access types. |
| `KoiFengShuiSystem.Api/Controllers/AuthController.cs` | `SecurityUtil` from service implementation namespace | API controller directly uses an implementation utility rather than an interface/application contract. |
| `KoiFengShuiSystem.Api/Program.cs` | Concrete service and repository implementation types such as `AccountService`, `DashboardService`, `UnitOfWorkRepository`, `GenericRepository<>`, `CloudService`, and `TransactionSyncService` | Host startup currently binds the API project to concrete implementation assemblies and data-access infrastructure. |

## Controllers And Injected Dependencies

| Controller | Injected dependencies | Target module |
|---|---|---|
| `AccountController` | `IAccountService`, `ILogger<AccountService>` | `Identity` |
| `AdminPostController` | `IAdminPostService`, `ICloudService`, `ILogger<AdminPostController>` | `Community` with upload dependency from `Shared.Infrastructure` |
| `AuthController` | `IAccountService`, `IJwtUtils`, `IHttpClientFactory`, `ILogger<AuthController>` | `Identity` |
| `CompatibilityController` | `ICompatibilityService` | `FengShui` |
| `ConsultationController` | `IConsultationService` | `FengShui` |
| `DashboardController` | `IDashboardService` | `Admin` |
| `ElementController` | `IElementService` | `FengShui` |
| `FAQController` | `IFAQService`, `ILogger<FAQController>` | `Admin` |
| `MarketplaceListingsController` | `IMarketplaceListingService`, `ILogger<MarketplaceListingsController>`, `IHttpContextAccessor`, `GenericRepository<Account>` | `Marketplace` with direct `Identity` data dependency to remove |
| `MarketCategoryController` | `IMarketCategoryService` | `Marketplace` |
| `PostController` | `IPostService`, `ILogger<PostController>` | `Community` |
| `SubcriptionTiersController` | `ISubcriptionTiersService` | `Marketplace` |
| `TransactionController` | `ITransactionService`, `IHttpContextAccessor` | `Payments` |
| `UploadImageController` | `ICloudService`, `IImageService` | `Shared.Infrastructure` initially |

## Service Interfaces And Implementations

| Interface | Implementation | Notes |
|---|---|---|
| `IAccountService` | `AccountService` | Uses `IJwtUtils`, account and element repositories, email, and logging. |
| `IAdminPostImageService` | `AdminPostImageService` | Uses `KoiFengShuiContext` directly. |
| `IAdminPostService` | `AdminPostService` | Uses `KoiFengShuiContext` directly and `IImageService`. |
| `ICloudService` | `CloudService` | Cloudinary integration through `CloundSettings`. |
| `ICompatibilityService` | `CompatibilityService` | Uses several Feng Shui repositories and `CungPhiCalculator`. |
| `IConsultationService` | `ConsultationService` | Uses element, koi breed, shape category, and Feng Shui direction repositories. |
| `IDashboardService` | `DashboardService` | Uses account, traffic log, marketplace listing, transaction, and subscription tier repositories. |
| `IElementService` | `ElementService` | Uses `UnitOfWorkRepository`. |
| `IFAQService` | `FAQService` | Uses `KoiFengShuiContext` directly. |
| `IImageService` | `ImageService` | Uses `KoiFengShuiContext`, `IConfiguration`, and `UnitOfWorkRepository`. |
| `IJwtUtils` | `JwtUtils` | Interface and implementation are in `JwtUtils.cs`. |
| `IMarketCategoryService` | `MarketCategoryService` | Uses `UnitOfWorkRepository`. |
| `IMarketplaceListingService` | `MarketplaceListingService` | Uses `UnitOfWorkRepository`, `GenericRepository<Account>`, and `ICloudService`. |
| `IPostService` | `PostService` | Uses `UnitOfWorkRepository`. |
| `ISubcriptionTiersService` | `SubcriptionTiersService` | Uses `UnitOfWorkRepository`. |
| `ITransactionService` | `TransactionService` | Uses account, marketplace listing, transaction repositories, PayOS, unit of work, and logging. |
| `IUnitOfWorkRepository` | `UnitOfWorkRepository` | Interface exposes only transaction-oriented save methods; concrete implementation exposes repository properties. |

Additional concrete services without matching public service interfaces:

| Concrete type | Registered or used as | Notes |
|---|---|---|
| `AdminAccountService` | Scoped concrete service | Depends on `IAccountService` and `IConfiguration`. |
| `EmailService` | Scoped concrete service | Depends on `IOptions<MailSettings>` and optional `ILogger<EmailService>`. |
| `TransactionSyncService` | Hosted service | Background PayOS reconciliation through `IServiceProvider`, `ILogger<TransactionSyncService>`, and `PayOS`. |
| `SecurityUtil` | Static utility style class | No service interface discovered. |

`UnitOfWorkRepository` usage is inconsistent: several services inject the concrete `UnitOfWorkRepository` to access repository properties, while `TransactionService` injects `IUnitOfWorkRepository` and uses only save transaction methods. This makes the concrete type part of the de facto application dependency surface even though the interface does not expose those repositories.

## EF Entities

Entities exposed by `KoiFengShuiContext`:

| Entity | Primary module ownership | Navigation summary |
|---|---|---|
| `Account` | `Identity` | Belongs to optional `Element` and `Role`; owns collections of `FAQ`, `Follow`, `MarketplaceListing`, `Post`, `Recommendation`, `TrafficLog`, and `Transaction`. |
| `Country` | `FengShui` | Owns `KoiBreed` collection. |
| `Direction` | `FengShui` | Owns `FengShuiDirection` collection. |
| `Element` | `FengShui` | Central lookup for `Account`, `FengShuiDirection`, `KoiBreed`, `MarketplaceListing`, `Post`, and `ShapeCategory`. |
| `FAQ` | `Admin` | Belongs to `Account`. |
| `FengShuiDirection` | `FengShui` | Joins `Direction` and `Element`; owns `FishPond` collection. |
| `FishPond` | `FengShui` | Belongs to `FengShuiDirection` and `ShapeCategory`; owns `Recommendation` collection. |
| `Follow` | `Community` | Joins `Account` and `Post`. |
| `Image` | `Shared.Infrastructure` initially | Owns `ListingImage` and `PostImage` collections. |
| `KoiBreed` | `FengShui` | Belongs to `Country` and `Element`; owns `Recommendation` collection. |
| `ListingImage` | `Marketplace` | Joins `Image` and `MarketplaceListing`. |
| `MarketCategory` | `Marketplace` | Owns `MarketplaceListing` collection. |
| `MarketplaceListing` | `Marketplace` | Belongs to `Account`, `MarketCategory`, optional `Element`, and `SubcriptionTier`; owns `ListingImage` and `Transaction` collections. |
| `Post` | `Community` | Belongs to `Account`, optional `Element`, and `PostCategory` through `IdNavigation`; owns `Follow` and `PostImage` collections. |
| `PostCategory` | `Community` | Owns `Post` collection. |
| `PostImage` | `Community` | Joins `Image` and `Post`. |
| `Recommendation` | `FengShui` | Joins `Account`, `KoiBreed`, and `FishPond`. |
| `Role` | `Identity` | Owns `Account` collection. |
| `ShapeCategory` | `FengShui` | Belongs to optional `Element`; owns `FishPond` collection. |
| `SubcriptionTier` | `Marketplace` | Owns `MarketplaceListing` and `Transaction` collections. |
| `TrafficLog` | `Admin` | Belongs to optional `Account`. |
| `Transaction` | `Payments` | Belongs to `Account`, optional `MarketplaceListing`, and optional `SubcriptionTier`. |

Navigation-heavy aggregate relationships to protect during refactor:

| Aggregate root or hub | Navigation-heavy relationships | Refactor risk |
|---|---|---|
| `Account` | Crosses `Identity`, `Admin`, `Community`, `Marketplace`, `FengShui`, and `Payments` through many collections. | Avoid moving account entity behind a module boundary before cross-module read contracts exist. |
| `Element` | Referenced by `Account`, Feng Shui lookups, marketplace listings, posts, koi breeds, and shape categories. | Treat as a Feng Shui domain lookup with temporary shared read access during early phases. |
| `MarketplaceListing` | Connects seller account, category, tier, element, listing images, and transactions. | Split marketplace catalog from payments only after preserving listing/payment query behavior. |
| `Post` | Connects author account, element, category, follows, and images. | Community extraction must preserve image and identity lookups. |
| `Recommendation` | Joins account, koi breed, and fish pond recommendation state. | Feng Shui extraction must define whether recommendations remain user-owned or Feng Shui-owned. |
| `Transaction` | Links account, marketplace listing, subscription tier, PayOS integration, and dashboard reporting. | Payments extraction must keep reporting dependencies visible to Admin dashboards. |

## Dependency Table

| Source | Depends On | Reason | Target Phase |
|---|---|---|---|
| `KoiFengShuiSystem.Api` | `KoiFengShuiSystem.Services`, `KoiFengShuiSystem.Shared` | Direct `.csproj` references for host controllers, startup wiring, service interfaces, and DTOs. | Phase 1 introduces host/shared foundations; later phases move controllers by module. |
| `KoiFengShuiSystem.Api` source files | `KoiFengShuiSystem.Common`, `KoiFengShuiSystem.DataAccess`, concrete service implementation types | API source uses transitive dependencies directly: `UploadImageController` uses `Const`, `MarketplaceListingsController` injects `GenericRepository<Account>`, `AuthController` uses `SecurityUtil`, and `Program.cs` registers concrete implementations. | Phase 1/2 make host composition explicit; phases 3+ remove controller-level data access and implementation utility couplings. |
| `KoiFengShuiSystem.Services` | `KoiFengShuiSystem.DataAccess` | Services directly use EF entities, repositories, and `KoiFengShuiContext`. | Phase 3+ module extraction and infrastructure ownership. |
| `KoiFengShuiSystem.Services` | `KoiFengShuiSystem.Shared` | Service contracts and mappings use current request/response DTOs. | Phase 2 contract protection, then move DTOs near modules. |
| `KoiFengShuiSystem.Services` | `KoiFengShuiSystem.Common` | Feng Shui calculations use `CungPhiCalculator`. | Phase 1 shared foundations; Feng Shui module extraction later. |
| `KoiFengShuiSystem.Shared` | `KoiFengShuiSystem.DataAccess` | DTO layer currently references data models and request helpers include repository-like types. | Phase 1/2 untangle shared kernel from data access before module DTO moves. |
| `KoiFengShuiSystem.Common` | `KoiFengShuiSystem.DataAccess` | Common project references data access, even though current calculator tests target pure logic. | Phase 1 move pure primitives to `Shared.Kernel`; remove data access coupling when safe. |
| Controllers | Service interfaces | API layer delegates behavior to business services. | Phase 1 module installer shell; phases 3+ move controllers into module API projects. |
| `MarketplaceListingsController` | `GenericRepository<Account>` | Controller bypasses service boundary to access accounts. | Phase 2/3 add behavior coverage, then move account access behind application/service contract. |
| `Program.cs` | Concrete services and repositories | Manual DI registrations bind all current layers in the host. | Phase 1 module installer contract and duplicate-registration cleanup. |
| `AccountService` | `GenericRepository<Account>`, `GenericRepository<Element>`, `EmailService`, `IJwtUtils` | Auth/profile behavior mixes identity, Feng Shui element calculation, and notification delivery. | Phase 3 Identity extraction with Notifications and Feng Shui read contracts. |
| `CompatibilityService` | Feng Shui repositories, `CungPhiCalculator` | Core compatibility behavior combines element, direction, shape, breed, pond, and recommendation data. | Feng Shui module extraction phase. |
| `ConsultationService` | Feng Shui repositories | Consultation response depends on element, koi breed, shape, and direction data. | Feng Shui module extraction phase. |
| `MarketplaceListingService` | `UnitOfWorkRepository`, `GenericRepository<Account>`, `ICloudService` | Listing workflow touches marketplace persistence, identity account data, and cloud images. | Marketplace extraction after shared upload contract exists. |
| `TransactionService` | PayOS, transaction/account/listing repositories, unit of work | Payment behavior depends on external PayOS and multiple data aggregates. | Payments extraction after contract tests. |
| `TransactionSyncService` | `IServiceProvider`, `KoiFengShuiContext`, PayOS | Hosted reconciliation uses scoped database access and external payment state. | Payments infrastructure extraction. |
| `DashboardService` | Account, traffic log, marketplace listing, transaction, subscription tier repositories | Admin dashboard aggregates data across modules. | Admin extraction after read-model contracts exist. |
| `FAQService`, `AdminPostService`, `AdminPostImageService`, `ImageService` | `KoiFengShuiContext` directly | These services bypass repository abstractions. | Infrastructure cleanup during module extraction. |
| `IUnitOfWorkRepository` | Save transaction methods only | Interface supports transaction commits but does not expose repository properties. | Phase 3+ decide whether transaction boundary remains shared or becomes module-owned. |
| Concrete `UnitOfWorkRepository` | Specific repositories and `KoiFengShuiContext` | Concrete repository composition exposes repositories for posts, listings, images, elements, categories, and tiers; several services depend on this concrete type. | Phase 3+ replace concrete dependency with module-owned repositories or EF configurations. |
| EF entities | `KoiFengShuiContext` | One shared DbContext owns all entities and relationships. | Keep shared DbContext initially; split configurations before any context split. |
| `KoiFengShuiSystem.Tests` | `Common`, `Services`, `Shared` | Existing tests compile outside the solution and therefore are not covered by solution build/test commands. | Phase 2 test harness and solution inclusion. |
