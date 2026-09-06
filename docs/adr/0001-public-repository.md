# ADR-0001 — Public repository

## Status

Accepted

## Context

Librago is a personal self-hosted project, but the source repository is intended to be public.

The deployed application may contain private household information and credentials for external library accounts.

## Decision

The Librago repository will be public.

The repository must contain only generic application behavior and anonymized or synthetic test data.

No real credentials, session tokens, account identifiers, borrower identities, or private library activity may be committed.

## Consequences

- fixtures and debugging captures require careful anonymization;
- deployment-specific secrets stay outside version control;
- examples and screenshots must avoid exposing private library activity;
- the application design should avoid coupling core behavior to one household.
