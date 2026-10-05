# Loans

## Goal

Provide a single, family-wide list of current loans across all configured library accounts and library networks.

## Main user story

> As the person managing the family library accounts, I want to see all current loans in one list sorted by due date so that I immediately know which items must be returned first.

## Information hierarchy

Exact item metadata is intentionally not fully specified.

Metadata choices should be driven by data that can be reliably retrieved from external library portals, then refined through UI/UX iterations.

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

Distinguish a successful empty list from a failed or incomplete refresh. An incomplete response must preserve the last known state rather than make loans disappear. A loan absent from a complete successful account snapshot is no longer current. All current loans, including overdue loans, belong to the unfiltered list.

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

Loans are sorted by due date ascending, including in filtered results.

## Filters

Three combinable filter dimensions are required:

- library network;
- borrower;
- deadline severity.

With no active filters, show all current loans from all configured accounts. Different filter dimensions combine with AND. Multiple selected severities combine with OR.

Filter option lists are independent of the active filters and based on the full current list and configured networks. Deadline options remain available even when no current loan matches them. Clearly show active filters and allow each filter, or all filters, to be cleared. Clearing all filters restores the family-wide list.

Initial filter state is defined in [Navigation and initial filters](#navigation-and-initial-filters).

### Deadline severity filter

Use the categories defined in [Deadline severity](#deadline-severity), without a separate deadline calculation for filtering.

Provide a single filter labelled `Échéance`, allowing one or several exact severity levels to be selected. With no level selected, there is no deadline restriction.

Support:

- no deadline restriction (the default);
- each exact severity: `Normal`, `Watch`, `Close`, `Very Close`, or `Overdue`;
- a combination of exact severities.

Selecting `Close`, `Very Close`, and `Overdue` together returns the union of those levels. Selecting only `Close` or only `Very Close` excludes overdue loans. An exact `Overdue` filter includes only loans whose due date has passed. Filters use exact deadline severity levels rather than business attention levels or their visual treatment.

The active selection must make each selected deadline level understandable. Overdue loans must remain explicitly identifiable as late, including within a combined result.

## Deadline representation

Use two lines, consistent with the reservations list: a short relative delay as the primary urgency signal, followed by the action and absolute deadline, `Retour au plus tard le 29 septembre 2026`.

French UI examples:

- `Aujourd’hui`
- `Demain`
- `Dans 2 jours`
- `En retard de 3 jours`

Calculate remaining days by comparing the deadline calendar date with today's calendar date in the application's configured time zone. The due date is inclusive: a loan due today has zero remaining days and becomes overdue on the following calendar day. Recalculate relative delays and severity as the day changes, even if no new synchronization has occurred.

## Deadline severity

These calendar-day thresholds apply to both list display and filtering. The reservations list uses the same thresholds, with `Expired` for a passed pickup deadline, as described in [Pickup deadline](reservations.md#pickup-deadline).

| Remaining time | Severity |
| --- | --- |
| Due date passed | `Overdue` |
| 0–2 days | `Very Close` |
| 3–6 days | `Close` |
| 7–13 days | `Watch` |
| 14 days or more | `Normal` |

Deadline severity and business attention level are distinct. An overdue loan requires critical attention because its due date has passed and it may have consequences on the library account. A loan with `Very Close` severity requires urgent attention because its deadline is imminent. These attention levels do not prescribe a visual treatment.

Exact visual styling is intentionally left open.

Urgency must not be communicated by color alone.

## Navigation and initial filters

The loans page is reachable through application navigation and is usable on a phone.

The page can be opened with explicit initial filters. These replace any previously used list filters; unspecified dimensions have no restriction. Opening the page without explicit filters shows the full family-wide list. There is no filter persistence between pages.

Explicit initial filters behave like filters selected on the page: they remain visible, can be adjusted or cleared, and preserve the default sort and data-confidence indications. Multiple initial deadline levels appear as selected levels in the same `Échéance` filter.

## Synchronization and last known state

Librago preserves the last known state when synchronization fails.

A synchronization attempt has one of these results:

- `Success`
- `Partial`
- `Failed`

Loan data for a library network is fully up to date only when all configured accounts for that network synchronize their loans successfully. Loan and reservation updates are independent: success for one collection must not make the other appear current, and a failure for one must not prevent successful updates to the other.

For each network and collection, Librago distinguishes:

- last synchronization attempt;
- last complete successful synchronization;
- synchronization result/status.

On startup, Librago synchronizes immediately when a configured network has no previous attempt or when its previous attempt is due according to the configured interval. Otherwise, it waits until the next due synchronization. This prevents application restarts from causing unnecessary upstream requests. The default interval is six hours and can be configured.

A network collection's last complete successful synchronization applies only to the exact set of configured accounts that completed it. Adding, removing, or restoring an account requires a new complete synchronization before that collection is presented as fully current.

When an account is removed from configuration, its stored loans are hidden and are no longer synchronized. They are retained locally so that a temporary configuration mistake does not destroy the last known state. Restoring an account requires a new synchronization before its data is treated as current.

## Stale data

Loan data for a network is stale when there has been no complete successful loan synchronization for more than 24 hours. When no complete successful synchronization exists, explicitly indicate incomplete or unavailable coverage as appropriate instead of implying that data is current.

Stale, failed, or partially refreshed data must be visibly indicated, independently from loan urgency. A recent complete snapshot does not hide a subsequent failed attempt.

## Refresh status

A dedicated synchronization history/status page and manual refresh are outside the scope of this specification.

At the bottom of the list page, show one line per network with the last complete successful loan refresh time, or indicate that no complete successful loan refresh exists.

## Known data coverage

For each configured account and collection, distinguish a complete successful snapshot from the absence of any usable snapshot for that account and network. A successful empty snapshot is known data; the presence or absence of item records alone cannot establish whether an account has synchronized successfully. Preserve this distinction across later failed attempts and application restarts.

For the selected network scope:

- if no configured account has a usable successful snapshot, the collection is unavailable;
- if some accounts have usable snapshots and others do not, counts cover only known data and must be qualified as partial, including when that known count is zero;
- if all accounts have usable snapshots, last-known coverage is complete, but data may still be stale or affected by a failed or partial refresh.

The latest attempt result describes the update, not the coverage of preserved data. A `Partial` attempt does not necessarily mean that some accounts have no known data. Conversely, complete last-known coverage does not satisfy the rules for presenting data as fully current: a complete successful refresh for the exact configured account set is still required, with freshness and subsequent failures taken into account.

These coverage rules also apply independently to reservations.

## Initial state, failures, and empty results

The UI distinguishes:

- successful synchronization returning zero current loans;
- no loans matching the active filters;
- failed synchronization with a previous known state;
- partial coverage of configured accounts;
- a network with no usable loan data.

If synchronization fails and previous data exists, display the last known state and identify the affected network's loan collection as not successfully updated. Preserve successful updates for other accounts. Apply list filters to usable last-known records as well as successfully refreshed records. Retain indications of stale, failed, partial, or unavailable data for the selected network scope even when filters produce no results.

Qualify totals and matching counts as known or partial when coverage is incomplete. When no usable data exists, indicate that loan data is unavailable and use an unavailable value such as `—` rather than zero. A network that has never completed a successful refresh may still have usable partial data from some accounts.

A verified empty state requires complete, reliable data for the selected scope. When known records do not match the filters but data is incomplete, unavailable, stale, or affected by a failed update, say that no known loans match and retain the data-confidence indication; do not claim there are no current loans or nothing requiring attention. Offer filter adjustment or clearing for an empty filtered result.

## Authentication

Application-level authentication is outside the scope of this specification.

An upstream reverse proxy may provide temporary protection.

## Acceptance criteria

The loans list is functionally complete when:

- current loans from all configured accounts are visible in one list after successful synchronization;
- each loan identifies at least the item, library network, borrower, and due date;
- relative deadline information is displayed;
- loans are sorted by due date ascending, including in filtered results;
- network, borrower, and deadline severity filters work alone and in combination;
- exact severity filters and combinations use the shared calendar-day rules;
- multiple selected deadline levels return their union, while exact `Close` and exact `Very Close` exclude `Overdue`;
- opening with explicit filters selects the requested subset, replaces previous filters, and leaves unspecified dimensions unrestricted;
- active filters are visible and can be adjusted or cleared;
- overdue loans are clearly distinguishable and require critical attention, while `Very Close` loans require urgent attention without prescribing a visual treatment;
- approaching deadlines have an urgency indication independent of color;
- failures preserve and display the last known state when available;
- partially refreshed networks are not presented as fully up to date, and loan confidence is independent of reservation confidence;
- data older than 24 hours since the last complete successful loan synchronization is marked as stale;
- the last complete successful loan refresh time, or its absence, is shown for each network;
- partial counts, unavailable data, verified zero loans, and no matching loans are distinguishable;
- successful empty account snapshots remain distinguishable from missing snapshots across failures and restarts, independently of the latest attempt result;
- the interface is usable on a phone.
