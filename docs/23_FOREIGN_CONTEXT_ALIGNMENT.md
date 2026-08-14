# Wiiii Got This – Foreign Context Alignment

## Status

Repository-grounded WGT consumer-alignment baseline. System-wide ownership is authoritative in `wgt-system/architecture`; this document records only WGT-relevant consequences.

Sources of truth reviewed:

- `wgt-system/illumination` branch `dev`
- `wgt-system/vocation` branch `dev`
- `wgt-system/conveyance` branch `dev`

This document records only integration facts already accepted or explicitly planned by the owning bounded contexts. It is not a second Service Catalog or system-wide ownership source.

## 1. Illumination Alignment

Illumination has explicitly accepted the revised Wiiii Got This relationship.

### Accepted Illumination position

Illumination:

- remains an independent bounded context,
- owns learning domain/application/persistence semantics,
- owns future domain-specific synchronization and merge semantics,
- remains locally executable without a mandatory server,
- uses C# / .NET 10,
- uses SQLite + EF Core for its authoritative local persistence,
- may retain Avalonia only as an optional standalone/admin/dev host.

Wiiii Got This:

- is the primary end-user presentation for Illumination on Windows and iPhone,
- may host an Illumination capability runtime locally in-process,
- must communicate through explicit Illumination-owned application/published-contract boundaries,
- must not use Illumination domain objects directly.

This aligns with the WGT V1 Integration Adapter and presentation ADRs.

## 2. Illumination Deployment Alignment

A valid future deployment is:

```text
WGT iPhone process
├── WGT bounded-context code
├── WGT Illumination Integration Adapter
└── Illumination capability runtime
    ├── Illumination Application
    ├── Illumination Domain
    └── Illumination Persistence
```

The process boundary does not erase the bounded-context boundary.

The WGT Integration Adapter sees only the explicit Illumination application/published integration surface.

## 3. Illumination iOS Runtime Gate

Illumination currently retains EF Core for its persistence implementation.

Before WGT depends on local Illumination execution on iPhone, the Illumination project must prove its **actual iOS runtime/persistence configuration** through a focused iPhone integration smoke test.

This is a provider-readiness gate, not a reason for WGT to change its own persistence stack or to duplicate Illumination state.

WGT must not add a fallback implementation of Illumination persistence/business logic merely to bypass a provider-side iOS compatibility problem.

If Illumination later changes its persistence adapter for iPhone, that remains an Illumination implementation decision behind its published boundary.

## 4. Illumination Contract Readiness

Illumination has deliberately **not** yet authored speculative WGT or synchronization APIs.

Its implementation plan places a concrete Integration Surface in a later milestone when an actual consumer and semantics require it.

Therefore WGT may:

- build a reference/fake integration now,
- design its Integration Adapter seam now,
- not invent the real Illumination contract yet.

The first real Illumination WGT contract should be designed jointly when Illumination's study/application use cases needed by WGT are stable enough.

## 5. Illumination Synchronization Alignment

The two contexts agree on the ownership split:

```text
Conveyance
owns:
- generic durable opaque delivery under accepted Conveyance modes
- Current Object as the currently accepted delivery mode
- provider-owned technical delivery/security seams as separately gated

Illumination
owns:
- which learning state synchronizes
- change semantics
- authority
- conflict detection
- merge/reconciliation
- offline learning-domain semantics
```

Illumination additionally requires an iPhone-side local copy of the learning data needed for study when the Windows PC is off.

The concrete replication contract remains deferred.

## 6. Vocation Alignment

Vocation's accepted architecture remains compatible with WGT without a technology rewrite.

Vocation remains:

- an independent bounded context,
- Python 3.13 + FastAPI/Pydantic backend/application host,
- React + TypeScript + Vite standalone local web UI,
- SQLAlchemy/Alembic/SQLite local persistence,
- locally authoritative,
- independently startable.

Its standalone UI remains useful for rich desktop-oriented workflows such as:

- research/import workflows,
- prompt workflows,
- administrative/triage operations,
- later rich market views.

WGT does not need to replace this entire surface.

## 7. Vocation → WGT Boundary

Vocation's Context Map already defines:

- Open Host Service,
- Published Read Contracts,
- Customer/Supplier,
- Vocation owns job-market semantics,
- WGT owns device/platform presentation.

WGT must not:

- import Vocation domain classes,
- read the Vocation SQLite database,
- create a WGT JobOpportunity aggregate,
- reproduce Vocation assessment/decision logic.

## 8. Vocation Runtime Direction

The current natural Windows integration is local out-of-process:

```text
WGT Windows
    ↓
Vocation Integration Adapter
    ↓
versioned local HTTP/JSON Published Contract
    ↓
Vocation FastAPI runtime
```

This is compatible with WGT's provider-specific transport strategy.

There is no requirement to port Vocation to .NET.

## 9. Vocation Published Opportunity Overview

Vocation has implemented `Published Opportunity Overview 1.0` on `dev`.

Its canonical schema is:

```text
schemas/published-opportunity-overview-v1.schema.json
```

Its local read-only publication endpoint is:

```text
/published/v1/opportunity-overview
```

The contract is client-neutral, versioned, and read-only. It intentionally excludes:

- personal state,
- Availability/Freshness,
- URLs/navigation,
- maps,
- comparison,
- opportunity detail.

It can be consumed without accessing Vocation's database, domain classes, or internal React API. It is therefore the concrete contract consumed by WGT's first Windows integration.

Vocation also owns the accepted `Published Map Projection 1.0`, whose canonical schema is `schemas/published-map-projection-v1.schema.json`. WGT may consume either projection only through the provider-owned contract; neither projection transfers Vocation semantics or persistence authority to WGT. These contracts do not include later Vocation contracts such as Opportunity Detail, Groups/Waves, or Availability/Freshness.

The endpoint remains outside Vocation's internal React/OpenAPI surface and does not implement relay, authentication, remote persistence, or cross-device writes.

## 10. Vocation Synchronization Direction

For initial iPhone usage, Vocation is likely a read-oriented integration.

The architecture should allow Vocation to later choose among:

- read-only snapshots,
- replicated mobile read state,
- live remote read service,
- another Vocation-owned publication model.

WGT does not decide that mechanism for Vocation.

The accepted Sync/Relay context may transport Vocation-owned snapshots/changes if Vocation later publishes a suitable synchronization contract.

## 11. Vocation UI Ownership

The expected split is:

```text
Vocation standalone UI
├── research/import
├── prompt workflows
├── administration
└── rich Vocation-specific desktop workflows

WGT
├── Windows capability presentation where useful
└── iPhone capability presentation
```

Avoid duplicating every Vocation standalone screen inside WGT.

A concrete Capability is added to WGT only when the cross-device/integrated use case justifies its own WGT-native view.

## 12. Conveyance Alignment

Conveyance is the accepted separate bounded context for generic durable opaque cross-device delivery.

Conveyance owns:

- generic durable delivery,
- Current Object delivery and transport/relay mechanics,
- opaque Current Object storage and delivery,
- later security/trust transport mechanisms as separately accepted.

Conveyance does not own:

- Vocation semantics,
- Illumination semantics,
- WGT presentation,
- foreign merge/reconciliation rules.

Its current V1 path is:

```text
Vocation
  ↓ Published Opportunity Overview 1.0
WGT Windows
  ↓ protect/publish
Conveyance
  ↓ retrieve
WGT iPhone
  ↓ verify/decrypt/validate
WGT-native Vocation presentation
```

Conveyance currently implements the generic Current Object delivery mode. Production
authentication/cryptography interoperability remains gated in Conveyance; WGT must not
treat this alignment document as a claim that production secure cross-device integration
is complete. Retry, ordered/change, and other delivery semantics are not implied.

## 13. First Real Integration Ordering

Vocation Published Opportunity Overview 1.0 is the implemented first real WGT Windows integration because it is an accepted, versioned, consumer-ready read-only contract. The current `dev` baseline consumes it through the local HTTP adapter and presents it WGT-natively.

Repository reality now adds an important condition:

> The first real WGT integration was selected by the provider with the first **accepted, versioned, consumer-ready Published Contract**.

Therefore:

- Vocation is the current first Windows integration through Published Opportunity Overview 1.0; its iPhone provider acceptance remains gated by the real Apple runtime smoke.
- Illumination may become first if its WGT Integration Surface becomes contract-ready earlier.
- WGT Core/reference-provider development does not wait for either project.

Do not couple WGT bootstrap progress to foreign contract scheduling.

## 14. Shared Findings

Both foreign contexts agree with WGT on these invariants:

1. no shared database,
2. no cross-context domain-class imports,
3. no shared business-logic library that bypasses contracts,
4. published contracts are explicit and versioned,
5. presentation does not transfer domain ownership,
6. physical/process co-location does not transfer domain ownership,
7. server/Docker infrastructure is optional and must not silently change data ownership,
8. synchronization mechanics and domain merge semantics are separate concerns.

## 15. WGT Consequence

No WGT architecture reversal is required.

The WGT repository can be bootstrapped now using:

- Reference Integration,
- fake/reference contracts,
- accepted WGT Domain/Application semantics.

Further real provider integration work begins only when the owning provider has accepted the relevant contract. The Vocation Windows integration is released in v0.3.0; this does not claim iPhone acceptance.

## 16. Provider Readiness Checklist

Before adding a real Service Integration Adapter:

### Provider must supply

- stable Service identity semantics,
- concrete Capability identity,
- versioned Published Contract,
- compatibility/version rules,
- error semantics,
- required data/command semantics,
- runtime/deployment expectations,
- tests/fixtures suitable for consumer contract testing.

### WGT must supply

- Integration Adapter,
- WGT-native presentation,
- current Device/Platform Capability Resolution,
- availability/error mapping,
- WGT-side contract tests,
- no foreign domain/persistence dependency.

## 17. Current Readiness

### Illumination

Architecture relationship: **accepted**

Concrete WGT interaction contract: **deferred/not yet published**

Concrete sync contract: **deferred/not yet published**

iPhone local runtime viability: **must be proven by Illumination integration smoke test**

### Vocation

Architecture relationship: **accepted**

Standalone runtime/UI: **implemented direction remains valid**

Published Opportunity Overview 1.0: **implemented on `dev`, consumed by WGT's released Windows v0.3.0 baseline**

Later Opportunity Detail, Groups/Waves, and Availability/Freshness contracts: **not part of Published Opportunity Overview 1.0**

### Conveyance

Architecture relationship: **accepted as separate Conveyance bounded context**

V1 Current Object delivery: **implemented direction/contract available on `dev`**

Production security interoperability and WGT foreign-context integration: **not yet complete**

## 18. Rule

Foreign repository state outranks earlier WGT assumptions about foreign implementation details.

When Vocation or Illumination accepts a new integration ADR or published contract, WGT must consume that accepted boundary rather than preserve stale assumptions.
