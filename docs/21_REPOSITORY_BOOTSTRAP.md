# Wiiii Got This – Repository Bootstrap Specification

## Status

Implementation-ready bootstrap specification.

This document describes what the first Luna/Codex repository bootstrap should create. It is not the implementation itself.

## 1. Repository

Canonical repository/folder name:

```text
wiiii-got-this
```

Canonical product name in user-facing/specification text:

```text
Wiiii Got This
```

## 2. Branch Direction

Initial repository workflow:

- `main` — stable accepted milestone state,
- `dev` — active integrated development.

Do not create feature branches or GitHub issues merely to mimic ceremony.

Use them when a change is independently reviewable, risky, or genuinely parallelizable.

## 3. Toolchain

Baseline:

- .NET 10 SDK,
- C#,
- Avalonia 12,
- CommunityToolkit.Mvvm,
- SQLite,
- Microsoft.Data.Sqlite.Core,
- SQLitePCLRaw.bundle_green.

Install/use the current compatible Avalonia templates at bootstrap time.

## 4. Initial Solution Shape

Working target:

```text
WiiiiGotThis.sln

src/
├── WiiiiGotThis.Domain/
├── WiiiiGotThis.Application/
├── WiiiiGotThis.Contracts/
├── WiiiiGotThis.Infrastructure/
├── WiiiiGotThis.Integrations.Reference/
├── WiiiiGotThis.Presentation/
├── WiiiiGotThis.Desktop/
└── WiiiiGotThis.iOS/

tests/
├── WiiiiGotThis.Domain.Tests/
├── WiiiiGotThis.Application.Tests/
├── WiiiiGotThis.Infrastructure.Tests/
└── WiiiiGotThis.Integration.Tests/
```

The bootstrap may adapt the outer Avalonia platform project names to current template conventions.

It must preserve the logical dependency boundaries.

## 5. Dependency Rules

### Domain

May depend on:

- base class library functionality only.

Must not depend on:

- Avalonia,
- SQLite,
- Microsoft.Data.Sqlite,
- HTTP,
- foreign Service packages,
- UI toolkit,
- platform SDKs.

### Application

May depend on:

- Domain,
- application ports/contracts.

Must not depend directly on:

- Avalonia,
- SQLite implementation,
- Vocation/Illumination internals.

### Infrastructure

May depend on:

- Application,
- Domain,
- Microsoft.Data.Sqlite,
- platform-independent infrastructure dependencies.

Implements application ports.

### Integration Adapter

May depend on:

- WGT Application/Contracts as required,
- provider Published Contracts,
- technical transport/runtime adapter.

Must not make provider internal domain types visible to WGT Core.

### Presentation

May depend on:

- WGT Application/read models,
- Avalonia,
- CommunityToolkit.Mvvm.

Presentation must not own business rules.

### Platform Hosts

Own:

- startup,
- platform-specific services,
- composition root,
- OS lifecycle integration,
- iOS/Windows-specific adapters.

## 6. Shared Build Configuration

Bootstrap should create central build configuration appropriate for the current .NET SDK.

Expected baseline:

- nullable enabled,
- implicit usings if useful,
- deterministic builds,
- warnings treated seriously,
- consistent language version inherited from .NET 10/C# baseline,
- central package version management where it reduces duplication,
- formatting/analyzer configuration.

Do not enable rules that create large volumes of meaningless generated/UI warnings.

## 7. AOT / Trimming Discipline

Because iPhone is a first-class target:

- avoid runtime assembly scanning,
- avoid dynamic plugin loading,
- prefer compiled Avalonia bindings,
- prefer source-generated System.Text.Json metadata on AOT-sensitive paths,
- test iOS publishing early,
- treat unexplained trim/AOT warnings as defects rather than normal noise.

## 8. Persistence Bootstrap

Create:

- SQLite connection factory/adapter boundary,
- schema migration runner,
- migration metadata table,
- initial migration,
- repositories only for the first vertical slice.

Do not create speculative tables for:

- Sync/Relay,
- Vocation data,
- Illumination data,
- future Registry,
- future Shared Map.

## 9. Reference Integration Bootstrap

Create one non-business reference integration that proves:

- stable Service Identity,
- publication,
- multiple Capability descriptors,
- compatible/unavailable/unsupported cases,
- WGT-native presentation route,
- adapter failure isolation.

Do not use a real Service as the generic architecture fixture.

## 10. Initial UI

Keep it deliberately minimal.

Required:

- WGT application shell,
- Service Integration management surface,
- Capability catalog/navigation,
- reference Capability screen,
- explicit unavailable state.

Do not spend the first milestone on visual polish or a universal design system.

## 11. iOS Project

The iOS target exists from repository bootstrap.

It must not be postponed until after the Windows implementation.

A real signed/device build may wait for the Mac build-host setup, but the source/project target should exist immediately.

## 12. Tests

Bootstrap should establish:

- domain test project,
- application test project,
- infrastructure/migration test project,
- integration/reference-provider test project.

The first domain tests should encode accepted invariants from `docs/11_ACCEPTANCE_TESTS.md`.

## 13. CI

Initial CI should at minimum run what is available without Apple signing infrastructure:

- restore,
- build appropriate shared/Windows targets,
- tests,
- formatting/static analysis as accepted.

Add Mac/iOS CI once an actual macOS runner/build host is selected.

Do not pretend iOS is verified merely because shared .NET projects compile.

## 14. Documentation Gate

The repository must include these specifications as source of truth.

Implementation changes that alter:

- domain ownership,
- published contract semantics,
- accepted context boundaries,
- Device trust semantics,
- integration presentation model,

require corresponding documentation/ADR updates.

## 15. First Luna/Codex Work

After bootstrap, agents should receive small scopes.

Good first assignments:

1. Service Integration aggregate + tests.
2. Capability Resolution policy + tests.
3. SQLite migration/persistence adapter.
4. Reference Integration Adapter.
5. minimal shared read models/presentation.
6. Windows smoke.
7. iOS smoke once build host is usable.

Do not parallelize 1 and 2 until their shared terminology/types are stable.

## 16. Completion Gate

Repository bootstrap is complete when:

- solution builds on Windows,
- tests execute,
- accepted dependency rules are represented,
- reference Service can be resolved in tests,
- WGT-owned SQLite persistence survives restart tests,
- Desktop target launches,
- iOS target exists and is structurally buildable,
- no real foreign Service is required for WGT Core.
