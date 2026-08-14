# Wiiii Got This – Context Map

## 1. Purpose

This document defines the relationship between Wiiii Got This and surrounding bounded contexts.

It does not prescribe transport protocol, deployment topology, repository layout, programming language, or container model.

## 2. Wiiii Got This Context

Wiiii Got This owns cross-application integration semantics including, at the current working boundary:

- Service Integration configuration,
- Device-aware capability resolution,
- Platform-aware capability resolution,
- Service/Capability availability interpretation,
- integration presentation selection,
- explicit degradation behavior.

WGT does not own generic durable cross-device delivery; that capability belongs to the accepted Conveyance bounded context. Concrete domain-specific synchronization, identity/trust, registry, or shared-map semantics remain separate decisions where their ownership is not yet accepted.

## 3. Vocation Context

Vocation owns the personal job-market domain.

Relevant Vocation-owned concepts include:

- opportunities,
- companies,
- postings and sources,
- research observations,
- assessments,
- decisions,
- Vocation-specific projections and workflows.

Wiiii Got This must not duplicate these as Wiiii Got This domain entities.

Private Vocation application material, including CVs, cover letters, and personal application documents, remains Vocation-owned semantics. If WGT later presents or transports it, the integration must preserve the private boundary defined by ADR-0010; WGT must not publish or expose the material through public contracts or artifacts.

## 4. Vocation → Wiiii Got This

### Pattern

- Open Host Service
- Published Read/Capability Contracts
- Customer/Supplier
- Anticorruption/adapter boundary where Wiiii Got This requires its own presentation model

### Ownership

Vocation decides:

- job-market meaning,
- authoritative data,
- business rules,
- semantics of published operations.

Wiiii Got This decides:

- whether the integration is enabled,
- whether a published Capability is usable on the current Device/Platform,
- how the Capability is integrated into the Wiiii Got This presentation.

### Forbidden

- direct Vocation database access,
- importing Vocation domain classes,
- reimplementing Vocation job logic inside Wiiii Got This.

## 5. Illumination Context

Illumination owns the personal learning domain.

Relevant Illumination-owned concepts include:

- Learning Items,
- Reference Solutions,
- Reviews,
- Learning State,
- scheduling,
- Decks,
- learning progress.

## 6. Illumination → Wiiii Got This

### Pattern

Expected future relationship:

- Open Host Service
- Published Capability / Read / Command Contracts
- Customer/Supplier
- explicit adapter boundary

### Ownership

Illumination decides:

- learning semantics,
- review semantics,
- scheduling,
- authoritative learning state,
- command behavior.

Wiiii Got This decides:

- current Device/Platform suitability,
- availability interpretation,
- presentation/invocation path,
- integration navigation.

### Important pressure

Mobile/offline use may require service-owned state to be available without the original desktop runtime.

That requirement does not authorize Wiiii Got This to invent Illumination synchronization or conflict semantics.

## 7. Vocation ↔ Illumination

Current relationship remains outside the Wiiii Got This domain.

Their integration, if any, uses their own explicit published contracts.

Wiiii Got This must not become a hidden mediator merely because it integrates both services.

## 8. External Service Providers

Future independent applications may integrate with Wiiii Got This through the same architectural principles:

```text
Independent Service
    │
    │ Published Service/Capability Contracts
    ▼
Wiiii Got This
```

Each provider retains ownership of its own domain.

Wiiii Got This must not require all providers to share one internal implementation stack.

## 9. Candidate Service Registry Context

### Status

Unresolved / possible extraction.

A Service Registry may eventually own:

- registered Service identities,
- provider locations,
- publication/refresh metadata,
- possibly trust-related registration lifecycle.

### Current rule

Do not create this context until concrete scenarios show independent lifecycle/ownership beyond Wiiii Got This's supporting registration/discovery needs.

If extracted, Wiiii Got This becomes a consumer of its registry contracts rather than its database.

## 10. Synchronization / Relay Context

### Status

Accepted as the separate Conveyance bounded context for generic durable opaque cross-device delivery. See `docs/adr/0002-synchronization-relay-context-boundary.md` and the system Architecture Control Plane.

Possible responsibilities:

- Current Object delivery,
- generic opaque envelope transport,
- durable relay/delivery,
- generic sequencing/revision and delivery status where accepted.

### Ownership boundary

Generic delivery mechanics are owned by Conveyance.

However:

- Vocation owns merge/conflict semantics for Vocation data,
- Illumination owns merge/conflict semantics for Illumination data,
- Wiiii Got This must not invent foreign conflict resolution.

Future delivery modes may transport domain-defined changes only after the relevant domain contract and system decision exist; Conveyance must not own foreign merge rules.

## 11. Identity / Trust

### Status

V1 Device trust/pairing and hybrid recovery semantics are accepted as part of the product architecture.

Whether future public/multi-user account/auth semantics justify a separate bounded context remains open.

Multi-device and remote-provider scenarios may require:

- device trust,
- service authentication,
- user authorization,
- credential handling.

This may remain security infrastructure or become a separate context if product semantics justify it.

It is not currently part of the Wiiii Got This core domain.

## 12. Orientation Integration

### Status

Orientation is the accepted generic geospatial bounded context. WGT integrates it for product
composition and device/platform presentation.

Conceptual relationship:

```text
Vocation ───────┐
Other Service ──┼─> Published Map Contributions
                ▼
          Orientation
                │
                │ composed map capability
                ▼
        Wiiii Got This
```

### Rules

- source services own the meaning of their spatial data,
- Orientation owns generic composition/rendering semantics,
- Orientation never reads foreign databases,
- Wiiii Got This owns device/platform-specific integration of the resulting capability.

## 13. Physical Infrastructure

Bounded-context separation does not require separate physical machines.

Future deployment may share:

- a personal server,
- Docker host,
- reverse proxy,
- database engine,
- backup infrastructure,
- network infrastructure.

Shared infrastructure must not imply:

- shared schemas,
- shared persistence models,
- cross-context table access,
- shared domain entities,
- bypassing published contracts.

## 14. Presentation Context Relationship

Wiiii Got This may have multiple presentation adapters/clients.

These are not separate bounded contexts merely because they run on different platforms.

Conceptually:

```text
Mobile Client ──┐
Desktop Client ─┼─> Wiiii Got This Application/Domain
Web Client ─────┘
```

The exact runtime topology is an architecture decision.

## 15. Context Map Summary

```text
                 ┌────────────────────┐
                 │      Vocation      │
                 │   job-market BC    │
                 └─────────┬──────────┘
                           │ Published Read/Capability Contracts
                           ▼
┌────────────────┐   ┌──────────────────────┐   ┌────────────────────┐
│  Illumination  │──>│   Wiiii Got This     │<──│ Future Services    │
│  learning BC   │   │ integration/present. │   │ independent BCs    │
└────────────────┘   └──────────┬───────────┘   └────────────────────┘
                                │
                    optional future consumers/providers
              ┌─────────────────┼─────────────────┐
              ▼                 ▼                 ▼
         Orientation       Conveyance           Registry/Trust?
```

## 16. Context Map Rules

1. Wiiii Got This does not own foreign business models.
2. No context reads another context's database.
3. Published contracts are versioned.
4. Contexts may share infrastructure without sharing domain ownership.
5. Wiiii Got This presentation integration does not create domain ownership.
6. Candidate contexts remain candidates until their independent lifecycle is proven.
7. Vocation and Illumination do not communicate through Wiiii Got This unless a future explicit orchestration scenario requires it.
