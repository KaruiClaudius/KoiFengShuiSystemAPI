# KoiFengShuiSystem — Frontend API Guide

> **Audience:** frontend developers building UIs against this backend.
> **Status:** matches `master` @ .NET 10 build (2026-08-23).
> **Base URL:** `https://localhost:7285` (dev) · all bodies are JSON unless noted · camelCase serialization.

---

## 0. What's new since the last frontend release

| # | Feature | Why you care |
|---|---------|--------------|
| 1 | **Refresh-token auth flow** (`POST /api/Auth/refresh`, `POST /api/Auth/logout`) | Access tokens now live **15 minutes**. You MUST implement silent refresh + 401 retry. |
| 2 | **Passwordless Google login** + `GET /api/Auth/profile-status` | Google users get no password & possibly no DOB/gender — an onboarding gate is expected. |
| 3 | **Token-based password reset** (`ForgotPassword` emails a link `/reset-password?token=…`) | New frontend page required; no more emailed passwords. |
| 4 | **Partner Shops directory** (`api/partner-shops`) | New public browsing surface replacing the deleted marketplace. |
| 5 | **Member-created posts enter a `Pending` moderation queue** | Post feed shows approved content only after admin action; show submission status to members. |
| 6 | **Dashboard `content-summary` endpoint** | New admin widget data: total posts, per-category counts, pending queue size. |
| 7 | **RFC 7807 problem details on unhandled errors** | Error parsing changes: read `title` / `detail` / `traceId`. |
| 8 | **Rate limiting** | Auth + consultation endpoints return **HTTP 429** when spammed — handle it in UX. |
| 9 | Marketplace / transactions / wallet **removed** | Delete those screens and wallet balances everywhere. |
| 10 | **Auth error codes** (council D1) | Sign-in/sign-up failures return `{ "code": "…", "message": "…" }` — branch on `code`, never on message strings. See §3.1. |
| 11 | **Password minimum = 8** (council D7) | Server rejects shorter passwords on SignUp / change-password / reset. Align your forms. |
| 12 | **Upload returns `imageId`** + new `GET api/Post/categories` + `imageUrls` on public post responses (council D9–D11) | Submission form and blog detail are unblocked. Public feeds/detail are **Approved-only** now. |

---

## 1. Authentication model

- **Scheme:** JWT Bearer — send `Authorization: Bearer <accessToken>` on every protected call.
- **Roles:** `1 = admin`, `2 = member` (role claim rides the standard `role` claim).
- **Token lifetimes:** access ≈ **15 min** (exact value in `expiresInMinutes`), refresh = **30 days**.
- **Rotation:** every `refresh` call burns the presented refresh token and issues a new pair.
  Replaying a burnt token is treated as theft — **the whole session family is revoked**.
  ⇒ Store one refresh token per device, never call refresh twice concurrently,
  and refresh proactively around half-life to avoid boundary 401s.

### Recommended client behavior
```
401 received?
 ├─ have refreshToken? → POST /api/Auth/refresh { refreshToken } → store new pair → retry original once
 └─ refresh failed?    → clear credentials → route to /login
```

---

## 2. Endpoints reference

Legend: 🔓 anonymous · 🔒 any authenticated member · 👑 admin only

### 2.1 Auth — `api/Auth`

#### 🔓 POST `SignIn`
```json
// request
{ "email": "user@example.com", "password": "…" }
// 200 OK
{
  "id": 7, "fullName": "An Nguyễn", "email": "user@example.com",
  "token": "<jwt>", "refreshToken": "<opaque>",
  "expiresInMinutes": 15
}
// 400 { "code": "ACCOUNT_NOT_FOUND" | "INVALID_PASSWORD", "message": "…" }   ·   429 ⇒ RATE_LIMITED
```

#### 🔓 POST `SignUp`
```json
{ "fullName": "An Nguyễn", "email": "user@example.com", "password": "…",
  "dob": "1999-04-12T00:00:00Z", "phone": "09…", "gender": "male" }
```
`password` must be **≥ 8 characters**. `gender` accepts `male | nam | m | female | nữ | nu | f` (case/space-insensitive); blank defaults female-derived; garbage → 400.
Duplicate email → `400 { "code": "EMAIL_TAKEN", "message": … }`. Response = same shape as `SignIn`.

#### 🔓 POST `google-login`
```json
{ "accessToken": "<Google OAuth access token>" }
```
Server verifies with Google, finds-or-creates the account (**never emails passwords anymore**) and returns the full `AuthenticateResponse` above. New accounts have **no password, gender or DOB** until profile completion.

#### 🔓 POST `refresh`
```json
{ "refreshToken": "<current refresh token>" }
// 200
{ "token": "<new access>", "refreshToken": "<new refresh>", "expiresIn": 15 }
// 401 unknown/expired/reused (reused also revokes every active session for that user)
```

#### 🔒 POST `logout`
No body. Revokes **all** refresh tokens for the current user. → `204`.

#### 🔓 POST `ForgotPassword`
```json
{ "email": "user@example.com" }
// 200 "If a user with this email exists, a password reset email has been sent."
```
Response is deliberately neutral (no account enumeration). Email contains a link to
`<FrontendBaseUrl>/reset-password?token=<raw>` — the raw token is single-use, 15-minute TTL.

#### 🔓 POST `ResetPassword`
```json
{ "token": "<raw token from link>", "newPassword": "…" }
// 200 · 400 { "message": "Invalid or expired reset token." }
```
`newPassword` must be **≥ 8 characters**. On success all existing sessions for the account are revoked.

#### 🔒 GET `profile-status`
```json
{ "requiresProfileCompletion": true }
```
`true` when the signed-in user lacks DOB **or** gender (typical for fresh Google accounts).
Frontend should funnel these users through profile completion via `PUT /api/Account/{own id}`.

> ⚠️ Legacy note: `GET /api/Auth/me` does not exist — use `profile-status` + `GET /api/Account/{id}`.

### 2.2 Accounts — `api/Account` (class-level 🔒)

Ownership rule: a member may only touch their own id; admins bypass. Violations → `403`.

| Method & route | Access | Notes |
|---|---|---|
| GET `` (list) | 👑 | array of `AccountResponse` |
| GET `/{id}` | self or 👑 | single `AccountResponse` |
| PUT `/{id}` | self or 👑 | body `{ email, fullName, dob?, gender?, phone? }` |
| DELETE `/{id}` | 👑 | `204` |
| GET `email/{email}` | 👑 | single `AccountResponse` |
| PUT `/{id}/change-password` | self or 👑 | `{ currentPassword, newPassword }` |

```json
// AccountResponse
{ "accountId": 7, "fullName": "An Nguyễn", "email": "user@example.com",
  "dob": "1999-04-12T00:00:00", "phone": "09…", "gender": "male",
  "roleId": 2, "elementId": 2, "elementName": "Thủy" }
```
`elementId`/`elementName` are null until the user has a real DOB (feng shui derivation runs server-side). Prefer `elementId` when present; fall back to name mapping otherwise.

### 2.3 Feng Shui engine

#### 🔓 POST `api/Consultation/fengshui`
```json
// request
{ "yearOfBirth": 1990, "isMale": true }
// 200
{ "element": "Thủy", "cung": "Khảm",
  "luckyNumbers": ["1","6"],
  "fishBreeds": ["Kohaku", "Taisho Sanke"],
  "fishColors": ["Đỏ trắng"],
  "suggestedPonds": [ { "shapeName": "Tròn", "description": "…", "isRecommended": true } ],
  "suggestedDirections": [ { "directionName": "Đông", "description": "Tốt cho Thủy", "isRecommended": true } ] }
```
Rate limited (~30/min/IP). Reference results are cached server-side — expect fast responses.

#### 🔓 POST `api/Compatibility/lookup`
```json
// request
{ "dateOfBirth": 1990, "isMale": true,
  "direction": "Đông", "pondShape": "Tròn",
  "fishColors": ["Đỏ trắng", "Xanh"], "fishQuantity": 6 }
// 200
{ "overallCompatibilityScore": 87.5, "directionScore": 100, "shapeScore": 100,
  "colorScores": { "Đỏ trắng": 100, "Xanh": 50, "TotalScore": 75 },
  "quantityScore": 100,
  "recommendations": ["Các màu Koi (Xanh) có thể không tối ưu. …"] }
```
All scores are 0–100; `overall` = mean of the four dimensions. Vietnamese recommendation strings are server-generated.

#### 🔓 GET `api/Element/GetAll`
Legacy envelope:
```json
{ "status": 1, "message": "Get data success",
  "data": [ { "elementId": 1, "elementName": "Kim", … , "luckyNumber": "6,7" } ] }
```
`status`: `1` success · `-1/-4` failure · `4` no-data warning. *(Newer surfaces use plain JSON instead — this envelope survives only here and image upload.)*

#### Partner Shops — `api/partner-shops` ✨ NEW

| Route | Access |
|---|---|
| GET `` | 🔓 — active shops, sorted by name |
| GET `/{id}` | 🔓 |
| POST `` | 👑 — `201 Created` |
| PUT `/{id}` | 👑 — `204` |
| DELETE `/{id}` | 👑 — `204` |

```json
// request/response body
{ "name": "Koi Hồ Tây", "address": "123 Âu Cơ, Hà Nội",
  "linkUrl": "https://koihotay.example", "note": "Chuyên Kohaku lớn",
  "isActive": true }            // response adds: "id": 3, "createdAt": "2026-08-23T…"
```

### 2.4 Community — posts, FAQ, uploads, dashboard

#### Public posts — `api/Post`
| Route | Access | Notes |
|---|---|---|
| GET `GetAll` | 🔓 | feed of **Approved** posts only |
| GET `GetAllByPostType/{postTypeId}?page=1&pageSize=N` | 🔓 | paginated slice, Approved only |
| GET `Details/{id}` | 🔓/👑 | Approved only; **404 for non-approved** unless admin token (admin bypass reads the full queue) |
| GET `categories` ✨ | 🔓 | category constants — see below |
| GET `my-posts` ✨ 🔒 | 🔒 | caller's own queue (Pending **and** Approved) for the "my submissions" view — see below |
| POST `Create` | 🔒 | member submission — see below |
| DELETE `Delete/{id}` | 👑 | |

#### 🔒 GET `api/Post/my-posts?page=1&pageSize=50` ✨ (council Q11)
```json
{ "status": 1, "message": "…",
  "data": [ { ...PostResponse..., "status": "Pending", "imageUrls": [...] }, … ] }
```
Identity comes from the token only — there is no account-id parameter, so no
cross-account reads are possible. Returns the caller's posts in **all** statuses
(newest first). No `rejectionReason` exists; statuses are `Pending | Approved` today.
Static list — refetch when the member opens the view; no polling contract.
"Đang chờ duyệt" = items with `status === "Pending"`.

#### 🔓 GET `api/Post/categories` ✨ (council D10)
```json
{ "status": 1, "message": "…",
  "data": [ { "categoryId": 1, "categoryName": "…" }, … ] }
```
Consume this instead of hardcoding post-type ids. Interim defaults until wired: blog = `3`, community = `1`.

Member create request — server **ignores** any status/author fields you try to send:
```json
{ "title": "Koi mới của tôi", "content": "Vừa thả 6 chú Kohaku…",
  "categoryId": 3, "imageIds": [41, 42] }
```
Created posts land with `status: "Pending"` and appear to members as *awaiting approval*;
admins publish them via the AdminPost endpoints.

```json
// PostResponse (feed + Details items)
{ "postId": 12, "id": 3,              // ⚠️ "id" here = category id (legacy wire name)
  "name": "Koi mới của tôi",          //    post title lives in "name"
  "description": "Vừa thả…", "createAt": "…", "updateAt": "…",
  "accountId": 7, "accountName": "An Nguyễn",
  "elementId": 1,                     // 0 = uncategorized/member post without element
  "elementName": null, "status": "Approved",
  "imageUrls": ["https://res.cloudinary.com/….jpg"] }   // ✨ council D11; [] when none
```

#### Admin posts — `api/AdminPost` (👑 class-level)
| Route | Verb | Notes |
|---|---|---|
| `GetAllPosts` | GET | includes pending queue |
| `GetPostById/{id}` | GET | |
| `UpdatePost/{id}` | PUT | multipart/form-data: fields + `Images[]` (files) |
| `CreatePostWithImages` | POST | multipart/form-data, same binding |
| `DeletePostWithAllRelated/{postId}` | DELETE | removes links + images atomically |

```json
// AdminPostResponse — superset of PostResponse plus ImageUrls[]
{ …, "imageUrls": ["https://res.cloudinary.com/…jpg"] }
```
`AdminPostRequest` form fields: `id` (= category id), `name`, `description`, `accountId`,
`status` ("Approved"/"Pending"), `elementId`, `images` (zero or more files).

#### FAQ — `api/FAQ`
GET `GetAll`, GET `Details/{id}` public · POST/PUT/DELETE admin.
```json
{ "faqId": 2, "question": "Cho Koi ăn mấy lần một ngày?", "answer": "2–3 lần.", "createAt": "…" }
// Create/Update body: { "question": "≤1000 chars", "answer": "≤2000 chars" }
```

#### Upload — `api/UploadImage/UploadFile` 🔒
`multipart/form-data`, field `file`. Legacy envelope response:
```json
{ "status": 1, "message": "Upload file success.",
  "data": { "imageId": 41, "url": "https://res.cloudinary.com/….jpg", "message": "Upload file success." } }
```
`imageId` ✨ (council D9) is the id to send in post creation `imageIds[]`; `url` is unchanged for legacy adapters.

#### Dashboard — `api/Dashboard` (👑)

| Route | Returns |
|---|---|
| GET `new-users-count?days=30` | `{ "count": 14 }` |
| GET `new-users-list?days=30` | safe profiles (no credential fields): `[ { accountId, fullName, email, dob?, phone?, gender?, elementId?, roleId?, createAt, updateAt } ]` |
| GET `traffic-distribution` | `{ "registeredUsers": 62.5, "uniqueGuests": 37.5, "totalVisitors": 8 }` — percentages rounded to 2dp |
| GET `content-summary` ✨ | below |

```json
// GET content-summary
{ "totalPosts": 128,
  "byCategory": [ { "categoryId": 3, "categoryName": "Koi của tôi", "count": 64 } ],
  "pendingCount": 5 }
```

---

## 3. Error contract

**Unhandled exceptions** return RFC 7807 `application/problem+json`:
```json
{ "type": "https://httpstatuses.io/400", "title": "Invalid request", "status": 400,
  "detail": "Could not find element with name Thủy", "instance": "/api/Compatibility/lookup",
  "traceId": "0HN7GK…" }
```
Mapping: `ArgumentException→400` · `KeyNotFoundException→404` · `InvalidOperationException→409` · `UnauthorizedAccessException→403`. Server faults (500) never leak exception text.

Many older controllers still return ad-hoc shapes (`{ message }`, plain strings, legacy envelopes) — treat `problem+json` content-type as authoritative where present, else fall back to `message`/string body.

**Rate limiting:** exceeding limits yields bare **429** (auth ≈10/min/IP, consultation ≈30/min/IP, global 120/min). Back off and retry; don't hammer refresh.

### 3.1 Auth error codes ✨ (council D1)

Auth failures return `400 { "code", "message" }`. **`code` is authoritative** — the human
readable `message` ships only during the transition so legacy clients keep working and will
eventually disappear. Never string-match messages.

| Code | Meaning | Emitted by |
|---|---|---|
| `ACCOUNT_NOT_FOUND` | No account with that email | SignIn |
| `INVALID_PASSWORD` | Wrong password | SignIn |
| `EMAIL_TAKEN` | Email already registered | SignUp |
| `RATE_LIMITED` | Too many requests | any 429 response (status-code convention, not a body field) |

```json
// example
{ "code": "INVALID_PASSWORD", "message": "Incorrect password." }
```
Treat unknown codes as generic failure; new codes may be added in minor releases.

---

## 4. Removed surfaces — do NOT build against these

`/api/Transaction*` · `/api/MarketplaceListings*` · `/api/SubcriptionTiers*` · `/api/MarketCategory*` · wallet fields/endpoints. They return 404 (or 401 behind auth walls). Integration tests pin them gone.

---

## 5. Suggested frontend work breakdown

1. **Auth module rewrite** — token storage, axios interceptor (refresh+retry-once), logout, 429-aware throttling.
2. **Onboarding gate** — after Google login check `profile-status`; route to profile completion form.
3. **Reset-password page** at `/reset-password` reading `?token=` from query.
4. **Partner Shops browser** (+ admin CRUD screen).
5. **Community feed** with Pending-status awareness + member submission confirmation state.
6. **Admin dashboard widgets**: new-users charts, traffic distribution, content-summary (incl. pending queue badge).
