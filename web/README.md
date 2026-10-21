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

The pinned shadcn development CLI currently pulls in `braces` 3.0.3 through its glob tooling. npm reports seven high-severity package entries for the same [unpatched braces denial-of-service advisory](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm). This tooling is not shipped in the browser bundle; track the upstream fix rather than accepting npm's incompatible downgrade suggestion.

## Adding UI components

Follow the [shadcn foundation contract](../docs/architecture/web.md#shadcn-foundation). From `web/`, use the locally pinned CLI:

```sh
npx --no-install shadcn add <component>
```

The CLI reads `components.json`; keep Base Nova, Base UI, Lucide, CSS variables, and the Neutral generator base. Review generated files, dependencies, lockfile, and any global CSS changes before accepting them. Preserve Ocean/Natural tokens and self-hosted fonts. Do not rerun `init` over this application or overwrite matching components for routine additions. Compare with `https://ui.shadcn.com/r/styles/base-nova/<component>.json`, accounting for normal SPA, icon, import, heading-font, and menu transformations.

For foundation changes, build and inspect computed styles and keyboard behavior in all four appearances at desktop and mobile widths, using fictional data with a fixed date/time zone. Check focus/selection, body portals, nested Select/Dialog Escape behavior, animation completion, focus/scroll restoration, reduced motion, and appearance changes while a portal is open. Dialog portals unmount after closing; Base UI can retain hidden Select items for closed-trigger typeahead. Verify that closed content is inactive rather than requiring every Select node to disappear. Confirm the production CSS contains enter/exit keyframes and the toolkit's open/closed/checked variants.

The app root uses `CSPProvider disableStyleElements`; the documented scrollbar class is supplied through shadcn's `no-scrollbar` utility. Validate production assets under the [target CSP](../docs/architecture/security.md#browser-security-baseline), including a default aligned Select as well as the app's `alignItemWithTrigger={false}` composition. The local preview does not prove deployment acceptance; see the [deployment checklist](../docs/deployment/validation.md#browser-security).

### Deferred application work

The foundation retains the loans table skin, gradients/illustrations, mobile list, resized controls/badges, and bottom-attached Dialog composition. Revisit these during the subsequent composition/visual stages. Previously observed issues also remain separate: constrained/enlarged header reflow, focus return when resizing an open mobile dialog past the desktop breakpoint, the generated English close label, narrow long-title layouts, and missing client not-found content. Do not fold these into toolkit installation changes.

## Integration boundary

Frontend assets are not included in the deployed application. Backend integration, authentication, and live data remain deferred. See the [web architecture](../docs/architecture/web.md) for the target integration.
