# Application and deployment security

Librago's security boundaries cover household access, browser requests, provider automation, private state, and deployment. [ADR-0005](../adr/0005-oidc-web-security.md) and [ADR-0007](../adr/0007-browser-automation-isolation.md) record the authentication and browser-isolation decisions. Operational acceptance checks are recorded in the [deployment validation checklist](../deployment/validation.md).

## Browser security baseline

Production enforces the following Content Security Policy through the HTTP response header:

```text
default-src 'self'; script-src 'self'; style-src 'self'; style-src-attr 'none';
connect-src 'self'; img-src 'self'; font-src 'self'; object-src 'none';
base-uri 'none'; frame-ancestors 'none'; form-action 'self'
```

The policy excludes blanket `unsafe-inline`. Base UI's CSP provider disables style-element injection. Application components, including dialog/popover portals, focus behavior, and scrolling, operate under this policy in both themes and light/dark modes. Development Vite/HMR allowances are confined to development.

Fonts are self-hosted. Provider content is untrusted and rendered as text, never interpreted as HTML or executable content.

The browser baseline includes:

| Header / policy | Policy |
| --- | --- |
| `X-Content-Type-Options` | `nosniff` |
| `Referrer-Policy` | `no-referrer` |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` |
| Framing protection | CSP `frame-ancestors 'none'` |
| HSTS | ASP.NET Core HSTS outside Development, after trusted forwarded headers |
| Private responses | `no-store` for API/auth and error responses |

Static assets and the SPA document follow the [web build/cache contract](web.md#hosting-and-routing-contract). HSTS applies at the public HTTPS origin.

## Reverse proxy boundary

```text
Internet -> Reverse proxy (HTTPS) -> Librago container (private application port)
```

The production application port is private, without a directly published Librago port. Librago initiates OIDC and enforces household admission itself; proxy authentication does not replace that application boundary. Accepted public hosts and forwarding trust are limited to explicit host, proxy, and restricted proxy-network configuration.

Forwarding consumes `X-Forwarded-For` and `X-Forwarded-Proto` with forward limit one. Explicit proxy and network trust lists limit accepted forwarding sources; without configured trust, forwarded-header processing is disabled. It preserves the public Host and does not enable `X-Forwarded-Host`. The reverse proxy preserves the public Host and scheme used to generate the callback origin. Unrelated peers are outside the forwarding trust boundary.

Container deployment uses a private network shared with the reverse proxy, no published application port, and a Host-aware health probe. Network wiring alone does not establish proxy trust or identity admission.

## Persistent and ephemeral runtime state

Runtime storage separates immutable application files in `/app`, external read-only configuration in `/config`, private persistent SQLite data and Data Protection keys in `/data`, and ephemeral browser/Xvfb state in `/tmp`. Only designated locations are writable, with permissions assigned to the non-root application user. Key persistence and backup requirements are defined in [authentication](authentication.md#configuration-and-data-protection).

The deployment model requires non-root execution, a read-only root filesystem, sandboxed Chromium, seccomp, `no-new-privileges`, `cap_drop: ALL`, controlled temporary/shared-memory storage, and no privileged Docker execution. Chromium operates under this hardened policy without additional privileges or disabled sandboxing.

Chromium still runs external content alongside application functionality, credentials, and persistent state. This accepted trade-off and the triggers for a separate worker are recorded in ADR-0007.

## Network egress boundary

Non-root execution, seccomp, Chromium sandboxing, and dropped capabilities do not restrict network reachability by themselves.

The target egress boundary permits configured library providers, DNS, the OIDC provider, and other explicitly required destinations, and prevents unnecessary access to unrelated private or administrative services. Its scope includes relevant container/browser processes. Effective egress enforcement is a production requirement, not a guarantee supplied by container hardening. Its mechanism depends on the deployment environment and requires operational validation.

## Secrets and observability

Frontend bundles and build-time configuration are public. Library credentials, provider sessions, OIDC client secrets, application session material, and Data Protection keys remain private server/runtime data, absent from frontend variables, Git, and image layers.

Structured synchronization diagnostics report outcomes, collection kinds, networks, item counts, and failure categories. Logs, metric labels, traces, and recordings exclude secrets and personal library activity. Callback URLs can contain authorization codes, so full request-URL informational logging is suppressed. Provider screenshots, traces, HAR files, and browser recordings may contain authenticated or personal data and are excluded from routine production telemetry. Repository fixtures are synthetic or anonymized, following [ADR-0001](../adr/0001-public-repository.md).
