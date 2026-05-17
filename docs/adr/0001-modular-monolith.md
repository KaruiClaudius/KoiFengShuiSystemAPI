# 0001: Keep a Modular Monolith

## Status

Accepted

## Context

KoiFengShuiSystemAPI is currently deployed as a single ASP.NET Core application backed by one solution and one runtime process. The refactor plan introduces clearer module boundaries so Identity, FengShui, Community, Marketplace, Payments, Admin, and Notifications capabilities can be separated in code without changing deployment topology.

Splitting the system into distributed services now would add operational complexity, network failure modes, deployment coordination, and data consistency concerns before the module boundaries have been proven inside the codebase.

## Decision

The architecture remains a single deployable modular monolith during this refactor phase. Modules should be organized with explicit ownership boundaries and internal implementation details, but they continue to run in the same application process and are delivered as one deployable API.

The target architecture is a modular monolith with clearer feature/module ownership, not a microservices architecture.

## Consequences

The refactor can improve maintainability and ownership without introducing distributed-system costs. Cross-module interactions should still be made explicit so future extraction remains possible if there is a concrete need.

Deployment, hosting, observability, and release practices remain centered on one API application. Module boundaries must be enforced through code structure and dependency direction rather than process isolation.
