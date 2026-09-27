# Nantes connector reference

## Scope and implementation

Current loans and current reservations are implemented. Product behavior is defined in [Loans](../product/features/loans.md) and [Reservations](../product/features/reservations.md).

Implementation entry points:

- [NantesLibraryConnector](../../src/Librago/Connectors/Nantes/NantesLibraryConnector.cs): session, requests, and loan pagination.
- [NantesLoanParser](../../src/Librago/Connectors/Nantes/NantesLoanParser.cs): current loan extraction and validation.
- [NantesLoanParserTests](../../tests/Librago.Tests/NantesLoanParserTests.cs): loan parser checks.
- [NantesReservationParser](../../src/Librago/Connectors/Nantes/NantesReservationParser.cs): complete reservation snapshot validation and mapping.
- [ReservationParserTests](../../tests/Librago.Tests/ReservationParserTests.cs): synthetic reservation parser checks.

## Access and session

Base URL: `https://catalogue-bibliotheque.nantes.fr`.

Use an HTTP client with a cookie container and redirects disabled. Send `Accept: application/json` and the `X-microsite-id` value defined in the connector.

1. GET `/in/rest/api/settings.json` and read the nonblank `ckSite`.
2. POST JSON to `/in/rest/api/authenticate`, using the configured `username` and `password`, with `birthdate: ""`, `locale: "fr"`, `pin: null`, and `v3Token: ""`.
3. Require a nonblank `token` in the authentication response.
4. For each account-list request, send `X-InMedia-Authorization: Bearer {token} {ckSite} {signature}`.

Calculate the signature from the exact query string, including its initial `?`: normalize to Unicode Form C, start at `305419896`, and add each UTF-16 character value multiplied by its one-based position. Reuse the connector's request-signature method when adding reservations.

The borrower display name comes from the configured account's `Borrower`. Keep source identifiers scoped to that account's local `AccountId`. A single account session retrieves both loans and reservations; each query receives its own signature while reusing the session token and cookies.

## Loans

### Retrieval

GET `/in/rest/api/accountPage?type=loans&pageNo={page}&pageSize=100&locale=fr`.

Start at page 1. Append each page's records until the retrieved count reaches `total`. A page ending before the total is reached is an incomplete response.

### Data mapping

The response contains `items` and `total`; each item's fields are under `data`.

| Source | Librago value | Handling |
| --- | --- | --- |
| Account configuration: `Borrower` | Borrower | Required, nonblank |
| `title` | Title | Required, nonblank |
| `returnDate` | Due date | Required, `dd/MM/yyyy` |
| `loanDate` | Borrowing date | Optional, `dd/MM/yyyy` when present |
| `author` | Author | Optional |
| `categoryLabel` | Material type | Optional |
| `branch.desc` | Library | Optional |
| `branch.branchCode` | Library identity | Optional; scoped to the network, consistent with reservation pickup-library identities |
| `documentNumber` | External loan identity | Required; string or number, normalized to a trimmed string |

Trim text; blank optional values become null. A supplied date must parse successfully. ISBN, series, and other source metadata are not needed for the current loan snapshot except as noted below.

### Identity and validation

Use account-scoped `documentNumber` as the required external loan ID, restoring the previously used source field. Accept strings or numbers and normalize to a trimmed string. Manual synchronization has shown that distinct loans in one account can share `omnidexId`, so it must not identify loans. Reject missing or invalid document numbers and duplicate IDs across the complete paginated collection. Do not fall back to `omnidexId` or a generated identity. Existing data remains until a successful refresh; a changed identity resets its first-observation estimate without heuristic matching.

Require `items` to be an array and `total` to be a nonnegative integer, even for an empty result. The valid empty loan response is `{ "items": [], "total": 0 }`. Each item must have `data`, title, and a valid due date; optional metadata can be absent.

Return success only when the complete retrieved count equals the total. Do not return an incomplete account snapshot.

## Reservations

### Retrieval

Use the same authenticated account-list request with `type=reservations`, initially `pageNo=1` and `pageSize=100`. Authentication and signature handling are shared with loans.

The working source limit is three reservations per account. Reservation pagination is not planned unless a response requires it. Validate `total` rather than relying on that limit.

### Data mapping

Reservation fields are also under `items[].data`.

| Source | Librago value | Handling |
| --- | --- | --- |
| Account configuration: `Borrower` | Borrower | Use the same display name as loans |
| `title` | Title | Required, nonblank |
| `author` | Author | Optional |
| `zmatDisplay` | Material type | Optional display text |
| `branch.branchCode`, `branch.desc` | Pickup-library identity and name | Keep identity separate from display text |
| `resvDate` | Reservation date | Optional, `dd/MM/yyyy`; use the product estimate if absent |
| `dateHoldForPickup` | Availability date | Optional, `dd/MM/yyyy`; use the product estimate if absent |
| `expiryDate` | Pickup deadline | Optional, `dd/MM/yyyy`; never estimate |
| `rank` | Queue position | Numeric text; rank `1` can remain present when available |
| `statusCode`, `statusDescription` | Status group and network label | Classify from the code; preserve the label |
| `startSuspendDate`, `endSuspendDate` | Suspension dates | Optional; source format still to confirm |
| `canceled` | Canceled flag | Exclude records explicitly marked canceled |
| `omnidexId` | External reservation identity | See identity rules |

Treat blank optional fields as absent. A supplied reservation, availability, or deadline date must be valid. Do not use `transactionLocation` as the pickup library or `holdAssignDate` as the availability date. Additional item metadata can be added when needed by the UI.

### Status mapping

| Full source code | Product group |
| --- | --- |
| `ReservationCard.RESV_AVAILABLE` | Available |
| `ReservationCard.RESV_SOON_AVAILABLE` | In transit / approaching availability |
| `ReservationCard.RESV_NOT_AVAILABLE` | Unavailable; source label is `Pas encore disponible` |
| `ReservationCard.RESV_SUSPENDED` | Suspended |

Keep unrecognized codes visible as unknown and log a warning with the network and status code. Diagnostics only include codes matching the bounded `ReservationCard.RESV_[A-Z_]+` pattern; other values are logged as unclassified. Do not infer physical transport, suspension, or resumption from dates alone.

### Identity and validation

Use account-scoped `omnidexId` as the primary identifier candidate and reject duplicates. Its stability across status changes still needs confirmation. If this identity proves insufficient, the alternative source key is `reservationId` when supplied, otherwise the tuple `seqNo`, `volume`, `omnidexId`, and `branch.branchCode`.

Apply the loans envelope checks: a required list, a nonnegative total, and a retrieved count matching the total before excluding terminal records. The expected empty response is `{ "items": [], "total": 0 }`; this reservation response still needs confirmation. Missing optional dates or an unknown status do not invalidate an otherwise usable record.

Stable identity is necessary to retain first-observation dates across refreshes. A changed status, rank, date, or pickup library must not silently create a new reservation.

The implementation accepts suspension dates in `dd/MM/yyyy`, pending source confirmation. It excludes explicit `canceled` and `completed` flags (boolean or boolean text), and `ReservationCard.RESV_CANCELED` / `ReservationCard.RESV_COMPLETED` codes. Those additional terminal representations are defensive mappings tested with synthetic fixtures; their actual use by the source remains unconfirmed. Other unknown codes remain visible. A count mismatch fails rather than replacing a snapshot with incomplete data.

## Failure handling

Use `LibraryConnectorException` categories: authentication for a missing session token, upstream for failed HTTP requests, unexpected response for invalid JSON/envelopes or incomplete lists, and invalid data for unusable record fields.

Let synchronization preserve the previous state on failure. Keep credentials, cookies, tokens, borrower names, account identifiers, and raw responses out of diagnostics and committed artifacts.

When retrieval fails, connector event 1203 identifies loans or reservations, a fixed diagnostic reason, and the exception type. Loan reasons distinguish invalid JSON (`InvalidLoansJson`), invalid envelopes (`InvalidLoansEnvelope`), missing items (`MissingLoanItems`), duplicate identities (`DuplicateLoanIdentities`), and count mismatches (`LoanCountMismatch`). Other connector failures use their failure category; unexpected exceptions use `UnhandledException`. Exception messages and stack traces are not logged. Event 1002 remains the synchronization-level failure summary; event 1202 concerns optional catalogue enrichment only.

## Integration questions

- Confirm the empty reservation response.
- Confirm reservation identifier stability across status changes and resolve identity conflicts if needed.
- Confirm suspension date format.
- Confirm any additional terminal-state representation beyond `canceled` so completed records are excluded.
