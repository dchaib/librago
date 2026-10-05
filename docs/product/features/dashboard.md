# Home dashboard

## Goal

Provide a family-wide overview of current library activity, organized by library network.

The home page should answer two questions:

- what is currently borrowed or reserved in each network;
- what requires attention soon.

The dashboard is a summary and navigation surface. Detailed item information remains on the loans and reservations pages.

## Scope

The home page:

- shows one summary for each configured library network;
- shows current loan and reservation counts for each network;
- highlights loans that should be returned soon;
- highlights reservations that are available for pickup;
- highlights available reservations whose pickup deadline is close;
- surfaces urgent situations across all networks;
- links every actionable count to the corresponding filtered list;
- makes unreliable or incomplete data visible when necessary.

The dashboard does not list individual loans or reservations.

Borrower information is not shown on the dashboard. The dashboard represents the household as a whole; borrower-level detail belongs to the full lists.

## Network summaries

Show every configured library network in a stable configured order.

Each network contains a loan summary and a reservation summary.

### Loans

For each network, show:

- the total number of current loans;
- the number of loans to return soon.

The total includes all current loans, including overdue loans.

A loan is considered **to return soon** when its deadline severity is `Close` or more urgent:

| Remaining time | Severity | Included |
| --- | --- | --- |
| Due date passed | `Overdue` | Yes |
| 0–2 days | `Very Close` | Yes |
| 3–6 days | `Close` | Yes |
| 7–13 days | `Watch` | No |
| 14 days or more | `Normal` | No |

The dashboard should use the same deadline calculation and severity rules as the loans list rather than defining an independent return window.

Example:

> 8 prêts  
> 3 à rendre prochainement

When no loan qualifies, explicitly show that none requires attention soon rather than omitting the information.

Example:

> 2 prêts  
> Aucun à rendre prochainement

### Reservations

For each network, show:

- the total number of current reservations;
- the number of reservations currently available for pickup;
- when applicable, the number of available reservations whose pickup deadline is close or has passed.

The total includes all current reservations regardless of status.

Availability is the primary actionable reservation information. Pickup urgency is secondary.

Example:

> 5 réservations  
> 2 disponibles  
> 1 avec une date limite de retrait proche ou dépassée

A reservation counts as **available** whenever its last known status is `Available`, including when:

- no pickup deadline is supplied;
- the supplied pickup deadline has already passed.

Do not infer a pickup deadline when none is provided.

An available reservation is considered **to pick up soon** when its pickup deadline severity is `Close` or more urgent:

| Remaining time | Severity | Included |
| --- | --- | --- |
| Pickup deadline passed | Expired | Yes |
| 0–2 days | `Very Close` | Yes |
| 3–6 days | `Close` | Yes |
| 7–13 days | `Watch` | No |
| 14 days or more | `Normal` | No |
| No pickup deadline | Unknown | No |

Use the same deadline calculation and severity thresholds as the reservations list.

The combined pickup-attention count includes `Close`, `Very Close`, and `Expired`. Its label must cover both upcoming and passed deadlines, for example `date limite de retrait proche ou dépassée`. Reserve `date limite de retrait proche` alone for exact `Close` severity.

When no reservation is available, explicitly show this:

> 2 réservations  
> Aucune disponible

When reservations are available but none has a close or passed pickup deadline, do not show an additional zero-valued pickup urgency line.

Example:

> 5 réservations  
> 2 disponibles

rather than:

> 5 réservations  
> 2 disponibles  
> Aucune avec une date limite de retrait proche ou dépassée

## Global urgent attention

The dashboard must show a global attention area above the network summaries whenever known data contains at least one critical or urgent situation defined below.

This section is absent when no known item qualifies. Its absence must not imply that nothing requires attention when data is unreliable or incomplete; retain the separate data-confidence warning.

It aggregates all configured networks.

### Critical attention

An overdue loan requires critical attention.

Overdue loans are shown separately from upcoming deadlines because the due date has already passed and an overdue loan may have consequences on the library account.

Example:

> 2 prêts en retard

### Urgent attention

The following situations require urgent attention:

- loans with `Very Close` severity;
- available reservations with `Very Close` pickup severity;
- available reservations whose pickup deadline has passed.

A passed reservation pickup deadline remains urgent rather than critical. The deadline has already passed, but this does not have the same consequences as an overdue loan.

A reservation reported as available after its pickup deadline remains part of the available count. The dashboard must not assume that it can still be collected, only report the last known status and the passed deadline.

If very-close and expired pickup deadlines share a global count, use wording that covers both conditions. A count containing expired deadlines must not be described only as an upcoming deadline.

Do not promote `Close` items to the global attention area. `Close` items remain visible through the per-network summaries.

This creates three levels of attention on the dashboard:

- `Close`: prepare for an upcoming library visit;
- `Very Close`, or a passed pickup deadline: act very soon;
- overdue loan: critical attention because the due date has already passed.

Critical and urgent situations should remain distinguishable even if they are presented within the same global attention area.

## Navigation

Every dashboard count that represents a subset of data should navigate to the corresponding filtered full list.

Examples:

| Dashboard element | Destination |
| --- | --- |
| Total loans for a network | Loans filtered by that network |
| Loans to return soon | Loans filtered by that network and `Close` or more urgent |
| Total reservations for a network | Reservations filtered by that network |
| Available reservations | Reservations filtered by that network and `Available` |
| Reservations to pick up soon | Reservations filtered by that network, `Available`, and `Close` or more urgent |
| Global overdue loans | Loans across all networks filtered by `Overdue` |
| Global very-close loans | Loans across all networks filtered by `Very Close` |
| Global urgent reservations | Reservations across all networks filtered by `Available` and the union of `Very Close` and `Expired` |

Opening a filtered list from the dashboard should make the represented set understandable and inspectable.

The dashboard itself remains a household-wide view and is not affected by filters previously used on the loans or reservations pages.

## Data confidence

Synchronization status should not add visual noise during normal operation.

When all displayed data is reliable, the dashboard does not show synchronization health, freshness timestamps, or confirmation messages.

When data is stale, incomplete, failed, or unavailable:

- show a prominent global data-confidence warning;
- identify each affected network and collection locally;
- preserve usable last-known data where available;
- do not present partial or unavailable data as a confirmed complete total.

Loan and reservation confidence are independent for each network. A successful loan refresh does not imply that reservation data is current, and vice versa.

Determine whether data is known from successful account snapshots, including empty snapshots, using the shared [known data coverage rules](loans.md#known-data-coverage). Snapshot coverage and the result of the latest synchronization attempt are distinct: a failed update can leave complete last-known coverage, while a successful update for some accounts can leave other accounts without any known data.

### Last known data

When a synchronization fails but a previous successful snapshot exists:

- derive counts from the preserved last-known records;
- clearly indicate that the affected collection was not successfully updated.

Recalculate deadline-based counts using today's date in the configured application time zone, even without a successful new synchronization. Preserving records does not freeze their deadline severity or attention counts.

### Stale data

When data exceeds the application's freshness threshold:

- keep displaying the known counts;
- indicate that the affected collection is stale.

### Partial data

When only part of a collection is known:

- qualify the displayed counts as partial or known counts;
- do not present them as verified totals.

For example:

> 5 réservations connues  
> 2 disponibles

### Unavailable data

When no configured account has a usable successful snapshot for a collection:

- show an unavailable value such as `—`;
- do not substitute zero.

A successful empty snapshot is usable data. If only some accounts have usable snapshots and they contain no records, show a qualified known count such as `Aucun prêt connu`, with the incomplete-coverage warning, rather than an unavailable value or a verified empty state.

A synchronization or data-confidence problem is distinct from loan or reservation urgency. These are separate concepts even if both require visual attention.

## Empty states

Every configured network remains visible even when it has no current activity.

Examples:

> Aucun prêt

> Aucune réservation

A verified empty state requires complete, reliable data for the corresponding collection. When coverage is complete but data is stale or affected by a failed update, qualify a zero count as last-known data rather than a confirmed absence of current activity.

If data is partial or unavailable, do not claim that there are no loans, no reservations, or nothing requiring attention.

If no library network is configured, show an empty application state indicating that no library account or network is available. Configuration itself is outside the scope of the dashboard.

## Terminology

User-facing wording should consistently distinguish:

- loans **to return**;
- reservations **available for pickup**;
- the **pickup deadline** of an available reservation.

Suggested French wording:

- `à rendre prochainement` for loans with `Close` or more urgent deadlines;
- `à rendre très prochainement` for loans with `Very Close` deadlines;
- `disponible` / `disponibles` for reservations whose last known network status is `Available`, without guaranteeing that an expired or outdated reservation is still collectible;
- `date limite de retrait proche` for an available reservation with a `Close` pickup deadline;
- `date limite de retrait très proche` for an available reservation with a `Very Close` pickup deadline;
- `date limite de retrait dépassée` when the pickup deadline of an available reservation has passed.

For the combined per-network pickup-attention count, use `date limite de retrait proche ou dépassée` or equivalent wording covering `Close`, `Very Close`, and `Expired`.

Pickup deadline wording must not imply that an available reservation cannot already be collected.

Exact microcopy may vary, but it must preserve the distinction between:

- availability;
- upcoming deadline;
- very close deadline;
- overdue loan;
- passed pickup deadline.

## Acceptance criteria

The home dashboard is functionally complete when:

- every configured network is shown in a stable order;
- each network shows its total current loan count;
- each network shows how many loans have `Close`, `Very Close`, or `Overdue` severity;
- overdue loans remain part of both the total loan count and the loans requiring attention;
- each network shows its total current reservation count;
- each network shows how many reservations are currently available;
- available reservations remain counted even when their pickup deadline is missing or has passed;
- an available reservation with a `Close`, `Very Close`, or passed pickup deadline is reflected in the pickup-attention count;
- a reservation without a pickup deadline is not considered close;
- zero-valued primary states such as `Aucun à rendre prochainement` and `Aucune disponible` are explicit;
- zero-valued pickup urgency is omitted;
- overdue loans appear in the global urgent area as critical attention;
- very-close loans appear in the global urgent area as urgent attention;
- very-close or expired available reservations appear in the global urgent area as urgent attention;
- the global attention area is present whenever known data contains critical or urgent items, and combined pickup labels cover both very-close and expired deadlines;
- `Close` items do not appear in the global urgent area;
- every dashboard count links to the corresponding filtered full list;
- borrower information is not required on the dashboard;
- filters previously used on full lists do not alter dashboard totals;
- normal reliable data does not produce synchronization noise;
- stale, failed, partial, or unavailable data is visible globally and on the affected network collection;
- partial data is not presented as a verified total;
- unavailable data is not represented as zero;
- successful empty account snapshots remain distinguishable from missing snapshots, including after a later failed attempt;
- deadline-based counts are recalculated from preserved records as the calendar day changes;
- the dashboard never needs to display individual loan or reservation records to fulfil its purpose.
