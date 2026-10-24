# Web hosting and toolchains

The web frontend uses React, TypeScript, Vite, and React Router as a client-side SPA. Tailwind CSS 4 and editable shadcn/ui components built on Base UI supply styling integration and interaction primitives.

ASP.NET Core serves the compiled frontend assets and a same-origin application API in one deployable image. There is no React SSR, Next.js, or frontend Node.js server at runtime. Playwright's private Node executable remains separate provider automation tooling.

## Repository and build boundary

```text
/
├── server/
│   ├── src/Librago/Librago.csproj
│   ├── tests/Librago.Tests/Librago.Tests.csproj
│   ├── Librago.slnx
│   ├── Directory.Build.props
│   ├── Directory.Packages.props
│   └── NuGet.Config
├── web/
│   ├── src/
│   ├── package.json
│   ├── package-lock.json
│   └── vite.config.ts
├── docs/
├── scripts/
├── global.json
├── .node-version
├── Dockerfile
├── compose.yaml
└── README.md
```

The root `global.json` selects the .NET SDK and test runner, and the root `.node-version` specifies the Node.js version for compatible version managers. Run .NET commands from `server/` and frontend commands from `web/`; Docker, Compose, and deployment validation scripts use the repository root. .NET commands can also run from the repository root with explicit paths, such as `dotnet test --solution server/Librago.slnx`.

The repository has two physical toolchain boundaries. The .NET application and test projects are `Librago` and `Librago.Tests`, with `Librago.*` namespaces. Application responsibilities share one application project, and the root build coordinates the two toolchains without a monorepo orchestration framework.

The root product build assembles both toolchains. Frontend dependencies use locked installation with `npm ci`; Vite output is included in ASP.NET Core's `wwwroot`. A complete publish fails when required frontend assets are missing. A backend-only publish produces only the server artifact. Docker uses a frontend build stage and one final .NET runtime image.

## Hosting and routing contract

A reverse proxy terminates public HTTPS and forwards requests to Librago while preserving the public origin. Compiled assets, API, and authentication endpoints share that origin, without a production cross-origin SPA/API requirement or corresponding CORS configuration. Cookies and antiforgery follow the [authentication contract](authentication.md).

Only GET/HEAD requests for client document routes receive the SPA document. Routing reserves:

- `/api/*` for the HTTP API;
- `/auth/*` and OIDC callbacks for server authentication;
- `/health` and other backend endpoints for the server;
- static-asset paths, whose missing files return `404`.

Unsafe methods never receive SPA HTML. Unknown backend routes return backend errors rather than falling through to `index.html`.

Hashed `/assets` files use immutable caching and the SPA document is revalidatable. Private API/auth responses follow the [security policy](security.md#browser-security-baseline). Public static documents and bundles contain no household data.

## Development and publishing

.NET and frontend builds are independently runnable. Complete publishing installs/builds the frontend, restores locked .NET dependencies, and includes the assets in the application output. Browser checks require the matching Playwright Chromium installation.

Vite runs on loopback over HTTPS for secure-cookie development, proxying API, authentication, OIDC callbacks, and health requests to ASP.NET Core. The proxy preserves the browser-visible HTTPS origin for generated OIDC callbacks. Development proxy trust is limited to the actual proxy, and local certificates are private. Production uses certificate validation and explicit proxy trust; Vite/HMR allowances are confined to development.

## Design-system relationship

```text
Librago design documentation
        -> primitive and semantic tokens
        -> Tailwind theme integration
        -> editable shadcn / Base UI primitives
        -> Librago application components
```

[Principles](../design/principles.md) and [visual foundations](../design/foundations.md) define Librago's visual direction. Use the default conventions and accessible interaction primitives of shadcn/ui, Base UI and Tailwind CSS 4 wherever practical. Adapt tokens or components only for demonstrated screen or product needs; component APIs and implementations are defined in the frontend, not in separate design specifications. Feature documentation owns domain-specific behaviors and responsive compositions.

Technical token/theme mappings and component recipes translate these design roles into the frontend. The design-system references describe their visual and interaction semantics.
