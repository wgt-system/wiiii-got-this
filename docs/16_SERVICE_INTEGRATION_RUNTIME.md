# Wiiii Got This – Service Integration Runtime and Publication Model

## Status

Accepted V1 baseline. See `docs/adr/0003-v1-integration-adapter-presentation-model.md`.

## 1. Goal

Wiiii Got This must integrate independent bounded contexts implemented in different technologies without forcing them into one runtime or transport.

The integration model must support:

- in-process capability runtimes,
- local out-of-process providers,
- remote providers,
- future replicated/local capability runtimes,
- independently versioned published contracts.

## 2. Core Rule

`Bounded Context`, `Service`, `Process`, `Container`, and `Integration Adapter` are separate concepts.

A Service may be hosted:

- inside the WGT OS process,
- in another local process,
- on another Device,
- on a personal server,
- through future infrastructure.

The WGT domain must not change merely because deployment changes.

## 3. Integration Adapter

`Integration Adapter` is the technical WGT-side adapter for one foreign Service/Capability family.

It is **not** the foreign Service and is not automatically the domain concept `Plugin`.

Responsibilities may include:

- locating/connecting to the provider runtime,
- translating provider publication data into WGT integration descriptors,
- invoking provider Published Contracts,
- mapping boundary DTOs into WGT read/presentation state,
- exposing current technical observations to Capability Resolution,
- selecting/hosting a supported presentation adapter.

Integration Adapters must not contain replacement foreign business rules.

## 4. Runtime Modes

### 4.1 In-Process Runtime

Example direction:

```text
WGT iPhone process
├── WGT
├── Illumination Integration Adapter
└── Illumination Capability Runtime
```

Allowed when:

- the foreign bounded context provides a suitable portable runtime,
- runtime technology is compatible,
- published ports/contracts preserve the boundary,
- the WGT composition root does not expose foreign domain internals to WGT domain/application code.

Shared process does not mean shared bounded context.

### 4.2 Local Out-of-Process Runtime

Example direction:

```text
WGT Windows
    ↓ local published contract
Vocation FastAPI process
```

The provider may use:

- local HTTP,
- IPC,
- another explicit transport.

WGT sees only the adapter/contract boundary.

### 4.3 Remote Runtime

```text
WGT Client
    ↓ network published contract
Service Provider / Personal Server
```

Remote availability, trust, and versioning participate in Capability Resolution.

### 4.4 Replicated Local Runtime

A future Service may run locally on a Device against synchronized service-owned state.

The Service owns:

- local business semantics,
- synchronization payload semantics,
- reconciliation.

WGT owns only integration/presentation.

## 5. No Universal Wire Protocol Requirement

WGT does **not** require every Service to use one transport.

Examples may legitimately differ:

- Illumination: versioned .NET Published Contract in-process,
- Vocation: versioned HTTP/JSON Published Contract,
- future Go/Rust/Java Service: its own explicit interoperable contract.

This is intentional polyglot architecture.

## 6. Normalized WGT Integration Description

WGT still needs an internal normalized view sufficient for Capability Resolution.

Conceptually:

```text
Service Descriptor
├── Service Identity
├── display metadata
├── Publication observation/version
└── Capability Descriptors
    ├── Capability Identity
    ├── contract compatibility information
    ├── runtime requirements
    └── presentation/invocation options
```

This normalized model is WGT integration data.

It does not require every provider to expose an identical wire DTO.

Each Integration Adapter translates from the provider's Published Language.

## 7. Publication

A Service intentionally publishes the integration facts required by its adapter.

Publication may be obtained through:

- a local .NET contract/provider,
- an HTTP endpoint,
- a configured manifest,
- another versioned provider mechanism.

The publication mechanism is provider-specific in V1.

A universal Service Publication wire protocol may be introduced later if a generic registry/marketplace/discovery use case justifies it.

## 8. V1 Registration Strategy

V1 should favor **explicit registration through installed/known Integration Adapters** over speculative automatic network discovery.

Examples:

- built-in Illumination adapter knows how to compose/discover a local Illumination runtime,
- Vocation adapter knows how to reach/configure the local Vocation provider on Windows,
- future remote provider adapters may be configured with an explicit trusted location.

This satisfies the product need to know Services without prematurely introducing:

- LAN broadcast discovery,
- mDNS,
- a public plugin registry,
- marketplace infrastructure.

## 9. Discovery Expansion

Later discovery mechanisms can be added as adapters:

```text
Explicit Configuration
Local Runtime Discovery
LAN Discovery
Personal Server Registry
Remote Registry
```

Discovery never implies:

- enablement,
- trust,
- compatibility,
- availability.

## 10. Plugin-Like Product Semantics

For V1:

> `plugin-like` means a Service Integration can be known, enabled, disabled, and independently unavailable without merging the foreign domain into WGT.

It does **not** mean that the iPhone client downloads arbitrary new executable integration assemblies after installation.

The V1 direction is:

- executable WGT Integration Adapters are delivered with the WGT application version,
- Service/Capability publication data can change independently at runtime,
- compatible Services can be enabled/disabled through configuration,
- remote/local provider state can change independently,
- adding support for a completely new executable integration family may require a WGT application update.

This is particularly important for the iPhone distribution/runtime model, where dynamically downloading executable feature code is not an appropriate baseline.

A future extension mechanism may still support:

- declarative presentation/data contributions,
- HTML/JavaScript mini-app style surfaces where platform policy and product semantics justify them,
- server-driven metadata,
- new signed WGT releases containing additional native adapters.

Those possibilities are not V1 requirements.

## 11. Adding a New Service in V1

The current V1 implementation may require a WGT release containing a new Integration Adapter for a completely new Service family.

The accepted longer-term target is that an ordinary compatible remote/read Service can be added through an existing contract-driven WGT integration, invocation, and presentation capability without requiring a new WGT/iOS build merely because the Service was added. This is a target property, not a claim that v0.2.0 supports arbitrary runtime registration.

That is acceptable for the first product because:

- all initial Services are controlled projects,
- contracts are still evolving,
- arbitrary third-party plugins are not a product requirement,
- security and dynamic-code loading would otherwise expand scope substantially.

Enable/disable remains runtime configuration once the adapter exists.

See `docs/adr/0009-wgt-owned-presentation-and-contract-driven-service-integration.md`.

## 12. Contract Isolation

WGT Core projects must not directly depend on:

- `Illumination.Domain`,
- `Vocation` internal modules,
- foreign persistence implementations.

Service-specific adapter/host projects may depend on the **Published Contract** artifact required to integrate that Service.

Composition occurs at the outer application host.

Example:

```text
WGT.Domain
WGT.Application
        ↑
WGT.Integrations.Illumination
        ↓
Illumination.PublishedContracts
        ↓
Illumination runtime
```

The dependency direction must not allow WGT Domain to import Illumination Domain.

## 13. Failure Isolation

Each Integration Adapter must translate transport/runtime failure into bounded WGT observations/errors.

One adapter failing during discovery or invocation must not prevent unrelated adapters from loading.

## 14. Version Isolation

Provider-specific contract versions are evaluated by the relevant Integration Adapter.

WGT Core receives normalized compatibility results.

This prevents WGT Core from knowing every provider's detailed versioning scheme.

## 15. Accepted Presentation Decision

V1 uses WGT-native executable presentation delivered with the WGT application.

This is appropriate because:

- WGT is intended to feel like one coherent application,
- Windows and iPhone need intentionally different interaction design,
- arbitrary runtime-downloaded native code is not an appropriate iPhone plugin baseline,
- static/AOT-compatible integration is easier to test and release,
- foreign domain behavior can remain service-owned behind Published Contracts without shipping foreign UI code.

The main V1 candidates are:

### A. WGT-owned native views per integration

Service publishes data/commands; WGT Integration Adapter implements native Avalonia presentation.

### B. Declarative service-provided UI description

Service publishes constrained UI metadata/schema interpreted by WGT.

### C. Portable service-provided UI module

Service provides executable presentation code/surface loaded by WGT.

### D. Hybrid

WGT-native views are the baseline; later explicit contribution types can be added where justified.

The current architecture recommendation is **D with A as the V1 baseline**.

This best matches the product goal of one coherent Wiiii Got This interface while retaining an extension path.


## 16. V1 Presentation Baseline

Accepted V1 rule:

> A Service publishes semantics, data, commands, requirements, and bounded presentation metadata. Wiiii Got This supplies the executable native presentation for the Capabilities it supports.

Conceptually:

```text
Foreign Service
├── Published Read Contracts
├── Published Command Contracts
├── Capability metadata
└── optional bounded presentation metadata
            │
            ▼
WGT Integration Adapter
            │
            ▼
WGT-native Avalonia Presentation
├── Windows layout
└── iPhone layout
```

This does not require identical UI across Windows and iPhone.

The same Capability may have:

- a desktop-rich WGT view,
- a mobile-low-interaction WGT view,
- no supported presentation on a particular Client Environment.

## 17. Service-Supplied Presentation Metadata

The provider may still publish bounded metadata needed for correct presentation, for example:

- user-facing capability title/description,
- semantic field labels where genuinely provider-owned,
- interaction options,
- allowable commands/actions,
- content payloads,
- validation hints,
- capability-specific status information.

WGT decides:

- layout,
- navigation,
- platform adaptation,
- WGT design-system styling,
- current presentation availability.

Do not create a generic arbitrary UI schema in V1.

## 18. Future Extension

If repeated integration work later demonstrates that many Services need the same declarative presentation primitives, WGT may introduce a versioned declarative Presentation Contribution contract.

That contract should emerge from concrete repeated patterns rather than being designed as a universal UI language in advance.

Executable native plugin downloading remains outside the V1 model.
