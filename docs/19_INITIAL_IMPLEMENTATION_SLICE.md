# Wiiii Got This – Initial Implementation Slice

## Status

Historical, completed initial implementation sequence. This document records the original
post-bootstrap progression and is not a current work order.

This slice deliberately proves WGT's own architecture before integrating unfinished foreign business contracts.

## 1. Goal

Build the smallest vertical Wiiii Got This application that proves:

- the accepted C#/.NET/Avalonia stack,
- Windows and iPhone targets,
- WGT-owned persistence,
- Device identity,
- layered Service Integration enablement,
- Capability Resolution,
- WGT-native presentation,
- Integration Adapter isolation,
- no dependency on live Vocation or Illumination.

## 2. Reference Provider

Use a fake/reference integration provider with intentionally trivial non-business semantics.

It should publish:

- one stable Service Identity,
- several Capability identities,
- one compatible Capability,
- one unsupported Capability,
- one temporarily unavailable Capability,
- bounded presentation metadata.

It should not model jobs, learning, finance, documents, or another real product domain.

Its purpose is to test WGT integration mechanics only.

## 3. First Vertical User Flow

```text
launch WGT
    ↓
current Device is known
    ↓
reference Service Integration is known
    ↓
user enables Service globally
    ↓
WGT resolves Capabilities
    ↓
available Capability appears in native navigation
    ↓
user opens Capability
    ↓
WGT-native view shows reference-provider data
    ↓
user sets Device-specific override
    ↓
effective availability/navigation changes
```

## 4. Required WGT Domain Behavior

Implement first:

- Service Identity,
- Capability Identity,
- Device Identity,
- Service Integration aggregate/configuration,
- global enablement,
- Device override,
- effective enablement policy,
- Capability Resolution Result,
- Availability / Unavailable Reason.

No synchronization business implementation is required in this slice.

## 5. Required Application Use Cases

Initial use cases:

- initialize/read current Device,
- list known Service Integrations,
- enable/disable integration globally,
- set/clear current Device override,
- refresh reference Service publication,
- resolve current Capability catalog,
- invoke/open reference read Capability.

## 6. Required Persistence

Persist only WGT-owned state necessary for the slice:

- current/local Device identity and name,
- known reference Service Integration,
- global enablement,
- Device override,
- validated reference publication snapshot if required.

Use SQLite through a narrow persistence adapter.

Do not add Sync/Relay tables or foreign business-state tables.

## 7. Required Presentation

Windows and iPhone must both have a minimal WGT-native flow.

The presentation need not be visually polished.

Required surfaces:

- application shell,
- Service Integration list/detail or equivalent management surface,
- Capability catalog/navigation,
- one reference Capability view,
- clear unavailable state.

Windows and iPhone may use different layouts.

## 8. Project Boundary Direction

A likely source structure is:

```text
src/
├── WiiiiGotThis.Domain/
├── WiiiiGotThis.Application/
├── WiiiiGotThis.Contracts/
├── WiiiiGotThis.Infrastructure/
├── WiiiiGotThis.Integrations.Reference/
├── WiiiiGotThis.Presentation/
├── WiiiiGotThis.Windows/
└── WiiiiGotThis.iOS/

tests/
├── WiiiiGotThis.Domain.Tests/
├── WiiiiGotThis.Application.Tests/
├── WiiiiGotThis.Infrastructure.Tests/
└── WiiiiGotThis.Integration.Tests/
```

This is a working module/package direction, not permission to invent one network service per project.

The exact Avalonia project arrangement may be adjusted to framework conventions during repository bootstrap.

## 9. Test Gate

Before first real Service integration:

### Domain

- global enablement inheritance,
- Device override wins,
- clearing override restores inheritance,
- disabled/unreachable/incompatible/unsupported remain distinct,
- unrelated Service failure isolation.

### Application

- publication refresh preserves WGT-owned configuration,
- unknown/unsupported contract handling,
- capability catalog resolution,
- adapter failure translation.

### Persistence

- restart preserves WGT-owned configuration,
- migrations are deterministic,
- foreign authoritative data is absent.

### Presentation smoke

- Windows launches and resolves reference Capability,
- iPhone launches and resolves the same logical Capability,
- client-specific layout differences do not change semantics.

## 10. What Is Explicitly Not in the First Slice

Do not implement yet:

- production Synchronization / Relay,
- real encryption/key hierarchy,
- account system,
- automatic LAN discovery,
- generic service registry,
- dynamic native plugin loading,
- declarative universal UI language,
- a new WGT-owned generic map implementation,
- real Vocation integration,
- real Illumination integration,
- web client,
- public distribution.

## 11. First Real Service Integration

After the generic WGT mechanics are proven, integrate the first foreign Service that has an **accepted, versioned, consumer-ready Published Contract**.

Current repository alignment:

- Vocation remains a strong low-risk candidate because its first WGT use is expected to be read-oriented, but its production Mobile/WGT Read Contract is still a later Vocation slice.
- Illumination has already accepted WGT as primary Windows/iPhone presentation, but its concrete WGT interaction contract is intentionally deferred until the relevant application capabilities are ready.

Therefore no fixed provider order is part of WGT architecture.

Selection rule:

```text
provider contract ready first
        ↓
smallest meaningful WGT Capability
        ↓
Integration Adapter + native WGT presentation
```

The first synchronized Illumination flow follows only after Illumination publishes its synchronization semantics.

## 12. Luna / Codex Gate

The first Luna/Codex implementation task should be narrowly scoped and testable.

Do not ask an agent to "build Wiiii Got This."

Suitable first work packages after repo bootstrap include:

- domain identity/value objects and tests,
- Service Integration enablement aggregate/policy,
- Capability Resolution policy,
- SQLite persistence adapter once interfaces are stable,
- reference-provider adapter,
- Windows/iOS smoke presentation after shared read models are stable.

Parallel work is allowed only where project/file/contract overlap is low.
