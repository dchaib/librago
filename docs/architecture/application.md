# Application architecture

Librago's application functionality, synchronization, connectors, queries, and persistence coexist within one .NET application project and process. These responsibilities communicate through method calls and shared data contracts, following [ADR-0002](../adr/0002-application-architecture.md) and [ADR-0006](../adr/0006-application-api-boundary.md).

## Application responsibilities

| Responsibility | Role |
| --- | --- |
| HTTP endpoints | Expose application functionality under the [authentication and authorization contract](authentication.md). |
| Application services | Coordinate operations and construct read models from persisted information. |
| BackgroundService | Trigger scheduled synchronization by calling application functionality directly. |
| Provider connectors | Isolate network-specific authentication, retrieval, parsing, and source validation. |
| Domain types | Represent provider-independent loans, reservations, and synchronization results. |
| Persistence | Maintain item records and synchronization/coverage metadata. |

These are conceptual responsibilities within the monolith, rather than separate assemblies or independently deployable services.

## Data flows

### Synchronization

```text
BackgroundService -> Application synchronization -> Provider connectors
    -> Snapshots and validation -> Transactional persistence -> SQLite
```

Application synchronization selects the connector for each configured account. The connector owns one private authenticated session for retrieving independent loan and reservation results, as defined in [ADR-0003](../adr/0003-account-synchronization.md). It translates source data into domain snapshots; source completeness and record validation precede persistence. HTTP and Playwright session handling remain network-specific. Provider access, mappings, and validation details belong in the [network references](../README.md#networksnetworkmd).

The worker calls application functionality directly, without internal HTTP requests. A future manual trigger would invoke the same functionality; shared bounded execution is a prerequisite under ADR-0006.

### Reads

```text
HTTP API -> Application queries / read projections -> SQLite
```

Application queries combine persisted records with coverage and freshness metadata to construct read models. Reads preserve consistency between item data and its coverage metadata. Ordinary HTTP reads do not trigger provider retrieval or scraping. Synchronization and queries operate within the same application, with external data retrieval confined to the synchronization path.

## Persistence guarantees

SQLite is owned by one application instance; active-active replication is outside this deployment model.

Loans and reservations have independent retrieval outcomes. Successful replacement is transactional for one account collection, rather than for an entire account or network cycle. A successful empty snapshot replaces the prior collection; failed or incomplete retrieval does not enter that replacement path, and failed persistence rolls back the collection's transaction. Last-known valid data remains available.

Item records and synchronization/coverage metadata represent different concerns. Stored coverage distinguishes verified empty snapshots from missing, incomplete, or unknown data independently of the latest attempt result. Successful collection replacement and its coverage are persisted atomically; failures preserve both. Read models use this persisted information according to the [data-coverage specification](../product/features/loans.md#known-data-coverage).
