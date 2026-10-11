# ADR-0005 — Server-side OIDC and the application session boundary

## Status

Accepted

## Context

Librago contains library account information shared within a household. Only explicitly authorized users should be able to access it, even when other users can authenticate through the same identity provider (IdP).

The [web frontend](0004-react-frontend.md) needs a secure authenticated session without exposing OpenID Connect (OIDC) tokens or credentials to frontend JavaScript.

This decision defines how external identities are authenticated, admitted to Librago, and represented by application sessions.

## Decision

ASP.NET Core acts as a confidential OIDC client using Authorization Code with PKCE, keeping the client secret server-side. Before issuing its own bounded, non-persistent application session cookie, it checks admission: the validated external `(issuer, subject)` pair must appear in Librago's explicit list of identities authorized to access the application. Email, display name, and borrower name are not security identifiers. React never handles OIDC access/refresh tokens or credentials; tokens are not saved when no subsequent server operation needs them.

Authentication establishes identity; admission determines who may enter Librago. Household authorization determines what admitted users can access: every admitted user shares the same configured household library accounts. Unknown identities are denied. Do not introduce per-account or borrower grants without a concrete requirement.

API challenges return `401`, and authenticated forbidden requests return `403`, without redirects to login HTML. Interactive login is explicit. Logout is a CSRF-protected POST that ends the local session and clears private frontend state, without initiating OIDC provider sign-out.

Cookie-authenticated mutations require ASP.NET Core Antiforgery validation. Request tokens live in frontend memory and are attached centrally; failed mutations are never automatically replayed after token refresh. GET/HEAD application endpoints remain side-effect free.

Persist Data Protection keys with a stable application discriminator in private persistent storage, so container recreation does not invalidate otherwise valid sessions. The [authentication reference](../architecture/authentication.md) owns route, cookie, and configuration details; the [security reference](../architecture/security.md) owns browser and runtime requirements. Operational checks belong in [deployment validation](../deployment/validation.md).

## Alternatives

- **Browser-managed bearer tokens:** introduces token handling in the SPA without a need in a same-origin deployment.
- **Proxy-only authentication:** does not provide Librago's own OIDC session and explicit identity admission boundary.
- **Automatic admission of IdP users:** confuses successful authentication with permission to see household data.
- **Granular grants now:** adds policy complexity without a current product requirement.

## Consequences

- OIDC configuration and secret handling stay server-side; the frontend bundle is public.
- Secure, host-only cookies and narrowly trusted proxy forwarding require correct HTTPS deployment.
- IdP revocation does not necessarily end an already-issued application session immediately. Bounded lifetime is the accepted residual risk; immediate global revocation is not promised.
- Key material requires restrictive permissions and sensitive backups. No external vault is mandated.
- Login, admission, callbacks, and logout with the chosen OIDC provider must be validated before relying on this boundary in production.
- Future non-browser authentication, including PATs, remains deferred.
