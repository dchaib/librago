# Nozay connector reference

## Scope and implementation

Current loans and current reservations are implemented. Product behavior is defined in [Loans](../product/features/loans.md) and [Reservations](../product/features/reservations.md).

Implementation entry points:

- [NozayLibraryConnector](../../src/Librago/Connectors/Nozay/NozayLibraryConnector.cs): browser session, account count, and loan-page extraction.
- [NozayLoanRowParser](../../src/Librago/Connectors/Nozay/NozayLoanRowParser.cs): column mapping, borrower aliases, and loan identity.
- [NozayLibraryConnectorTests](../../tests/Librago.Tests/NozayLibraryConnectorTests.cs) and [NozayLoanRowParserTests](../../tests/Librago.Tests/NozayLoanRowParserTests.cs): current extraction and parser checks.
- [NozayLibraryConnector.Reservations](../../src/Librago/Connectors/Nozay/NozayLibraryConnector.Reservations.cs): reservation summary, scoped navigation, and list extraction.
- [NozayReservationRowParser](../../src/Librago/Connectors/Nozay/NozayReservationRowParser.cs) and [ReservationParserTests](../../tests/Librago.Tests/ReservationParserTests.cs): reservation mapping and synthetic parser checks.

## Access and session

Base URL: `https://www.cc-nozay-bibliotheques.fr`.

Use Playwright Chromium with `Headless = false`, the browser sandbox enabled, and locale `fr-FR`. The current connector uses 30-second timeouts. Container deployments provide a display through Xvfb.

1. Open `/accueil`.
2. If the title is `Making sure you're not a bot!`, wait for the portal's Anubis challenge to finish before authenticating.
3. Fill `input[name='username']` and `input[name='password']` from configured credentials, submit with Enter, and wait for the resulting document.
4. Open the authenticated account page, currently `/abonne/fiche/id_profil/1`, to obtain counts before reading lists.
5. Keep the same browser context for the account summary and both list requests. Loans and reservations use one authentication and one browser session per account synchronization. Reject an authenticated-page read if the login input remains visible.

When a parent account covers the family, configure that parent account only. Use the row's borrower name, falling back to the configured borrower if blank. Apply `BorrowerAliases` with case-insensitive matching after whitespace normalization; preserve unmatched names. Apply this to both loans and reservations. Borrower aliases do not affect source loan or reservation identities.

## Loans

### Retrieval

The current loan route is `/abonne/prets/id_profil/1`.

Read the count from `.abonneFiche.prets` on the account page: `Vous avez N prêts en cours` or `Vous n'avez aucun prêt en cours.`. Accept singular/plural and straight/curly apostrophes.

Read data rows from `#borrower_loans tbody tr`. Extract cell text and the relevant link URLs separately.

### Data mapping

Column indices are zero-based; the current parser requires at least eight cells.

| Column/source | Librago value | Handling |
| --- | --- | --- |
| 0 — borrower | Borrower | Source name, configured fallback, then aliases; required |
| 1 — support | Material type | Optional |
| 2 — thumbnail | No current snapshot value | Not needed for the current loan implementation |
| 3 — title | Title | Required; catalog notice links are not used for loan identity |
| 4 — author | Author | Optional |
| 5 — library | Library | Optional |
| Library identity | Absent | Leave null |
| 6 — due-date text and first link | Due date; preferred loan ID | Required date; loan ID from `/id_pret/{value}` |
| Borrowing date | Absent | Leave null |

Extract the `dd/MM/yyyy` date from the whole due-date cell, which can also contain renewal text. Normalize repeated whitespace and treat blank optional text as absent.

### Identity and validation

Use `nozay-loan:{id}` from the loan link's `id_pret`. A catalog notice ID is not a loan ID.

A missing or invalid `id_pret` fails the entire account loan refresh with invalid data, including when a renewal link is absent. Duplicate loan IDs are an unexpected response. Preserve the previous loan state on failure; do not generate identities or occurrence suffixes.

Validate parsed row count against the account summary. A missing table is accepted only with the exact normalized message `Pas de prêts en cours` in `.contenuInner > p.error`, and only if the expected count is zero. A present empty table also needs a matching zero count.

Reject missing required columns, title, borrower, or a recognizable valid due date. Pagination is not implemented; a count mismatch must fail rather than accept a truncated list.

## Reservations

### Retrieval

Read the account count from `.abonneFiche.reservations`: `Vous avez N réservation(s) en cours` or `Vous n'avez aucune réservation en cours.`.

The list route is `/abonne/reservations/id_profil/{profile}`. Obtain the reservation link from the authenticated summary and keep its profile scope consistent with the count. Do not copy profile values from account-specific captures.

The source table has classes `tablesorter reservations`; use `table.reservations tbody tr` for its data rows. A successful zero list retains the table headers and an empty body.

### Data mapping

Column indices are zero-based; the current reservation table has nine cells per data row.

| Column/source | Librago value | Handling |
| --- | --- | --- |
| 0 — Réservé par | Borrower | Use the same fallback and alias handling as loans |
| 1 — Support | Material type | Optional |
| 2 — Vignette | Secondary image metadata | Add only if needed by the UI |
| 3 — Titre and link | Title; catalog notice reference | Title required; notice ID is not a reservation ID |
| 4 — Auteur | Author | Optional |
| 5 — Bibliothèque | Pickup-library name | Scope filter identity to the network |
| 6 — État | Source status label and pickup deadline | `Disponible jusqu'au 13 octobre` supplies a day and month; can be blank |
| 7 — Rang | Queue position | Numeric text, optional |
| 8 — Suppr. link | External reservation identity | Read the full `id_delete` value; do not follow the deletion link |
| Reservation date | Absent | Use the generic first-observation estimate |
| Availability date | Absent | Estimate only once seen as available |
| Pickup deadline | Day and month in the available-state label | Resolve the missing year against the synchronization date in the configured time zone |

Normalize cell whitespace as for loans. For `Disponible jusqu'au <day> <French month>`, accept straight or curly apostrophes and resolve the year to the closest valid occurrence around the synchronization date. This keeps an already passed pickup date in the past rather than moving it to the next year. Reject an invalid day or month so a malformed available-state deadline cannot silently replace known data.

First-observation estimates are supplied by persistence, not by the connector. Deletion links are read only for identity and are never followed. Only the portal's same-origin reservation-list route is accepted for navigation.

### Status mapping

| Source label | Product group | Confidence |
| --- | --- | --- |
| Blank | Unavailable; fallback label `Pas encore disponible` | Working interpretation, pending additional states |
| `Disponible jusqu'au <day> <month>` | Available | Confirmed with a single available-state example; supplies the pickup deadline |
| `Disponible` | Available without a deadline | Expected label, not yet confirmed |
| Other nonblank text | Unknown until mapped | Preserve the network wording |

Do not classify every nonblank label as available. Intermediate and suspended states remain unspecified. Log an unrecognized status with sanitized diagnostics so its mapping can be added.

### Identity and validation

Treat the entire deletion-link `id_delete` value, currently two numeric components joined by an underscore, as an opaque account-scoped identifier candidate. Do not derive identity from title, queue position, or status. Its uniqueness and stability still need confirmation.

Validate data rows against the account count before excluding terminal records. Accept zero only with a recognized zero summary and the reservation table's empty body; a missing table or unreadable summary is an unexpected response. Reject a count mismatch rather than accepting partial data.

Require an identifiable borrower and title. Blank state and missing optional dates are supported. Distinct reservations must remain distinct, and first-observation dates must survive status changes. Do not add reservation pagination until source behavior requires it.

The connector reads the source's current list without inferring additional terminal states. Removed rows disappear on a successful refresh; unrecognized nonblank labels remain unknown until their meaning is confirmed.

## Failure handling

Use `LibraryConnectorException` categories: authentication when the login page is still shown, upstream for failed navigation or Playwright operations, unexpected response for absent/unreadable summaries or tables and count mismatches, and invalid data for unusable record fields.

Let synchronization preserve the previous state on failure. Keep browser sessions, credentials, borrower names, account identifiers, and raw page content out of diagnostics and committed artifacts.

## Integration questions

- Obtain reservation examples with multiple records, multiple borrowers, and different statuses.
- Confirm other available and intermediate status labels and any other pickup-deadline formats.
- Confirm reservation count/list coverage for family accounts and whether pagination is needed.
- Confirm reservation-identifier uniqueness and stability across status changes.
- Confirm how terminal reservations are represented so canceled or completed records are excluded.
