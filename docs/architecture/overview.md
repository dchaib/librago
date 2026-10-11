# Architecture overview

Librago is a self-hosted application that consolidates a household's library loans and reservations across configured accounts and networks. This directory describes its accepted target architecture as a whole.

## Application shape

Librago is a modular monolith built as one ASP.NET Core application project. It serves the compiled React frontend and a same-origin application API, runs scheduled synchronization, and integrates with library providers through network-specific connectors. One application instance owns a persistent SQLite database.

```text
Browser -> Reverse proxy (HTTPS) -> ASP.NET Core / Librago
                                      +-- React static assets
                                      +-- application API and authentication -> OIDC provider
                                      +-- BackgroundService
                                      +-- shared application functionality
                                             +-- SQLite database
                                             +-- network-specific provider connectors
                                                    +-- HTTP / Playwright
```

The `server/` and `web/` directories separate .NET and frontend toolchains, without introducing additional logical layers. The frontend presents loans, reservations, and dashboard summaries and handles client navigation. HTTP endpoints and the BackgroundService invoke shared application functionality directly.

## Data flow

Scheduled synchronization retrieves loan and reservation snapshots through provider connectors and stores each successful account collection transactionally. Failed or incomplete collections preserve last-known data; loans and reservations have independent outcomes.

Application reads and summaries use persisted state, with account/collection coverage and freshness determining data confidence. A successful empty snapshot is distinguishable from missing data. Application responsibilities, synchronization/read flows, connector boundaries, and persistence guarantees are described in the [application reference](application.md).

## Runtime and trust boundaries

A reverse proxy terminates public HTTPS and forwards requests to one application container. ASP.NET Core authenticates through an external OpenID Connect (OIDC) provider and admits explicitly authorized identities, all sharing the configured household accounts. It establishes a secure `HttpOnly` application cookie; React uses a separate in-memory antiforgery request token for mutations. Library credentials and provider sessions remain private server data.

Compiled assets and the application API share one origin. Trusted forwarding preserves the external scheme, and server-owned routes never fall through to SPA HTML. Persistent SQLite data and Data Protection keys survive container recreation; temporary browser/Xvfb state is ephemeral.

Provider content is untrusted. Playwright/Chromium runs inside the hardened application container, with residual exposure to application state and credentials accepted in [ADR-0007](../adr/0007-browser-automation-isolation.md). Container hardening and effective egress restrictions are part of the required [security boundary](security.md).

## Technical references and decisions

- [Application architecture](application.md) — application responsibilities, synchronization/read flows, connector contracts, and persistence guarantees; [ADR-0002](../adr/0002-application-architecture.md), [ADR-0003](../adr/0003-account-synchronization.md), and [ADR-0006](../adr/0006-application-api-boundary.md).
- [Web hosting and toolchains](web.md) — frontend build, hosting, routing, and design-system integration; [ADR-0004](../adr/0004-react-frontend.md).
- [Authentication and household access](authentication.md) — identity admission, sessions, antiforgery, and Data Protection; [ADR-0005](../adr/0005-oidc-web-security.md).
- [Security boundaries](security.md) — browser policies, runtime hardening, and network boundaries; [ADR-0007](../adr/0007-browser-automation-isolation.md) and [ADR-0001](../adr/0001-public-repository.md).
- [Network references](../README.md#networksnetworkmd) — provider-specific access, mappings, completeness, and identity constraints.
