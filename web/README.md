# Librago frontend

Standalone React development preview using TypeScript, Vite, React Router, Tailwind CSS 4, and shadcn components built on Base UI. It runs independently of the current ASP.NET Core/Razor application and does not call the backend yet.

## Development

Use the Node.js version declared in [`.node-version`](.node-version) and its bundled npm.

Run from `web/`:

```sh
npm ci
npm run dev
```

Open the loopback URL printed by Vite. The shared application shell provides navigation to Accueil (`/`), Prêts (`/loans`), and Réservations (`/reservations`). Home and reservations are minimal placeholders; `/verification` remains available as the original routing check. The header provides Ocean and Natural theme selection and a light/dark toggle. Appearance selection is local React state, survives route navigation, and resets on reload; Ocean Light is only the preview default.

## Application shell

`src/components/AppShell.tsx` provides the shared header, main content container, and navigation around the React Router outlet. Desktop navigation appears in the header from the `md` breakpoint (768 px); smaller screens use a fixed bottom bar with icons and visible labels. The header scrolls with the document. Mobile content reserves space for the bar and bottom safe-area inset; the viewport enables safe-area support. A keyboard skip link targets the main content.

`AppearanceControls` reuses the existing document-root `data-theme` and `.dark` state contract and semantic tokens. The theme selector and loans filters (including the mobile dialog) use the shadcn/Base UI Select; the mode toggle uses the existing Button. Both appearance controls have 44 px touch targets. Active navigation and secondary actions share the `secondary` color pair, preserving Ocean's appearance and using green selection colors in Natural.

The official horizontal logos are copied unchanged from `assets/brand/svg/` into `public/brand/`, with the approved `on-dark` asset used in dark mode. Their wordmarks are outlined Fraunces artwork and do not depend on the interface fonts. The loans page retains its own heading, illustration, filters, and responsive list/table composition.

## Loans preview

`/loans` presents 24 fictional loans, sorted by due date, with a desktop table and a compact mobile list. Network, borrower and exact deadline filters are encoded as `network`, `borrower` and repeated `severity` query parameters. For example: `/loans?network=rivages&severity=overdue&severity=very-close`. Invalid values are ignored. Filter changes replace the current history entry; mobile changes remain pending until confirmed.

The demo uses `Europe/Paris` as its application time zone, defined in `src/loans/loans.ts`. Mock deadlines are relative to the calendar date at page load and remain fixed while the page is open; relative labels and filtering update at midnight and on tab reactivation. Reloading generates a fresh demonstration snapshot. The two network synchronization times are also fictional. All snapshots are assumed complete and successful; confidence states remain deferred as specified in the product documentation.

To inspect the verified-empty state during development, pass `loans={[]}` to `LoansPage`; a filtered-empty example is `/loans?network=rivages&borrower=camille&severity=close`. Cover SVGs are original demonstration artwork (see `public/covers/README.md`). Missing or failed cover images use the same fallback.

The validated UX uses self-hosted variable Source Serif 4 for item titles and page headings, Inter for controls and metadata, right-aligned relative and absolute deadlines, and neutral removable filter badges. Mobile metadata combines network and borrower with locally included Lucide icons. Compact dates intentionally omit repeated visible “Retour au plus tard le” wording; that context remains available to screen readers.

## Visual identity

`src/index.css` defines self-hosted fonts and maps the four [visual foundation palettes](../docs/design/foundations.md) onto shadcn semantic variables, using `data-theme` and `.dark` on the document root. The current theme/mode combinations have been validated on the Loans page. Loans-specific composition styles live in `src/loans/loans.css`, imported by the page. Status colors are separate semantic examples and are not part of the approved theme palettes.

## Checks and build

```sh
npm run typecheck
npm run lint
npm test
npm run build
```

The build includes TypeScript validation and writes assets to ignored `dist/`. Use `npm run preview` to serve the build locally.

Dependencies use exact versions and a committed lockfile, maintained by Renovate. Use `npm ci` to install the locked dependencies.

## Integration boundary

Frontend assets are not included in the deployed application. Backend integration, authentication, and live data remain deferred. See the [web architecture](../docs/architecture/web.md) for the target integration.
