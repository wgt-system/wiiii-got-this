# ADR-0002: Synchronization and Relay Context Boundary

- Status: Accepted
- Date: 2026-08-09

## Current Architecture Context

This ADR established the separate ownership boundary for generic synchronization/relay
delivery. Current generic delivery capability and delivery-mode decisions are governed by
`wgt-system/architecture` and Conveyance-owned ADRs. Conveyance is the accepted owner of
generic durable opaque delivery, and Current Object is the currently implemented delivery
mode. This historical ADR does not by itself authorize every delivery mechanism discussed
below; ordered/change delivery requires a new explicit decision.

## Context

Cross-device continuity between Windows and iPhone is a product requirement.

A synchronized workflow must work even when the originating Device is powered off.

Example:

```text
Windows
→ Illumination review/coding progress
→ synchronization infrastructure

Windows PC off

iPhone
→ obtain synchronized state
→ continue learning
```

At the same time:

- synchronization is optional,
- Services/data may remain entirely local,
- remote infrastructure must not imply readable remote storage,
- Vocation, Illumination, and other bounded contexts retain ownership of their domain state,
- foreign merge/conflict semantics must not become Wiiii Got This business logic.

The same generic delivery problem may be needed by multiple bounded contexts.

## Decision

Treat **Synchronization / Relay** as a separate bounded context/service in the target architecture.

It is **not** a Wiiii Got This subdomain.

The Synchronization / Relay context owns generic asynchronous transfer semantics such as:

- receiving synchronization envelopes,
- Device/Service routing,
- durable pending delivery,
- acknowledgements,
- retries,
- generic sequencing/correlation where appropriate,
- retention/delivery status,
- optional opaque/encrypted payload storage.

It does not own foreign business semantics.

Each participating Service remains responsible for:

- whether specific state may synchronize,
- sync payload semantics,
- authority,
- domain change identity,
- conflict detection,
- merge/reconciliation,
- offline command semantics,
- sensitivity/locality policy.

## Conceptual Flow

```text
Service-owned change/state
        │
        ▼
Service Sync Adapter
        │
        ▼
opaque/generic sync envelope
        │
        ▼
Synchronization / Relay
        │
        ▼
target Device
        │
        ▼
Service Sync Adapter
        │
        ▼
Service-owned reconciliation
```

## Local-Only Operation

A Service or data class may opt out of synchronization.

In that case:

- no sync envelope is produced,
- no remote copy is required,
- WGT may still integrate local Capabilities,
- cross-device continuity for that state is explicitly unavailable.

## Deployment Consequences

A likely future deployment is:

```text
Windows
├── WGT client
├── local capability runtimes/adapters
└── local databases

iPhone
├── WGT client
├── local capability runtimes/adapters
└── local databases/replicas

Personal Server
├── Synchronization / Relay service
└── optional service-specific server components
```

The server-side Sync / Relay component may be containerized.

A web UI is not required.

## Repository Consequence

The production Synchronization / Relay context should eventually have independent ownership from the `wiiii-got-this` repository.

A fake/reference relay may exist in WGT tests but must not become the production implementation accidentally.

The eventual project name and implementation technology are deliberately not selected by this ADR.

## Security Consequences

Real remote synchronization cannot be implemented until the architecture explicitly addresses:

- Device identity,
- trust,
- authentication/authorization,
- key establishment,
- encryption,
- key storage,
- revocation,
- lost Device handling,
- server compromise assumptions.

Those concerns may remain security architecture or may later justify a separate Identity / Trust context.

## Rejected Alternatives

### Synchronization inside Wiiii Got This

Rejected as the target ownership boundary because it would pull generic cross-service relay infrastructure into the WGT integration/presentation bounded context.

### Every Service implements its own delivery infrastructure

Rejected as the generic baseline because it duplicates routing, retry, delivery, retention, and always-available server infrastructure.

Service-specific synchronization semantics remain service-owned above the generic relay layer.

## Follow-up

Do not implement the production Sync / Relay service yet.

First define one concrete synchronized Service flow, most likely Illumination, and use it to finalize the first real sync contract.
