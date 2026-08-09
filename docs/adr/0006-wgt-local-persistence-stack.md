# ADR-0006: WGT Local Persistence Stack

- Status: Accepted
- Date: 2026-08-09

## Context

Wiiii Got This needs a small local authoritative store for WGT-owned integration state on both Windows and iPhone.

Initial durable state includes:

- local Device identity/configuration,
- known Service Integrations,
- global enablement,
- Device-specific overrides,
- validated Service/Capability publication snapshots,
- limited WGT-owned local metadata.

Foreign authoritative business state does not belong in this store.

The iPhone client uses .NET/iOS AOT/trimming constraints, so the persistence stack should be simple, explicit, and easy to validate on-device.

## Decision

Use:

- **SQLite**
- **Microsoft.Data.Sqlite.Core**
- **SQLitePCLRaw.bundle_green**

for Wiiii Got This-owned local persistence.

Use a thin repository/persistence adapter with explicit SQL.

Do **not** use EF Core for the WGT V1 persistence layer.

Use simple versioned SQL migrations owned by WGT.

## Why `bundle_green`

The selected SQLitePCLRaw bundle uses a suitable native SQLite integration policy across platforms and uses the iOS system SQLite library on iOS.

The exact compatible NuGet patch versions are selected during repository bootstrap and kept current through ordinary dependency maintenance.

## Migration Strategy

WGT owns deterministic ordered migration scripts, for example:

```text
migrations/
├── 0001_initial.sql
├── 0002_add_publication_snapshot.sql
└── ...
```

The persistence adapter records applied schema versions in a WGT-owned migration metadata table.

Rules:

- migrations run in deterministic order,
- each migration is applied atomically where SQLite permits,
- already applied migrations are not silently rewritten,
- migration failure aborts startup of the writable store rather than partially accepting a schema,
- migration behavior is covered by tests.

Do not introduce a general migration framework unless the schema later becomes complex enough to justify one.

## Repository Boundary

Persistence interfaces/ports belong toward the application boundary.

SQLite-specific code belongs in infrastructure.

Conceptually:

```text
WGT Domain/Application
        │
        ▼
Persistence Port
        │
        ▼
SQLite Adapter
        │
        ▼
Microsoft.Data.Sqlite
```

Domain objects must not depend on:

- `SqliteConnection`,
- SQL row representations,
- SQLite schema details.

## Serialization

Where structured metadata is stored as JSON:

- use `System.Text.Json`,
- prefer source-generated serialization metadata for AOT/trimming-sensitive paths,
- never serialize arbitrary foreign domain objects into WGT storage merely for convenience.

## Encryption

This ADR does not claim that plain SQLite is sufficient for every sensitive future data class.

WGT-owned integration configuration is the initial scope.

Foreign sensitive replicated state remains foreign-Service-owned and may require separately encrypted storage.

If WGT later stores security-sensitive credentials or key material, use platform secure-storage/key facilities rather than treating the ordinary SQLite database as a secret vault.

## Consequences

### Positive

- small dependency surface,
- explicit schema,
- straightforward restart/migration testing,
- avoids making WGT persistence dependent on a full ORM,
- appropriate for a small integration/configuration model,
- easier AOT/trimming reasoning.

### Cost

- SQL and mapping code are explicit,
- migration discipline must be maintained by the project.

## Rejected Alternative: EF Core

EF Core is not rejected globally and may remain appropriate in other bounded contexts such as Illumination.

It is not selected for WGT V1 because WGT's schema is small and a full ORM provides limited benefit relative to added AOT/trimming and migration complexity.

## Follow-up

The first iPhone smoke slice must prove:

- database creation,
- migration,
- read/write,
- restart persistence,
- publication snapshot persistence,

on the actual iOS target.
