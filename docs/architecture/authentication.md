# Authentication and household access

[ADR-0005](../adr/0005-oidc-web-security.md) defines the authentication and authorization boundary.

## Identity and admission

```text
Browser -- Secure application cookie --> Librago / ASP.NET Core
                                           |
                                OIDC Authorization Code + PKCE
                                           |
                                           v
                                      OIDC provider
```

ASP.NET Core is a confidential client of an external OpenID Connect (OIDC) provider. It validates protocol tokens, then looks up the validated `(issuer, subject)` in the explicit admission set. Email and display name are mutable attributes; borrower names are domain/presentation information, not security identities.

OIDC provider authentication alone does not authorize access. Unknown/unadmitted identities are denied, and protected application access checks admission. Every admitted user shares all configured household accounts, without per-account or borrower grants.

The server establishes its own cookie session. React never receives OIDC access/refresh tokens or user credentials. OIDC uses the `openid` scope, PKCE, HTTPS metadata, and unmapped issuer/subject claims. Protocol tokens are not persisted (`SaveTokens=false`).

## Route contract

| Route | Behavior |
| --- | --- |
| `GET /auth/login?returnUrl=...` | Explicit interactive OIDC challenge; accepts a local path only; invalid return URL is `400` |
| `/signin-oidc` | Server-owned OIDC callback; never a SPA route |
| `/signout-callback-oidc` | Reserved OIDC handler path; local logout does not initiate OIDC provider sign-out |
| `GET /api/session` | Admitted session bootstrap returning local user id and antiforgery request token |
| Protected `/api/*` | Unauthenticated `401`, authenticated but forbidden `403`; no login redirect/HTML |
| `POST /auth/logout` | Admission and antiforgery required; clears local cookie and returns `204` |

## Application session

The application cookie, `__Host-Librago.Auth`, is `Secure`, `HttpOnly`, `SameSite=Lax`, scoped to path `/`, and host-only with no `Domain`. The browser stores and sends it with same-origin API requests; frontend JavaScript cannot read it. The cookie is non-persistent, with a bounded session ticket lifetime (eight-hour baseline) and sliding renewal disabled. OIDC nonce/correlation cookies have protocol-specific requirements and are separate from this application cookie.

Local logout is a CSRF-protected POST. On success, the frontend clears private data and in-memory session/CSRF state. Loss of authentication also clears private state. Logout clears only Librago's session; the OIDC provider's SSO session can remain active, so a subsequent explicit login may require no new credentials.

Revocation at the OIDC provider does not necessarily revoke an issued application cookie immediately. Bounded lifetime is the accepted residual risk; immediate global logout or revocation is not promised.

## Antiforgery contract

Cookie-authenticated POST, PUT, PATCH, and DELETE undergo explicit ASP.NET Core Antiforgery validation before handlers execute. GET/HEAD application endpoints do not perform business mutations. SameSite is an additional defense, not a replacement for token validation.

The authenticated bootstrap issues an antiforgery companion cookie and a request token. The companion, `__Host-Librago.Csrf`, is Secure/HttpOnly, path `/`, host-only with no Domain, and `SameSite=Strict`. React stores the request token only in memory; the central API client sends `X-CSRF-TOKEN` with mutations. Session material and request tokens are absent from browser persistent storage such as localStorage.

Validation failure returns `400` Problem Details with `code=csrf_invalid`. The client can bootstrap fresh CSRF state after expiration/validation failure. It does not automatically replay the failed mutation or interpret failure as success; retry depends on explicit user action. Logout and authentication loss clear the in-memory tokens.

## Configuration and Data Protection

Private server configuration supplies the OIDC authority, confidential client credentials, and explicit admission entries mapping validated `(issuer, subject)` pairs to stable local user ids. Admission pairs and local ids are unique. Secrets and admission data are absent from frontend build variables.

Data Protection keys persist at `/data/keys` with stable application discriminator `Librago`. The directory/volume and discriminator persist across recreation. Directory and file permissions restrict access to the key ring; backups are sensitive. Keys are absent from Git and image layers. An external vault is not mandatory; filesystem persistence does not imply encryption at rest.

Operational checks for discovery, callbacks, admission, logout, and browser-session continuity are recorded in the [deployment validation checklist](../deployment/validation.md).
