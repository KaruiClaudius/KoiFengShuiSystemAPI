# 0004: PostgreSQL Migration and EF Migrations Consolidation

## Status

Accepted

## Context

The system persisted to SQL Server via `Microsoft.EntityFrameworkCore.SqlServer`, with EF
migrations living in the legacy `KoiFengShuiSystem.DataAccess` project. That migration
history had drifted badly from the current model (removed shop/payment entities and
`Account.Wallet` still present in snapshots; identity hardening columns and the modular
entities were missing), so it could neither be replayed nor trusted. The team decided to
standardize on PostgreSQL for deployment.

## Decision

1. **Provider**: `Npgsql.EntityFrameworkCore.PostgreSQL` 8.x replaces SqlServer; referenced
   only by `Shared.Infrastructure`, which owns the `DbContext`. The legacy DataAccess
   project no longer references any provider.
2. **Fresh-start baseline**: the stale SQL Server migration history was deleted and replaced
   by a single `ConsolidatedBaselinePostgreSql` migration under
   `src/Shared/Shared.Infrastructure/Migrations`. No data is preserved: the old database is
   disposable and no ETL is performed. `MigrationsAssembly` now points at the context's own
   assembly.
3. **DateTime mapping**: all DateTime properties map to PostgreSQL `timestamp without time
   zone` via `ConfigureConventions` on the context. The Npgsql default (`timestamptz`)
   rejects non-UTC `DateTimeKind` values, and this codebase mixes `DateTime.Now`,
   `DateTime.UtcNow` and unspecified kinds across legacy write paths. Wall-clock storage is
   the low-risk choice today; once writers are normalized to UTC, flip to `timestamptz`
   here. Trade-off: no automatic timezone/DST handling until then.
4. **Local provisioning**: docker-compose gains a `postgres:17-alpine` service with a named
   volume, `pg_isready` healthcheck, and credentials configurable via `KFS_PG_*` env vars;
   the API service waits for it to be healthy and defaults its connection string to the
   compose network host name `postgres`.

## Consequences

- Existing SQL Server databases must be recreated from scratch (`dotnet ef database update`
  against a fresh PostgreSQL instance); nothing migrates over.
- New developer setup is: start the compose postgres service, set the Development connection
  string (defaults provided), apply the single baseline migration.
- Schema drift risk drops: migrations live beside the model they describe and regenerate
  cleanly from the current entity graph.
