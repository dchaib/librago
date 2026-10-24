# Librago web

Librago's React frontend, built with TypeScript, Vite, React Router, Tailwind CSS 4, and shadcn/ui on Base UI. The interface is in French; code and documentation are in English.

## Development

Use the Node.js version specified in the repository root's `.node-version` and npm. Run the following commands from `web/`:

```bash
npm ci
npm run dev
```

In a second terminal, run `dotnet run --project server/src/Librago` from the repository root after configuring the backend as described in the root README. The launch profile uses `Development` and listens on `http://127.0.0.1:5080`. Vite listens on loopback and proxies `/api` to that address. No CORS configuration is required.

| Command | Purpose |
| --- | --- |
| `npm run dev` | Start the development server with hot reload. |
| `npm run typecheck` | Check TypeScript project types. |
| `npm run lint` | Run ESLint. |
| `npm test` | Run the loan domain tests using Node's test runner. |
| `npm run build` | Check types and build static assets into `dist/`. |
| `npm run preview` | Serve the built assets locally after a build. |
| `npm run format` | Format TypeScript and TSX files with Prettier, including Tailwind class ordering. |

The tests cover date calculations, severity thresholds, sorting, URL filters, API decoding, HTTP failures and data-confidence rules. They do not verify browser layout or interactions. Check the Loans page at mobile and desktop widths, in light and dark modes, when changing its presentation.

## Source layout

| Path | Responsibility |
| --- | --- |
| `src/main.tsx` | React entry point and Base UI CSP integration. |
| `src/App.tsx` | Client routes. |
| `src/components/AppShell.tsx` | Shared navigation and page container. |
| `src/components/mode-toggle.tsx` | System color preference and the session's light/dark override. |
| `src/components/ui/` | Editable shadcn/ui primitives. |
| `src/index.css` | Tailwind imports, shadcn theme variables, fonts, and base styles. |
| `src/loans/LoansPage.tsx` | Loan page composition, URL filters, and the mobile filter dialog. |
| `src/loans/loan-data-states.tsx` | Read errors, coverage alerts, and empty states. |
| `src/loans/loan-synchronization-status.tsx` | Per-network synchronization times and confidence indicators. |
| `src/loans/loan-filters.tsx` | Desktop and mobile filter controls. |
| `src/loans/loan-list.tsx` | Mobile loan list, desktop table, covers, and deadlines. |
| `src/loans/loans.ts` | Pure domain types, date calculations, sorting, and filtering. |
| `src/loans/use-calendar-date.ts` | Calendar-day updates in the application time zone. |
| `src/loans/loans-api.ts` | API contract, response validation and data-confidence rules. |
| `src/loans/use-loans.ts` | Cancellable reads on entry/foreground return, five-minute throttling and preserved data on errors. |
| `tests/fixtures/mock-loans.ts` | Synthetic test data; never imported by the application. |
| `tests/loans.test.ts` | Domain tests. |
| `public/` | Static assets served from the site root. |

The `@/` import alias resolves to `src/` in Vite and TypeScript.

## UI foundation

The current implementation deliberately starts from standard shadcn styling: `base-nova`, Neutral colors, Geist, and the `.dark` convention. There is no custom Librago palette for now. The mode toggle follows the system preference until manually changed; the override lasts across SPA navigation and resets on reload.

Use existing primitives and their variants before introducing custom styling. Keep domain compositions in feature folders. Simple HTML with Tailwind layout utilities is appropriate when a toolkit subcomponent adds unnecessary behavior or styling dependencies, as with loan covers and deadlines.

### Content Security Policy

Keep `CSPProvider disableStyleElements` around the application in `src/main.tsx` and the `.base-ui-disable-scrollbar` rule in `src/index.css`. Together they provide Base UI's scrollbar styles through the external stylesheet instead of injected style elements. See the [security reference](../docs/architecture/security.md#browser-security-baseline) for the deployment policy; Vite development alone does not validate production CSP compatibility.

## Product and architecture references

- [Loans behavior](../docs/product/features/loans.md)
- [Web architecture and hosting](../docs/architecture/web.md)
- [Design principles](../docs/design/principles.md) and [visual foundations](../docs/design/foundations.md)

The appearance sections of the architecture and design references still describe the earlier Ocean/Natural implementation. The current frontend uses the neutral foundation described above while custom theming is deferred.
