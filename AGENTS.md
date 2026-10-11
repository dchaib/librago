# Librago development guidelines

## Project principles

- Librago is a self-hosted web application.
- The repository is public.
- Code and repository documentation are written in English.
- The user interface is primarily intended to be in French.
- Prefer simple solutions over speculative abstractions.
- Implement features as small vertical slices.
- Do not design distant roadmap items in detail before they approach implementation.

## Privacy and security

- Never commit real credentials.
- Never commit session cookies, access tokens, or authentication captures.
- Never commit real library account identifiers.
- Never commit real borrower names or other household-specific personal data.
- Test fixtures must be synthetic or carefully anonymized.
- Review screenshots and captured HTML/JSON for personal data before committing them.

## Documentation

Read [`docs/README.md`](docs/README.md) before adding or restructuring documentation.

- product behavior belongs in `docs/product/`;
- unresolved product questions belong in `docs/product/open-questions.md`;
- shared visual principles, foundations, and UI component guidance belong in `docs/design/`;
- structural technical decisions belong in `docs/adr/`;
- living technical architecture and deployment requirements belong in `docs/architecture/`;
- the root `README.md` is public-facing and should not be used as an agent instruction file.

## Working with the architecture

- Follow the actual repository structure and implemented behavior when changing code; use the [README workflows](README.md#development) for the current checkout.
- Refer to [the architecture overview](docs/architecture/overview.md) and linked ADRs for accepted technical decisions. Review the corresponding decision explicitly before introducing a contradictory change.
- Keep `main` functional and releasable through small vertical slices. Track migration progress in the [product roadmap](docs/product/roadmap.md).
- Follow the existing design documentation for visual and interaction rules, and the [security reference](docs/architecture/security.md) for deployment requirements.

Preserve these product constraints:

- automatic synchronization is required; manual refresh remains deferred;
- failed or incomplete synchronization preserves last known state;
- a network collection is fully current only when all its configured accounts synchronize that collection successfully;
- loan and reservation confidence remain independent;
- API responses and displayed summaries must preserve durable account/collection coverage; missing rows do not prove a verified empty snapshot;
- shared bounded synchronization concurrency must precede a manual synchronization API;
- admitted application users share household accounts; borrower names are presentation/domain information, not security identities.

## Domain language

Use generic domain concepts such as:
- `Loan`
- `Reservation`
- `Borrower`
- `LibraryAccount`
- `LibraryNetwork`

Avoid embedding real library names, real borrower names, or household-specific data in the core domain.

Network-specific behavior belongs in network-specific connectors or adapters.
