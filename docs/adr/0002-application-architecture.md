# ADR-0002 — Application architecture for version 0.1

## Status

Accepted

## Context

Version 0.1 must periodically synchronize loans from several library accounts, preserve the last known state, and expose a small responsive web interface. Librago is self-hosted and its first implementation should remain simple to operate and change.

The existing connector experiments target .NET 10. Nantes exposes a JSON API that can be called with `HttpClient`. Nozay requires JavaScript execution to pass an Anubis challenge and has been successfully accessed with Playwright.

## Decision

Version 0.1 is a modular monolith built as one ASP.NET Core 10 application.

- Razor Pages renders the French interface on the server.
- A hosted background service synchronizes on first use or when the persisted synchronization interval is due, then continues on that configurable interval.
- Network-specific connectors implement a common interface and return generic loan snapshots.
- SQLite stores loans and synchronization state on a persistent volume.
- Schema changes are applied by small, ordered in-application migrations.
- Configuration comes from standard ASP.NET Core configuration providers. Deployment secrets are supplied outside the repository.
- Production diagnostics use structured logs and a health endpoint, without logging credentials, tokens, raw upstream responses, or loan details.

Source code is kept in one application project and organized by responsibility. Separate projects will only be introduced when a concrete need appears.

## Consequences

- deployment requires only one application process and one persistent data directory;
- synchronization and the web interface share a domain model without distributed-system complexity;
- SQLite is appropriate for the initial single-instance deployment but does not support active-active replicas;
- the Nozay connector makes the runtime image larger because Chromium is required;
- a failing connector cannot erase previously stored loans;
- moving synchronization to a separate worker remains possible later through the connector and repository boundaries.
