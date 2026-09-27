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
- borrowing date.

Additional metadata such as author, volume number, cover image, or material type should be evaluated once source data is known.

## Optional information and completeness

Author, material type, library, and borrowing date are optional. A missing optional value must not prevent an otherwise valid loan from appearing.

`Library` is the library reported for the loan. Current source data does not establish that it is necessarily the borrowing or return location. Reservations use `PickupLibrary` for their explicit pickup location.

`LibraryId` is optional and scoped to the library network. Preserve it when supplied, independently of the display name. Existing loans migrated from earlier schemas have no library identity until a successful synchronization supplies one.

Distinguish a successful empty list from a failed or incomplete refresh. An incomplete response must preserve the last known state rather than make loans disappear.

## Borrowing date

Display the network-provided borrowing date when available. Otherwise, estimate it from the first observation of the loan in the configured application time zone. Mark estimates with an asterisk linking to an explanation that they can be later than the actual borrowing date.

Require a nonempty source identity for each loan, unique within its account. Reject an entire account loan snapshot when an identity is missing or duplicated; preserve the previous state and allow reservations to update independently. Existing stored identities are retained until a successful refresh replaces them. Switching to a different source identity resets its first observation without heuristic matching.

Preserve the first observation across refreshes and restarts for the same account, network, and loan identity. A supplied date takes priority. A loan removed by a successful refresh and later observed again gets a new estimate. Failed refreshes preserve the previous state.

For loans stored before this feature, use their last recorded refresh as the earliest known observation. The return deadline continues to come from the network.

## Borrower display names

Use consistent household display names across networks when configured. Preserve an unexpected source name when no display-name mapping exists so that the associated loans remain visible.

## Network references

Available fields, account scope, and connector constraints are described separately in [Nantes](../../networks/nantes.md) and [Nozay](../../networks/nozay.md).

## Default sort

Loans are sorted by due date ascending.

## Filters

Two combinable filters are required:
- library network;
- borrower.

## Deadline representation

Use two lines, consistent with the reservations list: a short relative delay as the primary urgency signal, followed by the action and absolute deadline, `Retour au plus tard le 29 septembre 2026`.

French UI examples:
- `Aujourd’hui`
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

When an account is removed from configuration, its stored loans are hidden and are no longer synchronized. They are retained locally so that a temporary configuration mistake does not destroy the last known state. Restoring an account requires a new synchronization before its data is treated as current.

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
