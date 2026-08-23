# API Agreement Proposal — backend <-> frontend

> **Purpose:** reconcile `backend-api-contract.md` (the frontend's asks, written against
> the OLD backend) with the rebuilt backend documented in `FRONTEND_API_GUIDE.md`.
> Item markers: **[DONE]** already satisfied · **[BACKLOG]** backend will do ·
> **[DECIDE]** needs joint decision · **[REMOVED]** gone by product decision.
>
> Nothing here is final until both sides sign off in this file.

---

## 0. The headline

The frontend contract was written against a backend that **no longer exists**. The
marketplace/listing/wallet world was deliberately deleted (product decision: payments and
shop features removed; purchasing guidance moved to the Partner Shops directory). About
half of the frontend's "endpoints consumed today" (their sections 2.3, market parts of 2.6,
3.1, 3.2) target deleted surfaces. Conversely, the backend shipped an auth overhaul the
frontend has not seen yet (refresh tokens, profile gate, reset links, rate limits).

Both sides re-plan around the new surface; nothing below resurrects commerce.

---

## 1. Security audit items (FE section 4) - scorecard

| # | FE requirement | Status | Detail |
|---|---|---|---|
| S1 | Server-side RBAC | DONE | Role claim rides the JWT; `[Authorize(Roles=...)]` enforced server-side on Dashboard, AdminPost, FAQ writes, Account management, PartnerShop writes. Integration-tested authz matrix exists. |
| S2 | Approval state machine | DONE (posts) | Member `POST /api/Post/Create` forces `status:"Pending"`; only admins move it. Listing variant moot (section 2). |
| S3 | No client wallet mutations | DONE+ | Wallet does not exist anywhere anymore. |
| S4 | Short JWT + refresh | HALF | 15-min access + 30-day rotating refresh exists, delivered in JSON body. httpOnly-cookie variant not implemented - see decision D5. |
| S5 | Server-side HTML sanitization | OPEN | Post/FAQ rich text stored as-is. BACKLOG: allow-list sanitization on create/update paths. |
| S6 | CORS allow-list | DONE | `AllowedOrigins` config array + `WithOrigins`. Prod origins supplied at deploy; dev uses Vite proxy. |
| S7 | Structured errors, correct codes | HALF | Unhandled exceptions return RFC 7807 problem+json (400/404/409/403 mapping). Legacy ad-hoc shapes remain on older endpoints - see decision D6. |

---

## 2. Removed surfaces (FE 2.3, 2.6-market, 3.1, 3.2)

| FE item | Verdict |
|---|---|
| All `/api/MarketplaceListings/*` reads + Create | REMOVED. No listing table, no tiers, no prices. |
| Dashboard `new-market-listings-count` / `by-category` | REMOVED. Replacement: `GET /api/Dashboard/content-summary` (posts metrics). |
| Listing search `q=` parameter (FE 3.1) | REMOVED as specified. If search is still wanted, propose it against Posts (see D3). |
| Favorites for listings (FE 3.2) | REMOVED as specified. A favorites v2 could target posts/koi breeds - see D4. |

Frontend action: delete KoiListings/listing-detail/favorites screens; build the Partner
Shops browser instead (`api/partner-shops`, public GETs + admin CRUD, shapes in the guide).

---

## 3. Existing endpoints the FE still calls - deltas

| FE expectation (their doc) | Current backend reality | Resolution |
|---|---|---|
| `SignIn` failure strings `"Email not found."` / `"Incorrect password."` matched literally | Same strings still returned as `400 { message }` | DECIDE D1: keep strings (zero FE work) or switch to codes (`"code":"ACCOUNT_NOT_FOUND"` etc.) for anti-enumeration + i18n. Recommend codes. |
| SignUp body field `doB` | Binds fine (case-insensitive JSON). Server accepts gender aliases male/nam/m/female/nu/f; garbage = 400. | Keep FE as-is, or align to `dob`. No blocker. |
| Password min length 6 client-side | Server has no explicit min-length rule today. | BACKLOG: server enforces >= 8 with message. FE raises client check to match (DECIDE D7). |
| `GET /api/Account/email/{email}` for profile lookup (returns `wallet`) | Now ADMIN-ONLY and returns no wallet. Members must use `GET /api/Account/{ownId}` after SignIn (response includes id). | FE: profile screen reads own-id endpoint. Backend keeps email lookup admin-only. |
| Account fields include `wallet`, `roleId`, `elementId` | `roleId` and `elementName` present; `wallet` gone; element arrives as `elementName` (null until DOB set). | FE drops wallet UI; uses `elementName`. |
| `GET /api/AdminPost/GetAllPosts` then filter `status === "active"` client-side | Endpoint exists (admin-only now!). Public feed is `GET /api/Post/GetAll`; member posts enter `Pending` until approved. Status vocabulary is `Pending`/`Approved` today, not `active`. | DECIDE D2: status vocabulary (`Approved` vs `active`). Public blog pages move to `/api/Post/*`; AdminPost stays admin-only. |
| Single-post fetch (FE 3.3) - currently fetch-all + find | Public `GET /api/Post/Details/{id}` exists BUT response lacks imageUrls. | BACKLOG: add `imageUrls` to public PostResponse so `/blog/:id` works in one call. |
| FAQ Create sends `{ question, answer, accountId }` | accountId ignored server-side (derived from auth); Create is ADMIN-only now. | FE: drop accountId from body; members do not create FAQs in current design (DECIDE D8 if a "member asks" flow is wanted). |
| Mixed path casing `/api/Dashboard` vs `/api/dashboard` | Single casing everywhere now. | Closed. |
| `categoryid` vs `categoryId` casing on categories | Post wire still carries legacy `id` (= categoryId) plus `postId`. | BACKLOG (v-next): rename to `categoryId`, drop duplicate. FE adapts once, behind flag. |
| Element sentinel `"Non element"` string | Posts use `elementId: 0` + null name sentinel. | Formalize null-over-sentinel in v-next rename (same pass as above). |
| Tier names / `isFeatured` badges (FE 5.5) | Tiers removed entirely. | Closed - no badge data source; PartnerShops have no tiers by design. |

---

## 4. New backend surfaces the FE has not seen (from FRONTEND_API_GUIDE.md)

1. Token lifecycle: `POST /api/Auth/refresh` (rotate+reuse-detection), `POST /api/Auth/logout`.
2. `POST /api/Auth/google-login` creates passwordless accounts; `GET /api/Auth/profile-status`
   gate; profile completion via `PUT /api/Account/{ownId}`.
3. Reset link flow: email -> `/reset-password?token=...` -> `POST /api/Auth/ResetPassword`.
4. `api/partner-shops` directory (public GETs, admin CRUD).
5. Member post submission queue (`POST /api/Post/Create` -> Pending) + admin approval via
   AdminPost endpoints.
6. `GET /api/Dashboard/content-summary`.
7. Rate limits (429) on auth (~10/min/IP) and consultation (~30/min/IP).
8. RFC 7807 problem+json on unhandled errors.

---

## 5. Decision list (D1-D8) - fill in during agreement meeting

| # | Question | Options | Backend recommendation |
|---|---|---|---|
| D1 | Sign-in error contract | keep literal strings vs error codes | Codes + stable HTTP statuses |
| D2 | Post status vocabulary | `Pending/Approved` (current) vs `active/draft` | Keep current values |
| D3 | Search scope without listings | none vs `q=` on posts feed | Add `q=` to posts feed (cheap) |
| D4 | Favorites v2 target | posts vs koi breeds vs defer | Defer until product confirms |
| D5 | Refresh token transport | JSON body (current) vs httpOnly cookie | Body is fine for SPA + proxy; cookie only if XSS posture demands it |
| D6 | Envelope unification scope | unify all endpoints now vs new-endpoints-only | New-endpoints-first, migrate legacy envelopes opportunistically |
| D7 | Password minimum length | 6 vs 8+ | 8+, shared constant documented both sides |
| D8 | Member FAQ submissions | admin-only vs member questions queue | Defer; not in current product scope |

---

## 6. Sign-off

| Side | Name | Date | Agreement hash (git commit of this file) |
|---|---|---|---|
| Backend | | | |
| Frontend | | | |
