# ADR-0003: V1 Integration Adapter and Presentation Model

- Status: Accepted
- Date: 2026-08-09

## Context

Wiiii Got This is intended to feel like one coherent application on Windows and iPhone while integrating independently owned bounded contexts such as Vocation and Illumination.

The architecture must preserve:

- foreign domain ownership,
- independent service evolution,
- explicit versioned contracts,
- platform-specific WGT presentation,
- runtime enable/disable behavior,
- capability-level availability,
- iPhone compatibility.

A generic runtime plugin system that downloads arbitrary new native executable code after application installation would add substantial complexity and is not required for the first product.

The initial product is also controlled: the first integrated Services are known projects, not arbitrary third-party marketplace plugins.

## Decision

For V1:

1. **Executable Service Integration Adapters are delivered with the Wiiii Got This application version.**
2. **Wiiii Got This owns the executable native presentation for supported Capabilities.**
3. Services publish their semantics through explicit versioned:
   - Service/Capability publication metadata,
   - Read Contracts,
   - Command Contracts,
   - bounded presentation metadata where required.
4. Services do **not** publish arbitrary executable UI code to be dynamically loaded by WGT in V1.
5. Adding support for a completely new integration family may require a new WGT application release.
6. Once an adapter exists, the following remain runtime-dynamic:
   - integration enablement,
   - Device overrides,
   - Service reachability,
   - Capability publication,
   - compatible contract versions,
   - foreign data,
   - commands,
   - Availability,
   - synchronization state.

## Presentation Rule

The baseline is:

```text
Foreign Service
├── Published Read Contracts
├── Published Command Contracts
├── Capability metadata
└── bounded presentation metadata
            │
            ▼
WGT Integration Adapter
            │
            ▼
WGT-native Avalonia Presentation
├── Windows-specific/adaptive presentation
└── iPhone-specific/adaptive presentation
```

The same Capability does not need identical UI on every Client Environment.

For example, an Illumination capability may expose:

- rich coding interaction and statistics on Windows,
- low-interaction recall on iPhone.

The learning semantics remain Illumination-owned.

## Integration Adapter Boundary

An Integration Adapter may depend on the foreign Service's **Published Contract** artifact or wire contract.

It must not depend on foreign internal domain/persistence types merely for convenience.

WGT Core must not depend on:

- `Illumination.Domain`,
- Vocation internal Python modules,
- foreign persistence implementations.

The composition root may host a foreign runtime where appropriate, but the WGT domain/application layer communicates through the published integration boundary.

## Plugin-Like Product Semantics

`Plugin-like` in V1 means:

- known Service Integrations can be enabled/disabled,
- unavailable Services fail independently,
- published Capabilities can appear/disappear/evolve,
- integration can be removed from the active experience without deleting foreign domain data,
- Services remain independently owned.

It does not mean:

- arbitrary unsigned or runtime-downloaded native code,
- a public plugin marketplace,
- one generic UI language for all Services.

## Service-Supplied Presentation Metadata

A Service may publish bounded metadata required to present a Capability correctly, for example:

- user-facing title,
- description,
- semantic labels,
- supported user actions,
- content payload,
- command choices,
- capability-specific status.

WGT owns:

- layout,
- navigation,
- design-system styling,
- platform adaptation,
- presentation availability.

## Future Extension

If repeated integrations later show stable common presentation patterns, WGT may introduce a versioned declarative Presentation Contribution contract.

Examples could eventually include constrained:

- list/detail structures,
- forms,
- actions,
- charts,
- content blocks.

Such a contract must emerge from concrete repeated use cases.

It must not be designed in advance as a universal arbitrary UI description language.

## Consequences

### Positive

- coherent WGT UX,
- clear iPhone deployment model,
- static/AOT-friendly application composition,
- easier testing and release control,
- foreign business logic remains behind contracts,
- Windows and iPhone can intentionally diverge in presentation.

### Trade-off

Supporting a completely new Service integration may initially require:

- a WGT adapter implementation,
- WGT presentation implementation,
- a WGT release.

This is accepted for V1.

## Rejected Alternatives

### Arbitrary runtime-loaded native plugins

Rejected for V1 because they add deployment, signing, AOT, security, compatibility, and iPhone distribution complexity without a current product requirement.

### Service-provided universal declarative UI

Rejected as the V1 baseline because no stable shared UI vocabulary has yet emerged from multiple integrations.

### Embedding every foreign standalone UI

Rejected because it undermines the coherent Wiiii Got This experience and would preserve duplicated/heterogeneous UI unnecessarily.

## Follow-up

Revisit declarative Presentation Contributions only after multiple concrete integrations establish repeated patterns.
