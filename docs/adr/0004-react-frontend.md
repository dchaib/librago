# ADR-0004 — React frontend and separate toolchains

## Status

Accepted

## Context

Librago's original web interface uses ASP.NET Core Razor Pages to present loans and reservations across multiple library accounts and networks. The next iteration introduces a home dashboard, shared navigation, richer filtering interactions, and reusable UI components. Librago's design system, including the Ocean and Natural themes in light and dark modes, needs to work consistently across mobile and desktop views.

We want a frontend architecture that supports these interactions while keeping application functionality in .NET and retaining a single-image Docker deployment, without a separate frontend server in production.

This decision replaces only the Razor Pages frontend choice in [ADR-0002](0002-application-architecture.md).

## Decision

Use React, TypeScript, Vite, and React Router for a client-side SPA. Use Tailwind CSS 4 and editable shadcn/ui components built on Base UI primitives. The framework-neutral [design documentation](../design/principles.md) remains the visual and interaction authority; library defaults do not define Librago's identity.

ASP.NET Core serves compiled assets and a same-origin application API in one deployable image. There is no React SSR, Next.js, or frontend Node.js server at runtime. Playwright's private Node runtime remains separate provider tooling.

Separate frontend tooling in `web/` and .NET tooling in `server/`. These are physical boundaries, not application layers. Keep `Librago.slnx`, `Librago.csproj`, `Librago.Tests.csproj`, and `Librago.*` namespaces. A root product build assembles both toolchains. No monorepo orchestrator is required.

## Alternatives

- **Keep Razor Pages:** already functional and integrated with .NET, and capable of supporting these features. As client-side interactions grow, combining server-rendered pages with additional JavaScript becomes less convenient for the intended reusable UI patterns.
- **Blazor:** offers strong .NET integration. Blazor Server requires a persistent connection for interactivity; Blazor WebAssembly runs a .NET runtime in the browser. Both are viable, but React's component and frontend-tooling ecosystem better fits the intended development workflow.
- **Svelte:** a credible alternative with an appealing, simple component model. Its smaller component ecosystem offers fewer ready-made implementation and tooling options for the intended workflow.
- **React (selected):** provides a mature ecosystem, accessible UI primitives through Base UI, broad tooling support, and flexible client-side composition. It fits the desired SPA architecture at the cost of an additional frontend toolchain.

A successful technical proof of concept established the feasibility of React, Base UI/shadcn integration, and ASP.NET Core static hosting. It did not establish comparative performance or imply that alternatives cannot work.

## Consequences

- Development and CI must manage two toolchains; complete publishing must include the frontend and fail when required assets are missing.
- Same-origin hosting supports the cookie/antiforgery boundary in [ADR-0005](0005-oidc-web-security.md), without a production CORS requirement for the SPA.
- Server endpoints and missing assets must never fall through indiscriminately to SPA HTML; the [web reference](../architecture/web.md) owns routing and build details.
- Global state, query/cache, form, and schema-validation frameworks remain choices to justify during implementation.
- ADR-0002's monolith, SQLite, connector, and background-service decisions remain valid.
