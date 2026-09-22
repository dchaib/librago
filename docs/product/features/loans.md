# Loans

## Goal

Provide a single, family-wide list of current loans across all configured library accounts and library networks.

## Main user story

> As the person managing the family library accounts, I want to see all current loans in one list sorted by due date so that I immediately know which items must be returned first.

## Information hierarchy

Exact item metadata for version 0.1 is intentionally not fully specified.

The first implementation should be driven by data that can be reliably retrieved from external library portals, then refined through UI/UX iterations.

Each loan must identify:
- borrowed item;
- library network;
- due date;
- borrower.

Current visual priority:

Primary:
- borrowed item;
- relative time until due date;
- library network.

Secondary:
- borrower;
- absolute due date;
- borrowing date, if useful.

Additional metadata such as author, volume number, cover image, or material type should be evaluated once source data is known.

## Observed source data

The initial connector exploration established the following source capabilities. Source observations use these labels:

- **Confirmed**: directly observed from the source and represented only by anonymized fixtures or descriptions;
- **Assumed**: an implementation expectation that has not yet been observed from the source;
- **Synthetic invariant**: a deliberately invented failure or edge case used to test Librago's own behavior, not the external source.

| Source observation | Status | Basis |
| --- | --- | --- |
| Nantes settings response contains `ckSite`. | Confirmed | Observed response. |
| Nantes successful authentication response contains `token`. | Confirmed | Observed response. |
| Nantes nonempty loans response contains `items`, `total`, and loan `data` with title, author, borrowing date, due date, document number, ISBN, material category, series information, and branch data. | Confirmed | Observed response. |
| Nantes zero-loan response shape. | Assumed | See the open technical question. |
| Nozay loans page exposes borrower, material type, thumbnail, title, author, library, due date, and renewal information in an HTML table. | Confirmed | Observed page. |
| Nozay account page reports either zero current loans or a numeric current-loan count. | Confirmed | Observed page. |
| Nozay exposes a paginated loans list. | Assumed false until observed | Pagination is not implemented. |
| Nozay does not expose a borrowing date in the loans page. | Confirmed | Observed page. |

Version 0.1 therefore treats author, material type, branch, and borrowing date as optional. A missing optional value must not prevent an otherwise valid loan from being synchronized.

## Source response validation

Connectors distinguish a successful empty loan list from authentication/session failures and unexpected responses. Nantes validates the total returned by its API; the observed nonempty response contains both `items` and `total`. Nozay reads the current-loan count from the authenticated account page and verifies that it matches the rows retrieved from the observed loans page. The observed account-page summary explicitly distinguishes zero current loans from a positive count. The observed loans page explicitly displays `Pas de prêts en cours` when there are none; a missing loans table without that message is rejected. Pagination is not implemented until it is observed in the source.

## Borrower display names

Nozay may display a full borrower name while another network is configured with a shorter household display name. A configured Nozay account can provide `BorrowerAliases` to map a displayed Nozay name to the canonical name shown and filtered in Librago. Alias matching ignores case and repeated whitespace. An unmatched source name is preserved so that unexpected household activity remains visible.

When Nozay parent access lists the whole family, only that parent account should be configured for synchronization. This avoids duplicate records from separately configured child accounts.

## Default sort

Loans are sorted by due date ascending.

## Filters

Two combinable filters are required:
- library network;
- borrower.

## Deadline representation

The relative delay is the primary urgency signal.

French UI examples:
- `Aujourd'hui`
- `Demain`
- `Dans 2 jours`
- `En retard de 3 jours`

## Deadline severity

| Remaining time | Severity |
| --- | --- |
| Due date passed | Overdue / critical |
| 0–2 days | Very close |
| 3–6 days | Close |
| 7–13 days | Watch |
| 14 days or more | Normal |

Exact visual styling is intentionally left open.

Urgency must not be communicated by color alone.

## Synchronization and last known state

Librago preserves the last known state when synchronization fails.

A synchronization attempt has one of these results:
- `Success`
- `Partial`
- `Failed`

A library network is fully up to date only when all configured accounts for that network synchronize successfully.

Librago distinguishes:
- last synchronization attempt;
- last complete successful synchronization;
- synchronization result/status.

On startup, Librago synchronizes immediately when a configured network has no previous attempt or when its previous attempt is due according to the configured interval. Otherwise, it waits until the next due synchronization. This prevents application restarts from causing unnecessary upstream requests. The default interval is six hours and can be configured.

A network's last complete successful synchronization applies only to the exact set of configured accounts that completed it. Adding, removing, or restoring an account requires a new complete synchronization before the network is presented as fully current.

When an account is removed from configuration, its stored loans are hidden and are no longer synchronized. They are retained locally so that a temporary configuration mistake does not destroy the last known state. Re-adding the same stable `AccountId` requires a new synchronization before its data is treated as current.

## Stale data

For version 0.1, network data is stale when there has been no complete successful synchronization for more than 24 hours.

Stale or partially refreshed data should be visibly indicated, independently from loan overdue status.

## Refresh status

No dedicated synchronization history/status page is required in version 0.1.

At the bottom of the page, show one line per network with the last complete successful refresh time.

Manual refresh is excluded from version 0.1.

## Initial state and failures

The UI distinguishes:
- successful synchronization returning zero loans;
- failed synchronization with a previous known state;
- a network that has never synchronized successfully.

If synchronization fails and previous data exists, display the last known state and indicate that it may be stale.

## Authentication

Application-level authentication is outside version 0.1.

An upstream reverse proxy may provide temporary protection.

## Acceptance criteria

Version 0.1 is usable when:
- current loans from all configured accounts are visible in one list after successful synchronization;
- each loan identifies at least the item, library network, borrower, and due date;
- relative deadline information is displayed;
- loans are sorted by due date ascending;
- loans can be filtered by network and borrower;
- filters can be combined;
- overdue loans are clearly distinguishable;
- approaching deadlines have an urgency indication;
- failures preserve and display the last known state when available;
- partially refreshed networks are not presented as fully up to date;
- data older than 24 hours since the last complete successful synchronization is marked as stale;
- the last complete successful refresh time is shown for each network;
- a never-successfully-synchronized network is distinguished from a network with zero loans;
- the interface is usable on a phone.
