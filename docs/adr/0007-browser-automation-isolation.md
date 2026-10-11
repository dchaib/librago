# ADR-0007 — Retain provider automation in the hardened application container

## Status

Accepted

## Context

Some library portals require JavaScript execution, so Librago uses Playwright and Chromium to retrieve account information.

Chromium processes externally supplied web content while Librago also handles library credentials and persistent application data. Running the browser in a separate worker/container could improve isolation, but would introduce additional deployment and communication complexity for this self-hosted application.

This decision determines whether browser automation remains in the application container and which security conditions make that approach acceptable. Browser sandboxing and container hardening do not restrict network reachability by themselves.

## Decision

Initially keep provider automation in Librago's application container. Require non-root execution, a read-only root filesystem, Chromium sandboxing, seccomp, `no-new-privileges`, dropped Linux capabilities, and controlled writable directories. Privileged execution is not required. Runtime requirements belong in the [security reference](../architecture/security.md); operational checks belong in [deployment validation](../deployment/validation.md).

Accept that externally supplied web content executes in a container that also holds application functionality, provider credentials, and persistent state. This is an explicit security trade-off, not isolation equivalent to a separate worker.

Effective network egress restrictions are an outstanding production requirement. Permit configured library providers, DNS, the OIDC provider, and other explicitly necessary destinations while preventing unnecessary access to unrelated private or administrative services. Container hardening does not establish that boundary; the final network policy requires validation in the actual production environment.

## Consequences

- Preserve a single application deployment and existing connector orchestration, without introducing a browser service now.
- Namespace-local `chroot` can be permitted by seccomp to support sandboxing with `cap_drop: ALL`, without granting extra container capabilities.
- Validate the sandbox and network policy on the target host. Local success is not proof of enforcement in the production environment .
- Reconsider a separate browser worker if sandboxing must be disabled, arbitrary/user-configurable provider URLs or third-party plugins are added, significant privileges become necessary, egress cannot be sufficiently restricted, or the provider/browser threat model changes materially.
- [ADR-0001](0001-public-repository.md)'s privacy constraints apply to browser captures and diagnostics as well as source and fixtures.
