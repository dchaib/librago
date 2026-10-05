# Reservations

## Goal

Provide a single, family-wide view of current reservations across all configured library accounts and networks. Make items ready for pickup and their pickup deadlines easy to spot.

## Scope

The reservations page is for consultation only. It does not cancel, suspend, or resume reservations, and does not retain a reservation history. Reservations that are no longer current disappear after a successful synchronization, as loans do.

## Main user story

> As the person managing the family library accounts, I want to see all current reservations in one place, with those ready for pickup first, so that I do not miss a pickup deadline.

## Reservations shown

Show all current reservations, including those whose items are not yet available, those approaching availability or in transit, and suspended reservations. All current status groups belong to the unfiltered list.

Canceled and completed reservations are excluded. A passed pickup deadline does not by itself remove a reservation: it may still be reported as current or belong to the last known state after a failed synchronization.

## Status

Use these groups in this priority order:

| Group | Meaning | French filter label |
| --- | --- | --- |
| Available | Ready for pickup | `Disponible` |
| In transit / approaching availability | An intermediate state before pickup | `Bientôt disponible` |
| Unavailable | The item is not yet ready for pickup | `Pas encore disponible` |
| Suspended | The reservation is temporarily paused | `Suspendue` |
| Unknown | The network's status cannot yet be classified | `État inconnu` |

Display the network's status wording when supplied, except for available reservations: show the pickup deadline as described below, or `Disponible` if no deadline is supplied. This avoids repeating incomplete source wording such as `À votre disposition jusqu’au`. For an unavailable reservation with no source wording, use `Pas encore disponible`. Preserve the distinction between soon available and physically in transit when the network provides it.

An unknown status remains visible with the network's wording, or `État inconnu` if none is supplied. It must not be presented as available.

The `Available` filter follows the network's last known status, including when no pickup deadline is supplied or the supplied deadline has passed. A passed deadline must remain explicit; the last known available status does not guarantee that the item is still collectible. Deadline severity does not replace or change the status group.

### Suspended reservations

Keep suspended reservations visible in their own filter group, after unavailable reservations and before unknown statuses. Display the network's wording and the suspension start and end dates when supplied.

Follow the network's current status when a suspension ends. Librago does not promise a particular queue position after suspension or determine resumption from the dates alone.

## Information shown

Each reservation identifies the item, borrower, library network, pickup library, and status when available. Also show:

- reservation date, supplied by the network or estimated;
- date made available for pickup, supplied by the network or estimated;
- pickup deadline, when supplied by the network;
- queue position, when supplied, including for available reservations.

Missing optional details must not hide an otherwise usable reservation.

Author, material type, volume, and cover image are secondary information. Show the author when supplied. Omit material type from the current list to keep the cards light and consistent with loans; refine volume and cover-image presentation later.

### Pickup deadline

The pickup deadline is the primary date for an available reservation. Use two lines, consistent with the loans list: a short relative delay, followed by the action and absolute deadline, `Retrait au plus tard le 30 septembre 2026`.

- `Aujourd’hui`;
- `Demain`;
- `Dans 2 jours`;
- `Expirée depuis 2 jours` when the last pickup day has passed.

The relative delay describes the deadline, not when the item becomes available: it can already be collected. The second line explicitly states the last pickup day, which is inclusive; expiration starts on the following calendar day.

Use the shared calendar-day calculation and urgency thresholds in [loans](loans.md#deadline-severity). For available reservations, the corresponding pickup deadline categories are `Expired` for a passed deadline, then `Very Close`, `Close`, `Watch`, and `Normal`. An available reservation without a supplied pickup deadline has unknown pickup urgency and is not included in any dated severity category. Never infer a deadline from the reservation or availability date.

Calculate relative days in the application's configured time zone and recalculate as the day changes, even without a new synchronization. Make urgency perceptible without relying on color alone.

The expiry indication does not override the network's last status or remove a reservation that is still current. Distinguish an expired pickup deadline from a `Very Close` deadline in the list. Both require urgent attention rather than the critical attention required by an overdue loan: a passed pickup deadline does not have the same consequences on the library account. Deadline severity and business attention level are distinct, and these attention levels do not prescribe a visual treatment.

## Estimated dates

When the network does not supply a reservation date, use the date Librago first sees the reservation as an estimate. When the network does not supply an availability date, use the date Librago first sees it as available. No availability date is estimated before the reservation has been seen as available.

Mark estimates with an asterisk and an accessible explanation that they come from Librago's first observation. They can be later than the actual event, particularly on first use or after an extended synchronization failure.

If a reservation is already available when first seen, both estimates are that day's date. These estimates remain unchanged across later refreshes and application restarts. Prefer a network-supplied date if one becomes available.

Estimated dates participate in chronological sorting like supplied dates. Never estimate a pickup deadline.

## Default sort

Sort by:

1. status group: available, in transit / approaching availability, unavailable, suspended, unknown;
2. pickup deadline, earliest first, with missing deadlines after known deadlines within the same group;
3. reservation date, oldest first, using the network date when supplied and the estimate otherwise; compare both chronologically, with undated reservations last within the same group;
4. a stable title and borrower order for otherwise equal entries.

The same sort applies to filtered results.

## Filters

Provide five combinable filter dimensions:

- library network;
- pickup library;
- borrower;
- status group;
- pickup deadline severity.

Follow the [loans filter behavior](loans.md#filters): independent option lists based on the full current list and configured networks, visible active filters, and controls to clear individual or all filters. Different dimensions combine with AND; multiple selected pickup deadline categories combine with OR. Clearing all filters restores the family-wide list. Initial filter state is defined in [Navigation and initial filters](#navigation-and-initial-filters).

Identically named pickup libraries from different networks must remain distinguishable. Include the network in ambiguous option labels.

### Pickup deadline severity filter

Use the categories and calculation defined in [Pickup deadline](#pickup-deadline) for both display and filtering.

Provide a single filter labelled `Échéance`, allowing one or several pickup deadline categories to be selected. With no category selected, there is no pickup deadline restriction.

Support:

- no pickup deadline restriction (the default);
- each exact dated category: `Normal`, `Watch`, `Close`, `Very Close`, or `Expired`;
- **no pickup deadline**, for available reservations without a supplied deadline;
- a combination of pickup deadline categories.

Any active pickup deadline filter matches only reservations whose last known status is `Available`. Non-available reservations do not qualify, even if a source supplies a date. With no pickup deadline restriction, reservations of every status remain eligible, subject to the other filters. Combining an active pickup deadline filter with a status filter that excludes `Available` produces no matches; do not silently change the selected status.

Multiple selected categories return their union. Selecting `Close`, `Very Close`, and `Expired` together includes those three categories; selecting `Very Close` and `Expired` together includes only those two. Available reservations without a deadline are excluded unless **no pickup deadline** is selected. Exact `Close` and exact `Very Close` exclude `Expired`. **No pickup deadline** is distinct from the `Unknown` status group. Keep pickup deadline options available even when no current reservation matches them.

Use `date limite de retrait proche` for exact `Close`, `date limite de retrait très proche` for exact `Very Close`, and `date limite de retrait dépassée` for `Expired`. Wording describes the pickup deadline and must not imply that an available reservation cannot already be collected. The active selection must make each selected category understandable. Expired deadlines must remain explicitly identifiable as passed, including within a combined result.

## Synchronization and freshness

Retrieve reservations automatically on the same schedule as loans.

Update as much as possible: a failure for one account or for loans must not prevent successful reservation updates, and conversely. Failed or incomplete account snapshots preserve their last known state rather than removing reservations.

Each view displays the freshness of its own information. Reservations for a network are fully up to date only when all its configured accounts have successfully refreshed their reservations. A successful loan refresh must not make failed reservation data appear current. Track the last attempt, last complete successful refresh, and result separately for each network and collection, as described in [loans](loans.md#synchronization-and-last-known-state).

Show the last complete successful reservation refresh for each network, or indicate that none exists. Indicate failed, partial, or stale information independently from pickup urgency. Data is stale after more than 24 hours without a complete successful reservation refresh. When no complete successful refresh exists, explicitly indicate incomplete or unavailable coverage as appropriate. A recent complete snapshot does not hide a subsequent failed attempt.

Adding, removing, or restoring an account follows the same rules as loans: removed-account data is hidden, the successful refresh applies only to the exact configured account set, and a new complete refresh is required before the changed collection is considered current.

A dedicated synchronization history/status page and manual refresh are outside the scope of this specification, as in loans.

## Navigation and initial filters

Provide a separate reservations page, reachable through application navigation and from the loans interface, and usable on a phone. Refine its visual hierarchy during UI implementation.

The page can be opened with explicit initial filters. These replace any previously used list filters; unspecified dimensions have no restriction. Opening the page without explicit filters shows the full family-wide list. There is no filter persistence between pages.

Explicit initial filters behave like filters selected on the page: they remain visible, can be adjusted or cleared, and preserve the default sort and data-confidence indications. Multiple initial pickup deadline categories appear as selected categories in the same `Échéance` filter.

## Empty states and data confidence

Distinguish:

- a successful synchronization with no current reservations;
- no reservations matching the active filters;
- a failure with a previous known state;
- partial coverage of configured accounts;
- a network with no usable reservation data.

Apply list filters to usable last-known records as well as successfully refreshed records. Retain indications of stale, failed, partial, or unavailable data for the selected network scope even when filters produce no results. A network that has never completed a successful refresh may still have usable partial data from some accounts.

Apply the shared [known data coverage rules](loans.md#known-data-coverage) independently to reservation snapshots. A successful empty reservation snapshot is known data, even after a later failed update. Distinguish missing account snapshots from a failed or partial attempt that preserves usable snapshots for every account.

Qualify totals and matching counts as known or partial when coverage is incomplete. When no usable data exists, show unavailable data with a value such as `—` rather than zero. A verified empty state requires complete, reliable data for the selected scope. When known records do not match but data is incomplete, unavailable, stale, or affected by a failed update, say that no known reservations match and retain the data-confidence indication; do not claim there are no current reservations, none available, or nothing requiring attention. Offer filter adjustment or clearing for an empty filtered result.

## Acceptance criteria

The reservations list is functionally complete when:

- current reservations from all configured accounts appear in one family-wide view;
- all status groups and date sort rules work as specified, including in filtered results;
- all five filter dimensions work alone and in combination;
- exact pickup deadline filters and combinations use the shared calendar-day rules and match only `Available` reservations;
- multiple selected pickup deadline categories return their union;
- exact `Close` and exact `Very Close` exclude `Expired`, and available reservations without a deadline are included only when that category is selected or no pickup deadline restriction is active;
- opening with explicit filters selects the requested subset, replaces previous filters, and leaves unspecified dimensions unrestricted;
- active filters are visible and can be adjusted or cleared;
- the `Available` filter includes reservations without a deadline and those with a passed deadline, without assuming expired items are still collectible;
- pickup information, dates, and queue position are displayed when available;
- missing reservation and availability dates use clearly marked estimates;
- supplied and estimated reservation dates sort together chronologically;
- pickup deadlines are explicit, include their last calendar day, and show urgency independently of color;
- pickup deadline filter wording distinguishes close, very-close, and passed deadlines without implying delayed availability;
- expired pickup deadlines remain distinguishable from `Very Close` deadlines, and both require urgent attention rather than critical attention without prescribing a visual treatment;
- suspended reservations remain visible with their status and available suspension dates;
- completed and canceled reservations disappear after a successful refresh;
- failed or incomplete refreshes preserve the last known state and visibly affect confidence;
- successful updates remain usable when other updates fail, with reservation confidence independent of loan confidence;
- the last complete successful reservation refresh, or its absence, is shown for each network, and data older than 24 hours is marked as stale;
- partial counts, unavailable data, verified zero reservations, and no matching reservations are distinguishable;
- successful empty account snapshots remain distinguishable from missing snapshots across failures and restarts, independently of the latest attempt result;
- the interface is usable on a phone.

## Network references

Available fields and connector limitations are described separately in [Nantes](../../networks/nantes.md) and [Nozay](../../networks/nozay.md).
