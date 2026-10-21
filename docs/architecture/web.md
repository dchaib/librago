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
│   └── .NET tooling/configuration
├── web/
│   ├── src/
│   ├── package.json
│   ├── package-lock.json
│   └── vite.config.ts
├── docs/
├── scripts/
├── Dockerfile
├── compose.yaml
└── README.md
```

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

### shadcn foundation

`web/components.json` selects **base-nova**, Base UI, React SPA (`rsc: false`), TypeScript, Tailwind CSS 4, CSS variables, Lucide, and default/subtle menus without RTL. The generator base color is **Neutral**; it supplies initial semantic roles, not the runtime Ocean/Natural palettes. Vite's Tailwind plugin and the TypeScript/Vite `@/` aliases complete the build integration; Tailwind 4 needs no JavaScript Tailwind config.

`web/src/index.css` imports Tailwind, `tw-animate-css`, then `shadcn/tailwind.css`. The latter supplies shared state variants and utilities; Nova component recipes are already expanded into `components/ui`, so no separate Nova stylesheet is needed. Semantic colors and Nova's radius scale are exposed through `@theme inline`, with base border/outline, body, and font styles in `@layer base`. Application CSS remains separate.

`AppearanceControls` is the single owner of appearance state and document-root updates. It follows the shadcn `.dark` convention for mode, independently of `data-theme="ocean" | "natural"` for palette. Root variables also reach body portals. Ocean Light remains the initial default; state survives SPA navigation in the shared shell and resets on full reload. No system mode, storage, or additional ThemeProvider context is needed for the current controls. Keep existing font/color values and domain status pairs (`warning`, `danger`, `deadline-neutral`, etc.) separate from shadcn action colors such as `destructive`. Unused chart/sidebar roles retain their generated defaults until needed.

Start from official shadcn components and compositions matching the project's configuration. Customize through semantic theme tokens and supported component APIs first. Introduce application-specific CSS or compositions only for justified requirements. Do not recreate existing toolkit functionality through custom styles or native controls without a reason.

Generated implementations live in `web/src/components/ui`; pages and domain compositions live outside that directory. Existing application overrides do not establish new primitive defaults. See the [frontend development guide](../../web/README.md#adding-ui-components) for adding components and validating the foundation.

Official references: [Vite installation](https://ui.shadcn.com/docs/installation/vite), [components.json](https://ui.shadcn.com/docs/components-json), [CLI](https://ui.shadcn.com/docs/cli), [theming](https://ui.shadcn.com/docs/theming), [Vite dark mode](https://ui.shadcn.com/docs/dark-mode/vite), and the [Base Nova initialization registry](https://ui.shadcn.com/r/styles/base-nova/index.json). The dark-mode example's context, system preference, and persistence are illustrative application choices.
