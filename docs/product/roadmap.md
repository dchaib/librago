# Product roadmap

This roadmap is intentionally lightweight.

Only features close to implementation should be specified in detail.

## 0.1 — Consolidated loans

- automatically retrieve current loans from configured accounts;
- preserve and display last known state when synchronization fails;
- show all loans in one family-wide list;
- sort by due date;
- filter by borrower and library network;
- show deadline urgency;
- indicate stale or partially refreshed data;
- show last complete successful refresh time;
- provide a responsive interface.

Detailed behavior is described in [`features/loans.md`](features/loans.md).

## 0.2 — Reservations

Consolidated reservation list, status, availability, pickup deadline, borrower, library network, and possibly cancellation.

## 0.3 — Notifications

Approaching due dates, available reservations, and pickup-deadline reminders.

## 0.4 — Library opening hours

Regular schedules plus temporary or exceptional changes.

## 0.5 — Renewals

Manual renewal with clear success or failure feedback.

## 0.6 — Prepare a library visit

Items to return, reservations to collect, and relevant opening hours.

## 0.7 — Federated search

Search across multiple networks and present results according to availability and user convenience preferences.

## 0.8 — Reservations from Librago

Reserve through the appropriate account and preferred pickup location.

## 0.9 — Series and next-volume assistance

Identify likely next volumes and suggest reserving them.

## Later / cross-cutting concerns

Intentionally deferred but tracked:

- application-level authentication and OIDC;
- manual synchronization controls;
- synchronization history;
- configuration management;
- multi-user access;
- multi-language support;
- privacy and secret management for deployment;
- contribution and connector extension model.
