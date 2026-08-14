# Wiiii Got This – Architecture

## Status

Accepted V1 architecture baseline, with explicitly deferred security-cryptography and production synchronization contract details.

## Visual runtime model

The service-local [C4 runtime model](model/README.md) visualizes the accepted WGT
runtime topology. This document and the accepted ADRs remain authoritative for WGT
architecture decisions.

Accepted foundations include:

- Windows desktop + iPhone,
- C# / .NET 10,
- Avalonia 12,
- SQLite for WGT-owned local state,
- separate Synchronization / Relay target context,
- WGT-shipped Integration Adapters,
- WGT-native executable presentation,
- personal Device trust/pairing,
- hybrid recovery.

## 1. Architectural Goals

Wiiii Got This should support:

- a coherent cross-platform user experience,
- independently evolving bounded contexts,
- service/capability integration through explicit contracts,
- global integration configuration with optional Device overrides,
- contextual capability availability,
- local and remote provider runtimes,
- optional personal-server/container infrastructure,
- explicit data-locality and sensitivity constraints,
- graceful degradation when individual services fail,
- later addition of presentation surfaces without importing foreign domain logic.

## 2. Logical Architecture

The baseline logical structure is:

```text
Presentation Adapters / Clients
            │
            ▼
Wiiii Got This Application Layer
            │
            ▼
Wiiii Got This Domain
            │
            ├── Service Integration
            ├── Capability Resolution
            ├── Availability / Compatibility
            ├── Device / Platform Context
            └── Presentation Resolution
            │
            ▼
Ports / Adapters
   ├── Persistence
   ├── Device/Platform Observation
   ├── Discovery / Registration
   ├── Foreign Service Contracts
   ├── Reachability / Health
   └── optional future Sync / Trust infrastructure
```

This is a responsibility model, not a deployment diagram.

## 3. Presentation Architecture

Wiiii Got This is not one specific client executable.

Potential presentation adapters include:

- native mobile,
- native desktop,
- web.

The first implementation need not include all of them.

### Rules

- presentation clients must not import Vocation or Illumination domain classes,
- client-specific UI state must not become foreign domain truth,
- not every Capability must be supported by every presentation environment,
- presentation selection follows Capability Resolution.

### Integrated presentation

The preferred product direction is embedded/coherent presentation inside Wiiii Got This where supported.

External application delegation remains a possible explicit fallback.

## 4. Application and Domain Placement

The Wiiii Got This domain should remain independent of:

- UI framework,
- transport,
- persistence framework,
- Docker/container runtime,
- foreign service implementation technology.

The application layer coordinates:

- configuration commands,
- discovery/refresh,
- availability refresh,
- capability resolution,
- published service invocation.

Adapters translate between:

- foreign published contracts,
- runtime/platform APIs,
- persistence,
- WGT application/domain concepts.

## 5. Foreign Service Boundary

Each integrated Service is accessed only through an explicit adapter.

Conceptually:

```text
WGT Application
      │
      ▼
Service Port
      │
      ▼
Service-specific Adapter
      │
      ▼
Published Contract
      │
      ▼
Foreign Service
```

The adapter may use HTTP, IPC, local invocation, files, or another transport after architecture selection.

Transport is not the domain boundary.

## 6. Local and Remote Providers

The architecture must support both:

### Local provider

A provider runtime available on the current Device/local environment.

### Remote provider

A provider runtime reached through network infrastructure.

The same logical Capability contract should avoid leaking deployment topology where practical.

### Constraint

Remote provider access does not imply that all authoritative service data is remotely stored.

## 7. Personal Server / Container Infrastructure

A personal server is a valid deployment target.

Docker/containers are explicitly allowed where useful for:

- remotely reachable provider components,
- relay/connectivity,
- registry/discovery infrastructure,
- optional synchronization infrastructure,
- shared technical infrastructure.

Containerization is not required for local client/domain code merely for architectural symmetry.

One bounded context may have:

- local client components,
- server components,
- zero or more containers,

if concrete requirements justify that topology.

## 7A. V1 Client Host Topology

Each V1 WGT client is one primary application host. The current V1 implementation uses statically shipped Integration Adapters.

A foreign capability runtime may be hosted in-process, locally out-of-process, or remotely without changing its bounded-context ownership.

WGT itself has no mandatory V1 server.

See `docs/adr/0007-v1-client-host-composition-topology.md`.

The accepted target is contract-driven extension for ordinary compatible remote/read Services: adding such a Service should not inherently require a new WGT/iOS build when existing WGT invocation and presentation capabilities suffice. This target does not make arbitrary runtime registration or downloaded executable plugins part of the current V1 implementation. See `docs/adr/0009-wgt-owned-presentation-and-contract-driven-service-integration.md`.

## 8. Persistence

Wiiii Got This needs persistence for its own authoritative state, at minimum likely:

- known Service Integrations,
- global enablement,
- Device overrides,
- Device identity/configuration,
- accepted registration/publication metadata where durable,
- user-owned presentation/integration preferences where later required.

Foreign domain data is not automatically WGT persistence.

### Persistence rules

- no foreign tables,
- no shared ORM/domain models,
- no WGT ownership inferred from cache/storage,
- SQLite via `Microsoft.Data.Sqlite.Core` + `SQLitePCLRaw.bundle_green`,
- explicit SQL persistence adapters,
- deterministic WGT-owned migrations,
- migrations required for durable WGT state.

See `docs/adr/0006-wgt-local-persistence-stack.md`.

## 9. Publication Snapshot / Cache

Wiiii Got This may retain last-known Service/Capability publication metadata to allow:

- configuration while provider is temporarily offline,
- diagnostics,
- stable known-Service identity,
- change/deprecation reconciliation.

This is integration metadata, not foreign business state.

It must carry enough freshness/version information to avoid being mistaken for current runtime availability.

## 10. Availability Observation Architecture

Technical observation adapters may gather:

- provider reachability,
- health,
- publication refresh result,
- current platform/runtime facts,
- required local component presence.

The domain/application layer derives product-facing Availability.

### Rule

Do not put final availability policy inside low-level health-check adapters.

## 11. Discovery / Registration Architecture

V1 registration/discovery is explicit and Integration-Adapter-specific.

There is no mandatory universal publication wire protocol.

Possible future mechanisms may include:

- static/local configuration,
- explicit registration,
- local process/IPC discovery,
- LAN discovery,
- personal-server registry,
- remote registry.

The architecture should allow multiple discovery adapters if concrete use cases justify them.

### Extraction gate

If a Service Registry becomes independently meaningful for several consumers or develops its own trust/lifecycle/admin semantics, it should be reconsidered as a separate bounded context/service.

## 12. Capability Contract Architecture

Contracts are service-specific where business semantics differ.

Do not create one generic data API that tries to represent:

- Vocation job data,
- Illumination learning interactions,
- future unrelated domains.

WGT may standardize the integration envelope:

- identity,
- publication,
- capability metadata,
- compatibility,
- presentation contribution metadata.

The Capability payload itself may use service-owned Published Languages.

## 13. Presentation Contribution Architecture

V1 uses WGT-native executable presentation delivered with the Wiiii Got This application.

Services expose:

- versioned Read Contracts,
- versioned Command Contracts,
- Capability metadata,
- bounded presentation metadata where necessary.

WGT Integration Adapters map these contracts into WGT-native Avalonia presentation for Windows and iPhone.

The same Capability may have different Windows and iPhone layouts/interactions.

V1 does not support arbitrary runtime-downloaded native UI plugins.

Future constrained declarative Presentation Contributions remain possible if repeated integrations establish stable common primitives.

Explicit external delegation remains available as a capability-specific fallback where accepted.

## 14. Multi-Device / Synchronization Architecture

Cross-device continuity between Windows and iPhone is a product requirement.

Synchronization must work asynchronously: either client may be offline while the other synchronizes through always-available infrastructure.

The target architecture uses a separate **Synchronization / Relay bounded context/service** for generic delivery.

Participating Services retain ownership of:

- synchronization eligibility,
- payload semantics,
- domain change identity,
- merge/conflict rules,
- reconciliation,
- data-locality policy.

Services/data may remain entirely local when synchronization is disabled or prohibited.

Production sync cryptography and the first concrete sync contract remain deferred until a real Service flow—most likely Illumination—defines the required semantics.

## 15. Security and Trust

V1 uses explicit personal Device trust/pairing without a mandatory conventional user account.

The architecture distinguishes:

- WGT Device identity,
- installation identity,
- cryptographic Device credentials,
- trust enrollment,
- trust revocation,
- future account identity.

Recovery is hybrid:

- another trusted Device approves normal enrollment/recovery,
- separately stored recovery material supports emergency recovery when no trusted Device remains.

The personal server alone is not sufficient recovery authority.

Local-only operation remains possible without remote identity infrastructure.

A future public/multi-user product may add account/authentication above the trusted-Device model.

Exact enrollment protocol, recovery-material format, key hierarchy, and end-to-end encryption design remain deferred until the first production synchronized Service flow.

## 16. Fault Isolation

One Service failure must not become whole-product failure.

Required architectural behavior:

- timeouts/cancellation around remote providers,
- explicit unavailable results,
- independent capability resolution,
- error translation at adapter boundaries,
- no distributed transaction spanning unrelated contexts,
- no startup requirement that every integrated Service is healthy.

Exact resilience technology is selected later.

## 17. Versioning and Compatibility

Cross-context contracts are versioned.

WGT should perform deterministic compatibility checks before invocation.

Architecture must support independent provider/client release cadence.

Potential implementation techniques remain open.

## 18. Observability

WGT should eventually provide structured diagnostics for:

- Service Identity,
- Capability Identity,
- registration/discovery refresh,
- compatibility decisions,
- availability decisions,
- provider invocation failures,
- correlation identifiers.

Observability must not store sensitive foreign business payloads by default.

## 19. Deployment Boundary Rules

A future deployment diagram must follow actual requirements.

Do not assume:

```text
Bounded Context = one process
Bounded Context = one container
Subdomain = one microservice
Client = separate domain
```

Deployment separation is justified by factors such as:

- independent runtime availability,
- platform limitations,
- security,
- scaling,
- update cadence,
- fault isolation,
- connectivity.

## 20. Testing Architecture

Priority test layers:

1. pure domain policy tests,
2. application use-case tests with fake ports,
3. persistence tests,
4. service publication/contract tests,
5. provider adapter tests,
6. presentation interaction tests,
7. cross-process/network integration tests only where architecture requires them.

A fake/reference provider should be used to exercise the generic WGT integration model without coupling early WGT development to unfinished Vocation/Illumination contracts.

## 21. First Real Integration Direction

The released v0.3.0 baseline implements a read-only Vocation integration because it validates:

- Service publication,
- versioning,
- registration/discovery,
- Availability,
- read capability,
- integrated presentation,

without immediately requiring offline write synchronization.

Illumination is likely the stronger later test of interactive/offline capability architecture.

This is an implementation-order recommendation, not domain ownership.

## 21A. Foreign Provider Readiness

Current provider repositories establish:

### Illumination

- WGT is accepted as primary Windows/iPhone presentation.
- local in-process capability hosting is allowed behind explicit Illumination-owned boundaries.
- C#/.NET + SQLite/EF Core remain Illumination implementation choices.
- concrete WGT interaction and sync contracts are not yet published.

Before WGT relies on local Illumination iPhone execution, Illumination must prove its real iOS runtime/persistence configuration with a provider-side smoke test.

### Vocation

- standalone Python/FastAPI + React/TypeScript application remains valid.
- WGT consumes versioned read contracts rather than Vocation internals.
- `Published Opportunity Overview 1.0` is implemented on Vocation `dev` with canonical schema `schemas/published-opportunity-overview-v1.schema.json` and local endpoint `/published/v1/opportunity-overview`.
- later Vocation contracts remain provider-specific and are not implied by that overview contract.

WGT Core does not wait for either provider.

The first real Integration Adapter is the Vocation adapter for Published Opportunity Overview 1.0. Further provider adapters require their own accepted consumer-ready Published Contracts.

See `docs/23_FOREIGN_CONTEXT_ALIGNMENT.md`.

## 22. Initial Architecture Gate (Historical — Fulfilled/Superseded)

Before the initial implementation, explicit decisions were required for at least:

- initial target presentation platforms,
- programming language/runtime,
- UI framework/client strategy,
- WGT process/deployment topology,
- WGT persistence technology,
- initial Service publication/transport mechanism,
- first discovery/registration mechanism,
- initial presentation contribution strategy,
- whether a server component is required in the first milestone.

This initial gate is retained as historical context. It has been fulfilled and superseded by
the released implementation baseline (including the v0.3.0 read-only Vocation integration
and subsequent releases). Current open decisions are tracked in the deferred-decision and
provider-readiness documents; they do not make the initial implementation gate pending again.

Synchronization, identity/trust, and web-client architecture may remain deferred when the
current milestone does not require them.

## 23. Technology Decision Rule

Technology must be selected from product and architecture requirements.

Programming language and runtime must be explicitly reviewed before acceptance.

No agent may finalize them solely from:

- familiarity,
- résumé value,
- framework fashion,
- desire to reuse Vocation or Illumination's stack.

Reuse is valuable only where it improves the product without weakening boundaries or platform fit.
