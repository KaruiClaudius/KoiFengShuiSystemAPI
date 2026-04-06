# KoiFengShuiSystem - Project Research & Improvement Analysis

## 1. Project Overview

**KoiFengShuiSystem** is an ASP.NET Core Web API that combines **Eastern Feng Shui principles** with **Koi fish keeping recommendations**. The system calculates a user's Feng Shui element based on their birth year and gender (using the Vietnamese/Chinese **Cung Phi** method), then provides personalized recommendations for Koi fish breeds, pond shapes, directions, colors, and quantities.

### Tech Stack
- **Framework**: ASP.NET Core (Minimal API + Controllers)
- **ORM**: Entity Framework Core with SQL Server
- **Authentication**: JWT Bearer tokens
- **Payment Gateway**: PayOS (Vietnamese payment provider)
- **Cloud Storage**: Cloudinary (image uploads)
- **Architecture**: Clean Architecture (Api / Services / DataAccess / Common / Shared)
- **Containerization**: Docker + Docker Compose

---

## 2. Current Architecture

```
KoiFengShuiSystem/
├── KoiFengShuiSystem.Api/          # Entry point, controllers, middleware
├── KoiFengShuiSystem.Services/     # Business logic, service implementations
├── KoiFengShuiSystem.DataAccess/   # EF Core models, repositories, Unit of Work
├── KoiFengShuiSystem.Shared/       # DTOs, helpers, shared utilities
└── KoiFengShuiSystem.Common/       # Constants, shared enums
```

### Database Entities (18 tables)
| Entity | Purpose |
|--------|---------|
| **Account** | User accounts with DOB, gender, element, wallet, role |
| **Element** | Five Elements (Kim, Mộc, Thủy, Hỏa, Thổ) with lucky numbers |
| **KoiBreed** | Koi breeds linked to elements, countries, colors |
| **FengShuiDirection** | Direction-element compatibility mappings |
| **ShapeCategory** | Pond shape-element compatibility mappings |
| **FishPond** | Pond configurations (shape + direction) |
| **Recommendation** | User-specific Koi + pond recommendations |
| **Post** | Community posts with categories and images |
| **MarketplaceListing** | Koi marketplace with tiers and categories |
| **Transaction** | Payment records |
| **SubcriptionTier** | Subscription tiers for marketplace |
| **Follow** | Post following system |
| **FAQ** | User-submitted questions |
| **Image** | Centralized image storage |
| **Direction** | Cardinal directions |
| **Country** | Koi origin countries |
| **Role** | User roles |
| **TrafficLog** | API traffic analytics |

---

## 3. Current Features

### 3.1 Feng Shui Consultation
- **Cung Phi Calculation**: Determines user's element (Kim/Mộc/Thủy/Hỏa/Thổ) from birth year + gender
- **Element-based Recommendations**: Suggests compatible Koi breeds, colors, pond shapes, directions, and lucky numbers
- **Compatibility Assessment**: Scores a user's pond setup (direction, shape, colors, quantity) on a 0-100 scale

### 3.2 User Management
- Registration, login, JWT authentication
- Google OAuth login
- Password reset via email
- Profile management with element assignment

### 3.3 Social Features
- Community posts with images and categories
- Follow system for posts
- Admin post management

### 3.4 Marketplace
- Koi fish listings with categories and subscription tiers
- Image uploads via Cloudinary
- Transaction processing via PayOS
- Transaction sync background service

### 3.5 Admin Dashboard
- Dashboard analytics (transaction counts, revenue)
- Traffic logging middleware
- FAQ management

---

## 4. Current Limitations & Issues

### 4.1 Feng Shui Engine Limitations
1. **Only uses Cung Phi (birth year + gender)** - Ignores:
   - Full birth date (month, day, hour) for Ba Zi (Four Pillars) analysis
   - Chinese Zodiac animal year
   - Home/house facing direction
   - Kua number variations
   - Flying Stars (Xuan Kong Fei Xing) annual influences
   - Personal Day Master (日主) from Ba Zi

2. **Binary scoring system** - Direction/shape compatibility is 0 or 100, no gradient
3. **No elemental interaction logic** - Missing the Five Element cycle (生克 - generating/controlling relationships)
4. **No annual/yearly Feng Shui updates** - Flying Stars change annually
5. **No home environment analysis** - Doesn't consider house layout, room placement

### 4.2 Code Quality Issues
1. **Duplicate Cung Phi logic** in `CompatibilityService` and `ConsultationService` (DRY violation)
2. **Console.WriteLine** used for logging instead of proper ILogger
3. **Hardcoded Vietnamese text** in responses (no localization support)
4. **AllowAnyOrigin CORS** - Security risk in production
5. **Nullable reference types disabled** (`#nullable disable`)
6. **No input validation attributes** on request models
7. **No rate limiting** on API endpoints
8. **No caching** for frequently accessed Feng Shui data

### 4.3 Missing Features
1. No user Feng Shui profile/dashboard
2. No personalized daily/weekly/monthly horoscope
3. No element balance analysis for the user
4. No compatibility between users (relationship Feng Shui)
5. No home/property Feng Shui assessment
6. No auspicious date/time selection
7. No Feng Shui remedies/cures suggestions
8. No progress tracking for Feng Shui improvements

---

## 5. Improvement Recommendations

### 5.1 Core Feng Shui Engine Enhancements

#### A. Ba Zi (Four Pillars of Destiny) System
**Priority: HIGH**

Implement a full **Ba Zi (八字)** engine that analyzes:
- **Year Pillar** (年柱): Ancestors, early life
- **Month Pillar** (月柱): Parents, career, youth
- **Day Pillar** (日柱): Self (Day Master), spouse, middle age
- **Hour Pillar** (时柱): Children, later life, hidden talents

**New Models Needed:**
```
BaZiProfile
├── AccountId (FK)
├── BirthYear, BirthMonth, BirthDay, BirthHour
├── YearHeavenlyStem, YearEarthlyBranch
├── MonthHeavenlyStem, MonthEarthlyBranch
├── DayHeavenlyStem (Day Master), DayEarthlyBranch
├── HourHeavenlyStem, HourEarthlyBranch
├── DayMasterElement
├── StrongestElement, WeakestElement
├── FavorableElements[]
├── UnfavorableElements[]
└── ElementStrengthScores (Kim, Mộc, Thủy, Hỏa, Thổ)
```

**Benefits:**
- Much more personalized than Cung Phi alone
- Identifies user's **Day Master** (core self element)
- Calculates element balance/imbalance
- Determines favorable/unfavorable elements for the individual

#### B. Five Element Interaction Engine
**Priority: HIGH**

Implement the complete **生克 (Generating/Controlling) cycle**:
```
Generating Cycle (相生): Mộc → Hỏa → Thổ → Kim → Thủy → Mộc
Controlling Cycle (相克): Mộc → Thổ → Thủy → Hỏa → Kim → Mộc
```

This should power:
- User's element strength analysis
- Which elements to strengthen/weaken
- Compatible color/material recommendations
- Career, relationship, health insights

#### C. Chinese Zodiac Integration
**Priority: MEDIUM**

Add the 12 Chinese Zodiac animals (Rat, Ox, Tiger, Rabbit, Dragon, Snake, Horse, Goat, Monkey, Rooster, Dog, Pig) with:
- Zodiac-element combinations (60-year cycle)
- Zodiac compatibility between users
- Yearly zodiac fortune predictions
- Best/worst years for decisions

#### D. Flying Stars (Xuan Kong Fei Xing) Annual System
**Priority: MEDIUM**

Annual Flying Stars change positions each year. Implement:
- Current year's star chart
- Auspicious/inauspicious sectors for the year
- Monthly star updates
- Placement recommendations for Koi ponds based on annual stars

---

### 5.2 User-Centric Features (Beyond Koi Fish)

#### A. Personal Feng Shui Dashboard
**Priority: HIGH**

A comprehensive user status page showing:
- **Element Profile**: Current element balance with visual chart
- **Lucky Elements**: Colors, numbers, directions, shapes, materials
- **Unlucky Elements**: What to avoid
- **Daily Fortune**: Based on daily heavenly stems/earthly branches
- **Monthly/Yearly Outlook**: Trending fortune predictions
- **Health, Wealth, Career, Relationship** scores based on element balance

#### B. Life Aspect Recommendations
**Priority: HIGH**

Extend beyond Koi fish to cover all life aspects:

| Life Aspect | Recommendations |
|-------------|----------------|
| **Wealth** | Lucky directions for desk/office, wealth corner activation, money colors |
| **Career** | Best industries for element, office placement, career colors |
| **Health** | Foods for element balance, exercise directions, health colors |
| **Relationships** | Compatibility with partner's element, romance directions, love colors |
| **Home** | Room placement, furniture arrangement, decor colors |
| **Travel** | Auspicious travel directions, best travel dates |

#### C. Element Balance Analysis
**Priority: HIGH**

Visual representation of user's five element distribution:
- Radar chart showing element strengths
- Identifies dominant and deficient elements
- Suggests remedies (colors, materials, activities, directions)
- Tracks element balance over time as user makes changes

#### D. User Compatibility System
**Priority: MEDIUM**

Feng Shui compatibility between users:
- Element compatibility scoring (generating vs. controlling)
- Zodiac compatibility
- Combined Ba Zi analysis
- Relationship strength meter
- Suggestions for harmonizing elements

---

### 5.3 Home & Property Feng Shui

#### A. Home Assessment Tool
**Priority: MEDIUM**

Users can input their property details:
- House facing direction
- Floor plan layout
- Room positions
- Door and window placements
- Water feature locations (including Koi ponds)

System provides:
- Overall home Feng Shui score
- Room-by-room analysis
- Bagua map overlay
- Specific improvement suggestions
- Koi pond optimal placement within property

#### B. Koi Pond Designer
**Priority: MEDIUM**

Interactive pond planning:
- Visual pond shape selector
- Direction placement optimizer
- Fish quantity calculator based on element
- Color combination recommender
- Size and depth guidelines
- Integration with home assessment

---

### 5.4 Time-Based Features

#### A. Auspicious Date/Time Selector
**Priority: MEDIUM**

Chinese Almanac (通胜/黄历) integration:
- Best dates for specific activities (opening business, moving, starting projects)
- Auspicious hours within each day
- Days to avoid
- Integration with user's personal Ba Zi

#### B. Periodic Fortune Updates
**Priority: MEDIUM**

- **Daily Fortune**: Based on daily stem/branch vs. user's Day Master
- **Monthly Fortune**: Monthly pillar interactions
- **Yearly Fortune**: Annual pillar + Flying Stars
- Push notifications for significant days

#### C. Feng Shui Calendar
**Priority: LOW**

- Chinese lunar calendar integration
- Festival and auspicious day markers
- Personal lucky days highlighting
- Seasonal element shift notifications

---

### 5.5 AI & Advanced Features

#### A. AI-Powered Feng Shui Analysis
**Priority: MEDIUM**

- **Photo Analysis**: Users upload photos of their space, AI analyzes Feng Shui
- **Chat-based Consultation**: AI Feng Shui consultant for Q&A
- **Personalized Reports**: AI-generated comprehensive Feng Shui reports
- **Image Recognition**: Identify Feng Shui objects and their placement quality

#### B. Community & Social Features
**Priority: LOW**

- User success stories and testimonials
- Before/after Feng Shui transformations
- Community forum for Feng Shui discussions
- Expert consultant directory
- User-generated content moderation

#### C. Gamification
**Priority: LOW**

- Feng Shui mastery levels
- Achievement badges (e.g., "Element Balancer", "Harmony Seeker")
- Daily check-in streaks
- Progress tracking for Feng Shui improvements
- Social sharing of achievements

---

### 5.6 Technical Improvements

#### A. Architecture Refactoring
1. **Extract Cung Phi calculation** into a shared utility class (remove duplication)
2. **Implement CQRS pattern** for complex queries
3. **Add MediatR** for clean command/query separation
4. **Implement proper logging** with Serilog or similar
5. **Add health checks** endpoint

#### B. Performance
1. **Add Redis caching** for Feng Shui reference data (elements, directions, shapes)
2. **Implement response caching** for consultation endpoints
3. **Add database indexes** on frequently queried columns
4. **Optimize N+1 queries** with proper EF Core Include patterns

#### C. Security
1. **Restrict CORS** to specific origins
2. **Add rate limiting** per IP/user
3. **Implement refresh token** rotation
4. **Add API versioning**
5. **Enable nullable reference types**

#### D. Testing
1. **Unit tests** for Feng Shui calculation engine
2. **Integration tests** for API endpoints
3. **Load testing** for marketplace endpoints
4. **Test coverage reporting**

#### E. API Improvements
1. **Add OpenAPI/Swagger annotations** for all endpoints
2. **Implement consistent error response format**
3. **Add pagination** to all list endpoints
4. **Add filtering and sorting** parameters
5. **Implement API versioning**

---

## 6. Recommended Implementation Roadmap

### Phase 1: Foundation (Weeks 1-4)
- [ ] Refactor duplicate Cung Phi logic
- [ ] Implement proper logging
- [ ] Add input validation
- [ ] Fix CORS configuration
- [ ] Add caching layer
- [ ] Write unit tests for existing Feng Shui engine

### Phase 2: Ba Zi Engine (Weeks 5-10)
- [ ] Implement Heavenly Stems and Earthly Branches calculations
- [ ] Build Ba Zi profile generation from full birth datetime
- [ ] Create element balance analysis
- [ ] Add favorable/unfavorable element determination
- [ ] Build User Feng Shui Dashboard API

### Phase 3: User-Centric Features (Weeks 11-16)
- [ ] Personal Feng Shui Dashboard
- [ ] Life aspect recommendations (wealth, career, health, relationships)
- [ ] Element balance visualization
- [ ] User compatibility system
- [ ] Daily fortune calculations

### Phase 4: Advanced Feng Shui (Weeks 17-22)
- [ ] Flying Stars annual system
- [ ] Chinese Zodiac integration
- [ ] Auspicious date/time selector
- [ ] Home/property assessment tool
- [ ] Koi Pond Designer

### Phase 5: AI & Polish (Weeks 23-28)
- [ ] AI photo analysis integration
- [ ] AI chat consultant
- [ ] Periodic fortune notifications
- [ ] Gamification system
- [ ] Performance optimization
- [ ] Comprehensive testing

---

## 7. New Database Schema Additions (Proposed)

```sql
-- Ba Zi Profile
CREATE TABLE BaZiProfiles (
    ProfileId INT PRIMARY KEY IDENTITY,
    AccountId INT FOREIGN KEY REFERENCES Accounts(AccountId),
    BirthDateTime DATETIME2 NOT NULL,
    YearStem NVARCHAR(10), YearBranch NVARCHAR(10),
    MonthStem NVARCHAR(10), MonthBranch NVARCHAR(10),
    DayStem NVARCHAR(10), DayBranch NVARCHAR(10),
    HourStem NVARCHAR(10), HourBranch NVARCHAR(10),
    DayMasterElement NVARCHAR(10),
    ElementKimScore DECIMAL(5,2),
    ElementMocScore DECIMAL(5,2),
    ElementThuyScore DECIMAL(5,2),
    ElementHoaScore DECIMAL(5,2),
    ElementThoScore DECIMAL(5,2),
    FavorableElements NVARCHAR(100),
    UnfavorableElements NVARCHAR(100),
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

-- User Life Recommendations
CREATE TABLE UserLifeRecommendations (
    RecommendationId INT PRIMARY KEY IDENTITY,
    AccountId INT FOREIGN KEY REFERENCES Accounts(AccountId),
    Category NVARCHAR(50), -- Wealth, Career, Health, Relationship, Home
    RecommendationType NVARCHAR(50), -- Color, Direction, Material, Activity
    Value NVARCHAR(100),
    Priority INT,
    Description NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

-- User Compatibility
CREATE TABLE UserCompatibility (
    CompatibilityId INT PRIMARY KEY IDENTITY,
    User1Id INT FOREIGN KEY REFERENCES Accounts(AccountId),
    User2Id INT FOREIGN KEY REFERENCES Accounts(AccountId),
    ElementScore DECIMAL(5,2),
    ZodiacScore DECIMAL(5,2),
    OverallScore DECIMAL(5,2),
    Analysis NVARCHAR(1000),
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

-- Property Assessment
CREATE TABLE PropertyAssessments (
    AssessmentId INT PRIMARY KEY IDENTITY,
    AccountId INT FOREIGN KEY REFERENCES Accounts(AccountId),
    FacingDirection NVARCHAR(50),
    PropertyType NVARCHAR(50),
    FloorPlan NVARCHAR(MAX),
    OverallScore DECIMAL(5,2),
    Analysis NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

-- Daily Fortunes
CREATE TABLE DailyFortunes (
    FortuneId INT PRIMARY KEY IDENTITY,
    AccountId INT FOREIGN KEY REFERENCES Accounts(AccountId),
    FortuneDate DATE,
    WealthScore INT,
    CareerScore INT,
    HealthScore INT,
    RelationshipScore INT,
    LuckyColor NVARCHAR(50),
    LuckyDirection NVARCHAR(50),
    LuckyNumber INT,
    Analysis NVARCHAR(1000),
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

-- Flying Stars Annual
CREATE TABLE FlyingStarsAnnual (
    StarId INT PRIMARY KEY IDENTITY,
    Year INT,
    Direction NVARCHAR(50),
    StarNumber INT,
    StarName NVARCHAR(50),
    Element NVARCHAR(10),
    IsAuspicious BIT,
    Description NVARCHAR(500),
    Remedies NVARCHAR(500)
);
```

---

## 8. Key Feng Shui Concepts to Implement

### Five Elements (五行) Deep Integration
| Element | Colors | Shapes | Directions | Seasons | Organs | Emotions |
|---------|--------|--------|------------|---------|--------|----------|
| **Kim (Metal)** | White, Gold, Silver | Round, Oval | West, NW | Autumn | Lungs | Grief |
| **Mộc (Wood)** | Green, Teal | Rectangular | East, SE | Spring | Liver | Anger |
| **Thủy (Water)** | Black, Blue | Wavy, Irregular | North | Winter | Kidneys | Fear |
| **Hỏa (Fire)** | Red, Orange, Purple | Triangular | South | Summer | Heart | Joy |
| **Thổ (Earth)** | Yellow, Brown | Square, Flat | Center, NE, SW | Late Summer | Spleen | Worry |

### Heavenly Stems (天干) - 10 Stems
1. 甲 (Giáp) - Yang Wood
2. 乙 (Ất) - Yin Wood
3. 丙 (Bính) - Yang Fire
4. 丁 (Đinh) - Yin Fire
5. 戊 (Mậu) - Yang Earth
6. 己 (Kỷ) - Yin Earth
7. 庚 (Canh) - Yang Metal
8. 辛 (Tân) - Yin Metal
9. 壬 (Nhâm) - Yang Water
10. 癸 (Quý) - Yin Water

### Earthly Branches (地支) - 12 Branches
1. 子 (Tý) - Rat
2. 丑 (Sửu) - Ox
3. 寅 (Dần) - Tiger
4. 卯 (Mão) - Cat/Rabbit
5. 辰 (Thìn) - Dragon
6. 巳 (Tỵ) - Snake
7. 午 (Ngọ) - Horse
8. 未 (Mùi) - Goat
9. 申 (Thân) - Monkey
10. 酉 (Dậu) - Rooster
11. 戌 (Tuất) - Dog
12. 亥 (Hợi) - Pig

---

## 9. Summary

The current KoiFengShuiSystem is a solid foundation but is **limited to surface-level Feng Shui** (Cung Phi only) and **focused primarily on Koi fish recommendations**. The biggest opportunity is to transform this into a **comprehensive Eastern Feng Shui platform** that:

1. **Analyzes the user holistically** using Ba Zi (Four Pillars), not just birth year
2. **Provides life-wide recommendations** covering wealth, career, health, relationships, and home
3. **Offers time-based guidance** with daily fortunes, auspicious dates, and annual Flying Stars
4. **Enables property assessment** for complete home Feng Shui analysis
5. **Builds community** through compatibility, sharing, and expert consultations

The technical foundation is clean and well-structured, making it straightforward to extend with these features.
