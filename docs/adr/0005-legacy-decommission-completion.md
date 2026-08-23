# 0005: Legacy Layer Decommission Completion

## Status

Accepted (2026-08-23)

## Context

The repository began 2026 with two coexisting architectures: a legacy layered set of
projects at the repo root (`KoiFengShuiSystem.Api`, `.Services`/`.BusinessLogic`,
`.DataAccess`, `.Common`, `.Shared`) and a partially migrated modular monolith under
`src/`. ADR-0001 chose a single-deployable modular monolith as the destination; phases
3–6 moved entities, configurations, Identity and FengShui into modules, but both stacks
remained load-bearing: `src/Host` compiled only through references to the legacy
projects, migrations lived in the old DataAccess assembly, and duplicate domain logic
(the Cung Phi calculator existed three times) drifted silently.

## Decision

Complete the transition by deletion rather than parallel maintenance:

1. **Feature ports before deletion.** Every capability still served only by legacy code
   was ported into a module behind module-owned stores/services first (FAQ → Community,
   Posts/AdminPosts → Community, Dashboard (+content metrics) → Community, Image upload →
   Community, FengShui controllers → module services). Wire contracts were preserved
   byte-for-byte during each port so tests and consumers never straddled two shapes.
2. **One canonical implementation per concept.** The Cung Phi calculator was consolidated
   into `FengShui.Domain` behind an `IElementCalculator` seam; token validation parameters
   collapsed into a single factory consumed by all four validation paths.
3. **Then delete.** The legacy Api host, BusinessLogic, DataAccess and Common projects were
   removed from the solution entirely. Surviving shared primitives were relocated to their
   natural homes (rate limiting + installer scanning → `Shared.Kernel`, email service →
   `Identity.Infrastructure`, response codes → `Shared.Kernel.ResponseCodes`).
4. **Product scope reduction executed in the same effort:** payment gateways, marketplace,
   tiers and wallet deleted outright; replaced by the Partner Shops directory.

## Consequences

- Exactly one architecture remains: `Host` + `Shared.Kernel` + `Shared.Infrastructure`
  + `Modules/{Identity,FengShui,Community}`; `KoiFengShuiSystem.Shared` survives solely as
  the entity/helper library whose namespaces still read `KoiFengShuiSystem.DataAccess.Models`
  — folding those namespaces away is deferred until a convenient model-change window.
- Supersedes any remaining guidance in ADR-0002/0003 that implied the legacy projects'
  continued existence.
- Historical documentation sections describing the marketplace, transactions or SQL Server
  are marked *(legacy / REMOVED)* in the main documentation.
