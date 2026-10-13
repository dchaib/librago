# Librago frontend

Standalone React development preview using TypeScript, Vite, React Router, Tailwind CSS 4, and shadcn components built on Base UI. It runs independently of the current ASP.NET Core/Razor application and does not call the backend yet.

## Development

Use the Node.js version declared in [`.node-version`](.node-version) and its bundled npm.

Run from `web/`:

```sh
npm ci
npm run dev
```

Open the loopback URL printed by Vite. The preview provides Ocean and Natural theme controls, each with light and dark modes, plus two routes, a button counter, an input, and labeled status examples. Selection is local React state and resets on reload; Ocean Light is only the preview default.

## Visual identity

`src/index.css` maps the four [visual foundation palettes](../docs/design/foundations.md) onto shadcn semantic variables, using `data-theme` and `.dark` on the document root. The functional token values are implementation choices for the preview and remain provisional pending visual review. Status colors are separate semantic examples and are not part of the approved theme palettes.

## Checks and build

```sh
npm run typecheck
npm run lint
npm run build
```

The build includes TypeScript validation and writes assets to ignored `dist/`. Use `npm run preview` to serve the build locally.

Dependencies use exact versions and a committed lockfile, maintained by Renovate. Use `npm ci` to install the locked dependencies.

## Integration boundary

Frontend assets are not included in the deployed application. Backend integration, authentication, and real application screens remain deferred. See the [web architecture](../docs/architecture/web.md) for the target integration.
