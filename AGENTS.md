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
- structural technical decisions belong in `docs/adr/`;
- the root `README.md` is public-facing and should not be used as an agent instruction file.

## Current first slice

The first slice is the consolidated loans list.

Important constraints:
- automatic synchronization is required;
- manual refresh is not part of version 0.1;
- application-level authentication is not part of version 0.1;
- failed synchronization preserves last known state;
- a network is fully up to date only when all configured accounts for that network synchronize successfully;
- exact loan metadata and UI details should be refined after technical exploration.

## Domain language

Use generic domain concepts such as:
- `Loan`
- `Reservation`
- `Borrower`
- `LibraryAccount`
- `LibraryNetwork`

Avoid embedding real library names, real borrower names, or household-specific data in the core domain.

Network-specific behavior belongs in network-specific connectors or adapters.
