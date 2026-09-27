# ADR-0003 — Synchronize each library account as one session

## Status

Accepted

## Context

Loans and reservations belong to the same configured library account, but separate synchronization services caused each connector to open a session and authenticate twice. Some networks use an expensive browser challenge. At the same time, the loans and reservations views require independent freshness, and a failure reading one collection must not erase or make stale data in the other appear current.

## Decision

The worker invokes one account synchronization service. A connector opens one private session, authenticates once, and returns independent complete results for loans and reservations. Retrieval remains split into focused collection methods that reuse the session. Connector session types remain network-specific.

The service persists each successful collection independently and records separate network synchronization states. Failed or incomplete collections preserve their last known data. A successful empty collection replaces its prior data. Network completeness continues to require success for every configured account for that collection.

## Consequences

- each account is authenticated at most once per synchronization cycle;
- a collection failure does not discard a successful sibling collection;
- loan and reservation freshness remain independent;
- connectors share an orchestration contract without forcing HTTP and browser sessions into a common abstraction;
- future account operations can reuse session setup while keeping operation methods focused.
