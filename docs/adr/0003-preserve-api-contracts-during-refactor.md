# 0003: Preserve API Contracts During Refactor

## Status

Accepted

## Context

KoiFengShuiSystemAPI exposes HTTP endpoints used by existing clients. The current refactor is focused on internal architecture, module boundaries, and maintainability rather than changing product behavior or public integration contracts.

Changing routes, request shapes, response shapes, status codes, or authentication behavior during structural migration would create client regressions and make it harder to distinguish architecture issues from behavior changes.

## Decision

Public API contracts must be preserved during the migration unless a contract change is explicitly planned, reviewed, and versioned. Internal controllers, handlers, services, DTO organization, and module placement may change, but externally observable behavior should remain compatible.

The target architecture should support the same current API surface while moving implementation details behind clearer module boundaries.

## Consequences

The refactor can be validated with existing client expectations and regression tests. Migration work should prioritize behavior-preserving moves and keep compatibility concerns visible when touching endpoint code.

Some internal designs may need temporary adapters or transitional organization to avoid breaking consumers. Any intentional API change must be treated as a separate product/API decision rather than hidden inside the architecture refactor.
