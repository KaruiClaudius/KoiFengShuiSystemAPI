# 0002: Retain a Shared DbContext First

## Status

Accepted

## Context

KoiFengShuiSystemAPI currently uses a shared Entity Framework Core `DbContext` for persistence. The refactor plan moves toward module-owned entities and configurations, but the existing data model is still shared across multiple application areas.

Moving immediately to multiple contexts or independently owned persistence models would require broad schema, migration, transaction, and query changes while the module boundaries are still being clarified.

## Decision

The project will retain one shared `DbContext` until all entities and EF configurations have clear module ownership. During the migration, modules may organize their entity mappings and configuration code by feature, but registration remains coordinated through the shared context.

The target architecture is module-owned persistence configuration within a single deployable modular monolith, with any later split considered only after ownership boundaries are complete and justified.

## Consequences

This reduces refactor risk and preserves current transaction behavior, migrations, and database access patterns. It also avoids creating artificial persistence seams before the domain boundaries are stable.

The shared context remains a coupling point. New or moved persistence code should make ownership explicit and avoid increasing cross-module dependencies so the context can be decomposed later if needed.
