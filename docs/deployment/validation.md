# Production deployment validation

This checklist verifies the accepted architecture in the actual production environment. All checks remain pending. Acceptance is specific to the chosen OIDC provider, reverse proxy, their configuration, and the target host; local development checks do not establish production acceptance.

Requirements are defined in [authentication](../architecture/authentication.md) and [security](../architecture/security.md). Run the checks with private configuration, mark them complete only after verification, and keep credentials, cookies, keys, personal data, and sensitive evidence outside public repository artifacts.

## Acceptance checks

### OIDC provider

- [ ] Verify that real HTTPS OIDC discovery and login succeed with the confidential client.
- [ ] Verify that the registered production `/signin-oidc` callback URI completes login.
- [ ] Verify that an admitted `(issuer, subject)` succeeds and an unknown identity is denied.
- [ ] Verify that CSRF-protected local logout clears Librago state/cookie.
- [ ] Verify that local logout does not initiate OIDC provider sign-out; subsequent login follows the provider's SSO session state.

### Reverse proxy and public hosting

- [ ] Verify that public Host and HTTPS forwarding are correct, headers are narrowly trusted, and untrusted forwarding is rejected.
- [ ] Verify that the generated OIDC callback uses the correct public origin.
- [ ] Verify that HSTS is present at that origin and no application port is directly published.

### Data Protection

- [ ] Verify that `/data/keys` persists in the intended volume with restrictive directory/file permissions.
- [ ] Verify that a real browser session survives container destruction/recreation with the volume retained.
- [ ] Verify that sensitive key backups are access-controlled and recoverable with the stable discriminator.

### Browser security

- [ ] Verify the documented CSP response header at the public HTTPS origin with production React assets, without development/HMR allowances.
- [ ] Check for CSP violations while opening/closing Dialog and Select portals (including aligned Select), in Ocean/Natural and light/dark, with normal and reduced motion.
- [ ] Verify keyboard selection, nested Escape handling, focus return, background scroll locking/restoration, and scrollbar hiding under that policy. Local frontend probes do not complete these deployment checks.

### Chromium/runtime

- [ ] Verify that the Chromium sandbox works on the target host for required browser modes.
- [ ] Verify that seccomp, `cap_drop: ALL`, `no-new-privileges`, non-root execution, and read-only root are enforced.
- [ ] Verify that writable paths are controlled, with no privileged execution or unnecessary capabilities.

### Network egress

- [ ] Verify that required library provider, OIDC provider, DNS, and other explicitly necessary destinations remain reachable.
- [ ] Verify that unrelated private and administrative services are unreachable.
- [ ] Verify that restrictions cover the relevant container/browser processes and are enforced after recreation in the production environment.

## Logout after admission revocation

The logout authorization contract requires review before acceptance. Its current [route description](../architecture/authentication.md#route-contract) requires admission and antiforgery. A valid session whose identity has been removed from the admission set can therefore be denied logout before its local cookie is cleared.

Proposed correction, pending confirmation: `POST /auth/logout` requires an authenticated application session and antiforgery validation, clears the local cookie, and returns `204`; continuing household admission is not required. Household data access remains admission-protected.

Verify this scenario with an existing valid cookie after removal from the admission set. Resolve how the client obtains valid CSRF state when its token is missing or expired: `GET /api/session` currently also requires admission. The route contract and implementation remain unchanged until this behavior is resolved.
