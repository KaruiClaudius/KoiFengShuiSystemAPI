# KoiFengShuiSystem – Complete Project Documentation

> **Version:** 2.0
> **Last updated:** April 2026
> **Status:** Production / Active Development
> **Purpose:** A single source of truth for the entire KoiFengShuiSystem platform.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Tech Stack & Architecture](#2-tech-stack--architecture)
3. [Directory Structure](#3-directory-structure)
4. [Features](#4-features)
   - [4.1 Current Features](#41-current-features)
   - [4.2 Planned / Upcoming Features](#42-planned--upcoming-features)
5. [Database Schema](#5-database-schema)
   - [5.1 Entity Relationship Diagram](#51-entity-relationship-diagram)
   - [5.2 Table Definitions](#52-table-definitions)
   - [5.3 Seed Data](#53-seed-data)
6. [API Reference](#6-api-reference)
   - [6.1 Authentication & Account](#61-authentication--account)
   - [6.2 Feng Shui Consultation](#62-feng-shui-consultation)
   - [6.3 Compatibility](#63-compatibility)
   - [6.4 Admin Dashboard](#64-admin-dashboard)
   - [6.5 Elements & Directions](#65-elements--directions)
   - [6.6 FAQ](#66-faq)
   - [6.7 Community Posts](#67-community-posts)
   - [6.8 Marketplace](#68-marketplace)
   - [6.9 Transactions & Subscriptions](#69-transactions--subscriptions)
   - [6.10 Image Upload](#610-image-upload)
   - [6.11 Admin Post Management](#611-admin-post-management)
7. [Authentication & Security](#7-authentication--security)
8. [Key Business Logic – Feng Shui Engine](#8-key-business-logic--feng-shui-engine)
   - [8.1 Cung Phi Calculation (Current)](#81-cung-phi-calculation-current)
   - [8.2 Five Elements Interactions](#82-five-elements-interactions)
   - [8.3 Ba Zi (Four Pillars of Destiny) – Planned](#83-ba-zi-four-pillars-of-destiny--planned)
   - [8.4 Chinese Zodiac – Planned](#84-chinese-zodiac--planned)
   - [8.5 Flying Stars – Planned](#85-flying-stars--planned)
9. [Services & Background Jobs](#9-services--background-jobs)
10. [Middleware](#10-middleware)
11. [Configuration & Environment Variables](#11-configuration--environment-variables)
12. [Setup & Installation](#12-setup--installation)
    - [12.1 Prerequisites](#121-prerequisites)
    - [12.2 Local Development](#122-local-development)
    - [12.3 Docker Compose](#123-docker-compose)
13. [Deployment](#13-deployment)
14. [Testing](#14-testing)
15. [Known Limitations & Technical Debt](#15-known-limitations--technical-debt)
16. [Roadmap](#16-roadmap)
17. [Contributing](#17-contributing)
18. [License & Acknowledgements](#18-license--acknowledgements)

---

## 1. Project Overview

**KoiFengShuiSystem** is an ASP.NET Core Web API that merges **Eastern Feng Shui wisdom** with **Koi fish keeping recommendations**. The system calculates the user's innate Feng Shui element using the Vietnamese/Chinese **Cung Phi** method (birth year + gender) and provides personalised advice on Koi breeds, pond shapes, directions, colours, and quantities. It goes beyond simple element matching to create a holistic digital ecosystem: community posts, a full marketplace with payment integration, and an admin dashboard.

The long-term vision (detailed in the roadmap) is to evolve into a **comprehensive Eastern metaphysics platform** incorporating Ba Zi (Four Pillars), Chinese Zodiac, Flying Stars, home assessment, and AI-powered consultation.

---

## 2. Tech Stack & Architecture

- **Framework:** ASP.NET Core (Minimal API + Controllers) – .NET 8
- **Architecture:** Modular Monolith
  - The application is structured as a single deployable unit composed of **loosely coupled, independently organized feature modules**. Each module owns its domain, application logic, data access, and API surface. Modules communicate through well-defined **internal contracts (interfaces / in-process messaging)** rather than direct class references, keeping boundaries clean while avoiding distributed-systems complexity.

  | Layer / Concept | Responsibility |
  |---|---|
  | `Host` | ASP.NET Core entry point (`Program.cs`), middleware pipeline, module registration |
  | `Modules/*` | One folder per bounded context (see Directory Structure) |
  | `Module.Api` | Controllers and endpoint definitions for that module |
  | `Module.Application` | Use cases, CQRS commands/queries, service interfaces |
  | `Module.Domain` | Entities, value objects, domain events, business rules |
  | `Module.Infrastructure` | EF Core repositories, external integrations, background jobs |
  | `Shared.Kernel` | Cross-cutting contracts: base entities, result types, pagination, guard clauses |
  | `Shared.Infrastructure` | Shared EF Core DbContext, JWT, email, Cloudinary, PayOS clients |

  Modules are registered via `IModuleInstaller` convention; each module wires its own DI, EF entities, and route groups at startup.

- **Database:** SQL Server + Entity Framework Core (Code First, one shared DbContext with per-module `IEntityTypeConfiguration` files)
- **In-Process Messaging:** MediatR (commands, queries, domain events between modules)
- **Authentication:** JWT Bearer tokens, Google OAuth
- **Payment Gateway:** PayOS (Vietnamese domestic provider)
- **Cloud Storage:** Cloudinary (images)
- **Caching:** MemoryCache (built-in), Redis planned
- **Documentation:** Swagger / OpenAPI
- **Containerisation:** Docker + Docker Compose

---

## 3. Directory Structure

```
KoiFengShuiSystem/
├── src/
│   ├── Host/                                  # ASP.NET Core entry point
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Middleware/
│   │       ├── JwtMiddleware.cs
│   │       ├── TrafficLoggingMiddleware.cs
│   │       └── ExceptionMiddleware.cs
│   │
│   ├── Modules/
│   │   ├── Identity/                          # Auth, accounts, roles
│   │   │   ├── Identity.Api/
│   │   │   │   └── Controllers/AuthController.cs
│   │   │   ├── Identity.Application/
│   │   │   │   ├── Commands/
│   │   │   │   └── Queries/
│   │   │   ├── Identity.Domain/
│   │   │   │   └── Entities/Account.cs
│   │   │   └── Identity.Infrastructure/
│   │   │       └── Repositories/
│   │   │
│   │   ├── FengShui/                          # Consultation, element engine
│   │   │   ├── FengShui.Api/
│   │   │   ├── FengShui.Application/
│   │   │   │   └── Services/
│   │   │   │       ├── ConsultationService.cs
│   │   │   │       ├── CompatibilityService.cs
│   │   │   │       └── CungPhiCalculator.cs   # Shared calculator (extracted)
│   │   │   ├── FengShui.Domain/
│   │   │   │   └── Entities/
│   │   │   │       ├── Element.cs
│   │   │   │       ├── KoiBreed.cs
│   │   │   │       ├── Direction.cs
│   │   │   │       ├── FengShuiDirection.cs
│   │   │   │       ├── ShapeCategory.cs
│   │   │   │       └── FishPond.cs
│   │   │   └── FengShui.Infrastructure/
│   │   │
│   │   ├── Community/                         # Posts, follows, images
│   │   │   ├── Community.Api/
│   │   │   ├── Community.Application/
│   │   │   ├── Community.Domain/
│   │   │   │   └── Entities/Post.cs
│   │   │   └── Community.Infrastructure/
│   │   │
│   │   ├── Marketplace/                       # Listings, tiers, categories
│   │   │   ├── Marketplace.Api/
│   │   │   ├── Marketplace.Application/
│   │   │   ├── Marketplace.Domain/
│   │   │   │   └── Entities/
│   │   │   │       ├── MarketplaceListing.cs
│   │   │   │       └── SubcriptionTier.cs
│   │   │   └── Marketplace.Infrastructure/
│   │   │       └── BackgroundJobs/TransactionSyncService.cs
│   │   │
│   │   ├── Payments/                          # PayOS transactions
│   │   │   ├── Payments.Api/
│   │   │   ├── Payments.Application/
│   │   │   ├── Payments.Domain/
│   │   │   └── Payments.Infrastructure/
│   │   │
│   │   ├── Admin/                             # Dashboard, moderation, FAQ
│   │   │   ├── Admin.Api/
│   │   │   ├── Admin.Application/
│   │   │   └── Admin.Infrastructure/
│   │   │
│   │   └── Notifications/                     # Email, future push (planned)
│   │       └── ...
│   │
│   ├── Shared/
│   │   ├── Shared.Kernel/                     # Domain primitives, result type, pagination
│   │   │   ├── Entities/BaseEntity.cs
│   │   │   ├── Results/Result.cs
│   │   │   └── Guards/Guard.cs
│   │   └── Shared.Infrastructure/             # EF DbContext, JWT, Cloudinary, PayOS, Email
│   │       ├── Persistence/KoiFengShuiDbContext.cs
│   │       └── Integrations/
│
├── tests/
│   ├── UnitTests/
│   └── IntegrationTests/
│
├── docker-compose.yml
├── Dockerfile
└── README.md
```

### Module Boundary Rules

- A module **must not** reference another module's internal classes directly.
- Cross-module communication happens **only** via:
  - MediatR commands/queries published to the shared pipeline.
  - Contracts defined in `Shared.Kernel` (interfaces, DTOs, events).
- Each module registers its own EF entity configurations; the shared `DbContext` aggregates them at startup via reflection or explicit registration.

---

## 4. Features

### 4.1 Current Features

- **Feng Shui Consultation**
  - Cung Phi element calculation (birth year + gender)
  - Element-based Koi breed, colour, pond shape, direction recommendations
  - Lucky numbers per element
  - Pond compatibility scoring (0–100)
- **User Management**
  - Registration / login / JWT
  - Google OAuth
  - Password reset via email
  - Profile (DOB, gender, wallet, element)
- **Community**
  - Posts with categories and images
  - Follow/unfollow system
  - Admin moderation
- **Marketplace**
  - Koi listings with categories, subscription tiers, images
  - Cloudinary upload
  - PayOS payment / transaction tracking
  - Background transaction sync service
- **Admin Dashboard**
  - Revenue analytics, transaction counts
  - Traffic logging middleware
  - FAQ management
- **Infrastructure**
  - Response caching, memory caching
  - Swagger UI
  - Docker support

### 4.2 Planned / Upcoming Features

| Feature | Priority | Description |
|---|---|---|
| Ba Zi (Four Pillars) Engine | 🔴 HIGH | Full birth datetime analysis (Year/Month/Day/Hour pillars), Day Master, element balance |
| Five Element Interactions | 🔴 HIGH | Generating/Controlling cycles, element strength scoring |
| Personal Feng Shui Dashboard | 🔴 HIGH | Radar chart of element balance, lifetime recommendations |
| Life Aspect Recommendations | 🔴 HIGH | Wealth, career, health, relationships based on element profile |
| User Compatibility | 🟡 MEDIUM | Element & zodiac harmony between two users |
| Chinese Zodiac Integration | 🟡 MEDIUM | 12-animal cycle, yearly fortune, 60-year stem-branch combinations |
| Flying Stars (Xuan Kong) | 🟡 MEDIUM | Annual star chart, auspicious directions, pond placement advice |
| Auspicious Date/Time Selector | 🟡 MEDIUM | Chinese Almanac (通胜), user-personalised lucky days |
| Home / Property Assessment | 🟡 MEDIUM | House facing direction, room placement, Bagua overlay |
| AI Feng Shui Consultant | 🟢 LOW | Photo analysis, chat-based advice, automated reports |

---

## 5. Database Schema

### 5.1 Entity Relationship Diagram

```
                    ┌─────────────────────┐
                    │        Role         │
                    │ - RoleId (PK)       │
                    │ - RoleName          │
                    └──────────┬──────────┘
                               │ 1
                               │
                    ┌──────────▼──────────┐
                    │       Account       │
                    │ - AccountId (PK)    │
                    │ - FullName, Email   │
                    │ - Dob, Gender       │
                    │ - ElementId (FK)    │
                    │ - RoleId (FK)       │
                    │ - Wallet            │
                    └──┬──────┬───────┬───┘
                       │      │       │
          ┌────────────┘      │       └──────────────┐
          │                   │                      │
    ┌─────▼─────┐      ┌─────▼─────┐         ┌──────▼──────┐
    │   Post    │      │    FAQ    │         │ Transaction │
    │ - PostId  │      │ - FAQId   │         │ - Id        │
    │ - Title   │      │ - Question│         │ - Amount    │
    │ - Content │      │ - Answer  │         │ - ListingId │
    │ - Account │      │ - Account │         │ - TierId    │
    └─────┬─────┘      └───────────┘         └──────┬──────┘
          │                                         │
    ┌─────▼──────┐                         ┌───────▼──────┐
    │ PostImage  │                         │  Marketplace │
    │ -ImageId   │                         │   Listing    │
    └─────┬──────┘                         └───────┬──────┘
          │                                         │
    ┌─────▼─────┐                           ┌──────▼──────┐
    │   Image   │                           │ ListingImage│
    │ (Cloud)   │                           └──────┬──────┘
    └───────────┘                                  │
                                              ┌────▼──────┐
                                              │ Subscript. │
                                              │   Tier     │
                                              └────────────┘
                    ┌──────────────────┐
                    │    Element       │
                    │ - ElementId (PK) │
                    │ - ElementName    │
                    │ - LuckyNumber    │
                    └──┬──────┬──────┬─┘
                       │      │      │
          ┌────────────┘      │      └────────────┐
          │                   │                   │
    ┌─────▼──────┐    ┌───────▼──────┐    ┌───────▼──────┐
    │  KoiBreed  │    │ FengShuiDir. │    │ ShapeCategory│
    │ - BreedId  │    │ - DirectionId│    │ - ShapeId    │
    │ - Color    │    │ - ElementId  │    │ - ElementId  │
    │ - Country  │    └──────┬───────┘    └──────┬───────┘
    └─────┬──────┘           │                   │
          │                  │                   │
    ┌─────▼──────────────────▼───────────────────▼─────┐
    │                Recommendation                    │
    │ - AccountId, BreedId, PondId, CreatedAt         │
    └─────────────────────────────────────────────────┘

Additional tables: Country, Direction, MarketCategory, Follow, TrafficLog
```

### 5.2 Table Definitions

#### Account

| Column | Type | Constraints | Notes |
|---|---|---|---|
| AccountId | int | PK, IDENTITY | |
| FullName | nvarchar(50) | NOT NULL | |
| Email | nvarchar(100) | NOT NULL, UNIQUE | |
| Password | nvarchar(100) | NULL | Hashed |
| Dob | datetime | NULL | Used for Cung Phi & Ba Zi |
| Phone | nvarchar(20) | NULL | |
| Gender | nvarchar(10) | NULL | Male / Female / Other |
| ElementId | int | NULL, FK → Element | Assigned after calculation |
| RoleId | int | NULL, FK → Role | |
| CreateAt | datetime | NOT NULL | |
| UpdateAt | datetime | NOT NULL | |
| Wallet | decimal(18,0) | NULL | Balance for marketplace |

#### Element (Five Elements)

| Column | Type | Notes |
|---|---|---|
| ElementId | int | PK, IDENTITY |
| ElementName | nvarchar(50) | Kim, Mộc, Thủy, Hỏa, Thổ |
| Description | nvarchar(100) | |
| LuckyNumber | nvarchar(50) | e.g. "1,6,7,8" |

#### KoiBreed

| Column | Type | FK |
|---|---|---|
| BreedId | int | PK |
| ElementId | int | → Element |
| CountryId | int | → Country |
| BreedName | nvarchar(50) | |
| Color | nvarchar(20) | |
| Description | nvarchar(100) | |

#### Direction

| Column | Type |
|---|---|
| DirectionId | int PK |
| DirectionName | nvarchar(50) – e.g. North, Southwest |

#### FengShuiDirection

| Column | Type | FK |
|---|---|---|
| Id | int PK | |
| DirectionId | int | → Direction |
| ElementId | int | → Element |
| Description | nvarchar(100) | |

#### ShapeCategory

| Column | Type | FK |
|---|---|---|
| ShapeId | int PK | |
| ShapeName | nvarchar(50) | Round, Rectangular, Wavy, Square |
| Description | nvarchar(100) | |
| ElementId | int (nullable) | → Element |

#### FishPond

| Column | Type | FK |
|---|---|---|
| PondId | int PK | |
| ShapeId | int | → ShapeCategory |
| DirectionPlacement | int | → FengShuiDirection |

#### Recommendation

| Column | Type | FK |
|---|---|---|
| RecommendationId | int PK | |
| AccountId | int | → Account |
| BreedId | int | → KoiBreed |
| PondId | int | → FishPond |
| CreatedAt | datetime | |

#### Post

| Column | Type | FK |
|---|---|---|
| PostId | int PK | |
| Id (CategoryId) | int | → PostCategory |
| Name | nvarchar(255) | |
| Description | nvarchar(255) | |
| AccountId | int | → Account |
| ElementId | int (nullable) | → Element |
| Status | nvarchar(50) | |
| CreateAt/UpdateAt | datetime | |

*PostCategory, PostImage, Image, Follow* – see ERD.

#### MarketplaceListing

| Column | Type | FK |
|---|---|---|
| ListingId | int PK | |
| AccountId | int | → Account |
| TierId | int | → SubcriptionTier |
| Title / Description | nvarchar(MAX) | |
| Price | decimal(10,2) | |
| Quantity | int | |
| CategoryId | int | → MarketCategory |
| Color | nvarchar(20) | |
| ElementId | int (nullable) | → Element |
| ExpiresAt | datetime | |
| IsActive / Status | bit / nvarchar | |

*MarketCategory, ListingImage, SubcriptionTier, Transaction* – see ERD.

#### TrafficLog

| Column | Type | FK |
|---|---|---|
| Id | int PK | |
| Timestamp | datetime | |
| IsRegistered | bit | |
| AccountId | int (nullable) | → Account |
| IpAddress / UserAgent | nvarchar | |
| RequestPath / Method | nvarchar | |

Remaining tables follow the same pattern; see `PROJECT_DOCUMENTATION.md` for the full list.

### 5.3 Seed Data

- **Elements:** Kim (Metal), Mộc (Wood), Thủy (Water), Hỏa (Fire), Thổ (Earth) with lucky numbers.
- **Directions:** 8 cardinal/intercardinal directions.
- **Shapes:** Round (Kim), Rectangular (Mộc), Wavy (Thủy), Square (Thổ).
- **Roles:** Admin, User.
- **Countries:** Japan, China, Thailand, Indonesia, Vietnam, USA.
- **Post Categories:** General, Koi Care, Feng Shui, Pond Construction, Success Stories.
- **Market Categories:** Koi Fish, Pond Equipment, Pond Plants, Accessories.
- **Subscription Tiers:** Basic (5 listings/month, 30 days), Premium (20 listings, 90 days), VIP (unlimited, 365 days).

---

## 6. API Reference

> Base URL: `https://api.koifengshui.com/v1`
> All endpoints require `Authorization: Bearer <token>` unless marked `[Public]`.

### 6.1 Authentication & Account

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Register new user (returns JWT) |
| POST | `/api/auth/login` | Public | Email/password login |
| POST | `/api/auth/google-login` | Public | Google OAuth callback |
| POST | `/api/auth/forgot-password` | Public | Send reset link |
| POST | `/api/auth/reset-password` | Public | Reset with token |
| GET | `/api/accounts/{id}` | User/Admin | Get profile |
| PUT | `/api/accounts/{id}` | User | Update profile (includes DOB, gender) |
| GET | `/api/accounts/{id}/element` | User | Current element details |

### 6.2 Feng Shui Consultation

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/consultation/element?year=1995&gender=Male` | Calculate element from birth year + gender (returns element, lucky number, etc.) |
| GET | `/api/consultation/recommendations?accountId=1` | Personalised Koi breed, colour, pond shape, direction recommendations based on stored element |
| POST | `/api/consultation` | Generate and save recommendation for user |

### 6.3 Compatibility

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/compatibility/score` | Input: User's existing pond (shape, direction, colours, quantity). Output: 0–100 score. |

### 6.4 Admin Dashboard

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/dashboard/overview` | Admin | Total transactions, revenue, active listings |
| GET | `/api/dashboard/traffic-logs` | Admin | Recent API traffic |

### 6.5 Elements & Directions

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/elements` | All elements |
| GET | `/api/elements/{id}` | Element details |
| GET | `/api/directions` | All directions |
| GET | `/api/directions/compatible/{elementId}` | Directions compatible with element |

### 6.6 FAQ

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/faq` | Public | Public FAQ list |
| POST | `/api/faq` | User/Admin | Submit a question |
| PUT | `/api/faq/{id}` | Admin | Answer a question |

### 6.7 Community Posts

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/posts` | Public | Paginated post feed |
| GET | `/api/posts/{id}` | Public | Single post |
| POST | `/api/posts` | User | Create post (with category) |
| PUT | `/api/posts/{id}` | Owner/Admin | Edit post |
| DELETE | `/api/posts/{id}` | Owner/Admin | Delete post |
| POST | `/api/posts/{id}/images` | Owner | Attach images (Cloudinary) |
| POST | `/api/posts/{id}/follow` | User | Toggle follow |
| GET | `/api/posts/{id}/followers` | Public | Get follower list |

### 6.8 Marketplace

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/marketplace/listings` | Public | Search/filter listings |
| GET | `/api/marketplace/listings/{id}` | Public | Detail view |
| POST | `/api/marketplace/listings` | User | Create listing (select tier) |
| PUT | `/api/marketplace/listings/{id}` | Owner | Update |
| DELETE | `/api/marketplace/listings/{id}` | Owner/Admin | Remove |
| GET | `/api/marketplace/categories` | Public | List market categories |
| GET | `/api/marketplace/tiers` | Public | Subscription tiers info |
| POST | `/api/marketplace/listings/{id}/images` | Owner | Upload images |

### 6.9 Transactions & Subscriptions

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/transactions/create` | User | Initiate PayOS payment for tier/listing |
| POST | `/api/transactions/webhook` | PayOS | PayOS callback (internal) |
| GET | `/api/transactions/history` | User | User's transaction log |

### 6.10 Image Upload

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/upload/image` | User | Direct Cloudinary upload (returns URL) |

### 6.11 Admin Post Management

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/admin/posts/pending` | Admin | Pending posts |
| PUT | `/api/admin/posts/{id}/approve` | Admin | Approve / reject |

---

## 7. Authentication & Security

- **JWT Settings:** Defined in `appsettings.json` – secret key, issuer, audience, token lifetime (default 60 min).
- **Password:** Hashed with BCrypt.
- **Google OAuth:** External login callback registered at `/signin-google`.
- **CORS:** Policy `AllowedOrigins` configurable per environment. *Currently allows any origin in development; must be locked down before production.*
- **Rate Limiting:** Not yet implemented. Recommended to add ASP.NET Core rate limiting middleware.
- **Input Validation:** Uses data annotations on DTOs (e.g., `[Required]`, `[EmailAddress]`). A validation filter is active globally.
- **Refresh Tokens:** Not yet implemented; planned for Phase 3.

---

## 8. Key Business Logic – Feng Shui Engine

### 8.1 Cung Phi Calculation (Current)

The Cung Phi method determines a person's element based on **birth year and gender**. Implementation steps:

1. Sum the last two digits of the birth year.
2. Divide by 9, take the remainder (`r`).
3. Look up the "magic number" for the gender:
   - **Male:** Use a fixed mapping table (remainder → number).
   - **Female:** Different mapping.
4. Convert the resulting number to one of the Five Elements (Kim, Mộc, Thủy, Hỏa, Thổ).

> **Refactoring note:** The logic has been extracted into the shared `FengShui.Application/Services/CungPhiCalculator.cs` utility, eliminating the previous duplication across `CompatibilityService` and `ConsultationService`.

### 8.2 Five Elements Interactions

Even though not yet fully used, the system seeds the five elements and can later implement:

- **Generating cycle (相生):** Wood → Fire → Earth → Metal → Water → Wood
- **Controlling cycle (相克):** Wood → Earth → Water → Fire → Metal → Wood

These relationships will power element balance analysis and advanced recommendations.

### 8.3 Ba Zi (Four Pillars of Destiny) – Planned

The planned BaZi engine will calculate the four pillars from full birth datetime (year, month, day, hour) using:

- **Heavenly Stems (10):** 甲, 乙, 丙, 丁, 戊, 己, 庚, 辛, 壬, 癸
- **Earthly Branches (12):** 子, 丑, 寅, 卯, 辰, 巳, 午, 未, 申, 酉, 戌, 亥

Each pillar yields a stem-branch pair. The Day Stem becomes the user's **Day Master (日主)**, representing their core element. Element strengths of all pillars are then aggregated to determine favourable/unfavourable elements.

### 8.4 Chinese Zodiac – Planned

The 12 earthly branches correspond to zodiac animals. A 60-year cycle combines a Heavenly Stem and an Earthly Branch (e.g., 2024 is 甲辰 – Yang Wood Dragon). This enables zodiac compatibility and yearly forecasts.

### 8.5 Flying Stars – Planned

Each year, nine stars (1–9) fly to different sectors of the house. Their positions are determined by the year's Lo Shu square and the current 20-year period. The system will calculate annual star charts and recommend pond placement in favourable sectors.

---

## 9. Services & Background Jobs

All services live inside their respective module's `Application` or `Infrastructure` layer and are registered at startup via `IModuleInstaller`.

| Service | Module | Notes |
|---|---|---|
| `ConsultationService` | FengShui | Main element recommendation engine |
| `CompatibilityService` | FengShui | Pond compatibility scoring |
| `CungPhiCalculator` | FengShui | Shared utility; extracted from duplicated code |
| `UserService` | Identity | Account profile and element assignment |
| `PostService` | Community | CRUD + business rules |
| `MarketplaceService` | Marketplace | Listing management |
| `TransactionService` | Payments | Payment initiation, PayOS sync |
| `TransactionSyncService` | Marketplace.Infrastructure | Hosted background service – periodically queries PayOS and reconciles transaction status |
| `ImageService` | Shared.Infrastructure | Cloudinary upload logic |

All services are registered via dependency injection (Scoped/Transient/Singleton as appropriate per module).

---

## 10. Middleware

Middleware lives in `Host/Middleware/` and applies globally across all modules.

| Middleware | Purpose |
|---|---|
| `JwtMiddleware` | Validates JWT on every request, attaches user context |
| `TrafficLoggingMiddleware` | Logs every API call into the `TrafficLog` table |
| `ExceptionMiddleware` | Global exception handler returning consistent error envelopes (active) |

---

## 11. Configuration & Environment Variables

`appsettings.json` structure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=KoiFengShui;..."
  },
  "Jwt": {
    "Key": "your-256-bit-secret",
    "Issuer": "KoiApi",
    "Audience": "KoiClient",
    "ExpireMinutes": 60
  },
  "AllowedOrigins": "http://localhost:3000,https://yourfrontend.com",
  "PayOS": {
    "ClientId": "...",
    "ApiKey": "...",
    "ChecksumKey": "..."
  },
  "Cloudinary": {
    "CloudName": "...",
    "ApiKey": "...",
    "ApiSecret": "..."
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "Username": "...",
    "Password": "..."
  }
}
```

All sensitive values are overridden via environment variables in production (`secrets.json` or Docker secrets).

---

## 12. Setup & Installation

### 12.1 Prerequisites

- .NET 8 SDK
- SQL Server (local or Docker)
- Docker (optional)
- Cloudinary account
- PayOS sandbox account

### 12.2 Local Development

```bash
# Clone repo
git clone https://github.com/your-org/KoiFengShuiSystem.git
cd KoiFengShuiSystem

# Restore packages
dotnet restore

# Update connection string in appsettings.Development.json
# Apply migrations
dotnet ef database update --project src/Shared/Shared.Infrastructure

# Run API
dotnet run --project src/Host

# Browse Swagger: https://localhost:5001/swagger
```

### 12.3 Docker Compose

```bash
# Start SQL Server + API
docker-compose up -d

# The API will be available on http://localhost:8080
```

`docker-compose.yml` includes:

- `db` service (`mcr.microsoft.com/mssql/server`)
- `api` service (build from Dockerfile)
- Environment variables set in `docker-compose.override.yml`

---

## 13. Deployment

- **Container registry:** Build Docker image, push to Docker Hub / Azure Container Registry.
- **Orchestration:** Deployable on Azure App Service, AWS ECS, or Kubernetes.
- **Database:** Use Azure SQL or managed SQL Server.
- **Background services:** `TransactionSyncService` runs inside the same Host process. Because the architecture is a modular monolith, extracting it to a separate worker process later requires only moving the `Marketplace.Infrastructure` background job project — no distributed messaging changes needed.

---

## 14. Testing

- **Unit Tests:** xUnit + Moq. Focus on service layer and module application logic, especially Feng Shui calculations.
- **Integration Tests:** ASP.NET Core `WebApplicationFactory` with per-module test fixtures against an in-memory or SQL Server test database.
- **Module isolation tests:** Each module's application layer is tested independently by mocking the `Shared.Kernel` contracts, ensuring no hidden cross-module dependencies.
- **Run tests:**

  ```bash
  dotnet test
  ```

- **Test coverage:** Aim for >80% on core logic. Currently sparse; expanding is a priority.

---

## 15. Known Limitations & Technical Debt

1. **Console.WriteLine** used instead of structured logging (planned: Serilog).
2. **Hardcoded Vietnamese strings** – no localization support yet.
3. **CORS** permissive in dev; must be tightened before production.
4. **Nullable reference types disabled** in several files (`#nullable disable`).
5. **No rate limiting** – potential for abuse.
6. **Caching limited** – Redis is planned for large-scale deployments.
7. **No API versioning** – future changes may break clients.
8. **MediatR pipeline behaviours** (logging, validation, transactions) not yet wired up globally.
9. **Module boundary enforcement** is currently by convention only; a Roslyn analyzer or ArchUnit-style test should be added to catch violations at CI time.

---

## 16. Roadmap

| Phase | Timeline | Deliverables |
|---|---|---|
| **1 – Foundation** | Weeks 1–4 | Complete modular monolith restructure, Serilog, global exception middleware, CORS lockdown, rate limiting, unit tests |
| **2 – Ba Zi Engine** | Weeks 5–10 | Heavenly Stems/Branches, BaZi profiles, element balance, new DB tables inside `FengShui` module |
| **3 – User Dashboard** | Weeks 11–16 | Personal dashboard, life recommendations, element radar chart, compatibility (new `Dashboard` module) |
| **4 – Advanced Feng Shui** | Weeks 17–22 | Flying Stars, Chinese Zodiac, auspicious dates, home assessment, pond designer |
| **5 – AI & Polish** | Weeks 23–28 | AI photo analysis, chat consultant, notifications module, gamification, performance tuning, Redis caching |

---

## 17. Contributing

1. Fork the project.
2. Create a feature branch (`feature/your-feature`).
3. Commit changes following [Conventional Commits](https://www.conventionalcommits.org/).
4. **Do not reference another module's internal types directly.** Use `Shared.Kernel` contracts or MediatR.
5. Open a pull request with a detailed description.
6. Ensure all tests pass and new functionality is covered.

---

## 18. License & Acknowledgements

- **License:** MIT
- **Feng Shui references:** Classical texts, Vietnamese/Chinese metaphysical traditions.
- **Third-party services:** Cloudinary, PayOS, Google OAuth.
- **Icon/logo:** *(insert credits)*

---

*This document is the central source of truth for the KoiFengShuiSystem project. For any discrepancies, refer to the codebase and update this file accordingly.*
