# Reservations

## Goal

Provide a single, family-wide view of current reservations across all configured library accounts and networks. Make items ready for pickup and their pickup deadlines easy to spot.

## Scope

Version 0.2 is for consultation only. It does not cancel, suspend, or resume reservations, and does not retain a reservation history. Reservations that are no longer current disappear after a successful synchronization, as loans do in version 0.1.

## Main user story

> As the person managing the family library accounts, I want to see all current reservations in one place, with those ready for pickup first, so that I do not miss a pickup deadline.

## Reservations shown

Show all current reservations, including those whose items are not yet available, those approaching availability or in transit, and suspended reservations.

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

Use the same urgency thresholds as [loans](loans.md#deadline-severity): passed, 0–2 days, 3–6 days, 7–13 days, and 14 days or more. Make urgency perceptible without relying on color alone.

The expiry indication does not override the network's last status or remove a reservation that is still current. Calculate relative days in the application's configured time zone.

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

## Filters

Provide four combinable filters:

- library network;
- pickup library;
- borrower;
- status group.

Follow the loans filter behavior: independent option lists based on the full current list and configured networks, with no filter persistence between pages. Distinguish no matching reservations from no current reservations.

Identically named pickup libraries from different networks must remain distinguishable. Include the network in ambiguous option labels.

## Synchronization and freshness

Retrieve reservations automatically on the same schedule as loans.

Update as much as possible: a failure for one account or for loans must not prevent successful reservation updates, and conversely. Failed updates preserve their last known state.

Each view displays the freshness of its own information. Reservations for a network are fully up to date only when all its configured accounts have successfully refreshed their reservations. A successful loan refresh must not make failed reservation data appear current.

Show the last complete successful refresh for each network and indicate partial or stale information. Data is stale after more than 24 hours without a complete successful refresh.

Adding, removing, or restoring an account follows the same rules as loans: removed-account data is hidden, and restoring an account requires a successful refresh before its data is considered current.

## Navigation and empty states

Provide a separate reservations page, reachable from the loans interface and usable on a phone. Refine its visual hierarchy during UI implementation. A future home page serving as a starting point for different views is deferred.

Distinguish:

- a successful synchronization with no current reservations;
- no reservations matching the filters;
- a failure with a previous known state;
- a network that has never synchronized successfully.

## Acceptance criteria

Version 0.2 is usable when:

- current reservations from all configured accounts appear in one family-wide view;
- all status groups and date sort rules work as specified;
- all four filters work alone and in combination;
- pickup information, dates, and queue position are displayed when available;
- missing reservation and availability dates use clearly marked estimates;
- supplied and estimated reservation dates sort together chronologically;
- pickup deadlines are explicit, include their last calendar day, and show urgency;
- suspended reservations remain visible with their status and available suspension dates;
- completed and canceled reservations disappear after a successful refresh;
- failed refreshes preserve the last known state and visibly affect freshness;
- successful updates remain usable when other updates fail;
- empty results and never-successful synchronization are distinguishable;
- the interface is usable on a phone.

## Network references

Available fields and connector limitations are described separately in [Nantes](../../networks/nantes.md) and [Nozay](../../networks/nozay.md).
