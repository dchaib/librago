# Librago frontend

Standalone React development preview using TypeScript, Vite, React Router, Tailwind CSS 4, and shadcn components built on Base UI. It runs independently of the current ASP.NET Core/Razor application and does not call the backend yet.

## Development

Use the Node.js version declared in [`.node-version`](.node-version) and its bundled npm.

Run from `web/`:

```sh
npm ci
npm run dev
```

Open the loopback URL printed by Vite. The preview provides two routes and a button counter to check rendering, navigation, and interaction.

## Checks and build

```sh
npm run typecheck
npm run lint
npm run build
```

The build includes TypeScript validation and writes assets to ignored `dist/`. Use `npm run preview` to serve the build locally.

Dependencies use exact versions and a committed lockfile, maintained by Renovate. Use `npm ci` to install the locked dependencies.

## Integration boundary

Frontend assets are not included in the deployed application. Backend integration, authentication, and Librago themes remain deferred. See the [web architecture](../docs/architecture/web.md) for the target integration.
