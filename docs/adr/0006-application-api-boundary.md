# ADR-0006 — Application-oriented API as an adapter

## Status

Accepted

## Context

The React frontend needs HTTP endpoints for Librago's loans, reservations, and other application functionality. Scheduled synchronization runs within the .NET application and uses the same underlying data and operations.

If application behavior were implemented directly in HTTP handlers, background jobs and future interfaces would have to duplicate that logic or make HTTP calls to the application itself.

This decision defines shared application functionality independent of how it is invoked. It extends [ADR-0002](0002-application-architecture.md) and preserves [ADR-0003](0003-account-synchronization.md).

## Decision

Keep a modular monolith with one .NET application project, organized by responsibility. HTTP endpoints adapt incoming requests to shared application functionality. API handlers and the BackgroundService call that functionality directly; the worker does not call the application's HTTP endpoints.

The API is application-oriented: its endpoints expose useful Librago operations and data, primarily for the frontend. It is neither a strict screen-by-screen Backend for Frontend (BFF) nor a generalized public platform API.

Read projections select or summarize application data for a specific use case. For example, a dashboard response could return loan and reservation counts grouped by library network, rather than requiring the frontend to retrieve every item and calculate those summaries.

Keep the design compatible with future CLI or MCP adapters without implementing them or introducing speculative interfaces and separate Domain, Application, or Infrastructure assemblies. Browser authentication and household authorization follow [ADR-0005](0005-oidc-web-security.md); transport authentication does not replace application access rules.

## Consequences

- Application rules and synchronization behavior remain reusable without depending on HTTP handlers or requiring internal HTTP calls.
- Read projections must preserve [account/collection data-confidence semantics](../product/features/loans.md#known-data-coverage), including verified empty versus unknown snapshots. This ADR does not prescribe a persistence schema.
- A strict BFF would couple API capabilities to individual screens; a public platform would require long-term API compatibility and authentication commitments beyond the current product's needs.
- [ADR-0003](0003-account-synchronization.md)'s one-session-per-account orchestration and independent collection outcomes remain valid.
- Shared bounded synchronization concurrency must precede any manual synchronization API; rate limiting alone is insufficient.
