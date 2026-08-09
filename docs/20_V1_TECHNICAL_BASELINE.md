# Wiiii Got This – V1 Technical Baseline

## Status

Accepted implementation baseline for repository bootstrap.

## Runtime and UI

- C# / .NET 10
- Avalonia 12
- CommunityToolkit.Mvvm for presentation state/commands
- compiled/static-friendly bindings and code paths where supported
- Windows desktop primary client
- iPhone primary client
- optional future web client

## Persistence

- SQLite
- Microsoft.Data.Sqlite.Core
- SQLitePCLRaw.bundle_green
- explicit SQL repositories/adapters
- WGT-owned ordered SQL migrations
- System.Text.Json for bounded structured metadata
- source-generated JSON metadata preferred on AOT-sensitive paths

## Testing

Initial test layers:

- pure domain tests,
- application tests using fakes,
- SQLite integration/migration tests,
- Integration Adapter contract tests,
- Avalonia presentation/view-model tests where valuable,
- Windows smoke test,
- iPhone smoke build/device test before substantial feature growth.

Use the current supported xUnit generation selected during repository bootstrap; avoid tying domain design to a test framework.

## Project Shape

Use Avalonia's cross-platform host model as the outer application basis, while keeping domain/application/infrastructure projects explicit.

Working solution direction:

```text
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

The Avalonia template/project conventions may justify slightly different outer project naming during bootstrap.

Do not collapse Domain/Application/Infrastructure boundaries merely because the stock template uses one shared `Core` project.

## Dependency Direction

```text
Presentation/Hosts
       ↓
Application
       ↓
Domain

Infrastructure ──implements──> Application ports

Integration Adapter
       ↓
provider Published Contract
```

WGT Domain must not depend on:

- Avalonia,
- SQLite,
- Microsoft.Data.Sqlite,
- foreign Service implementations,
- transport frameworks.

## Dependency Injection

Use ordinary explicit .NET composition.

Do not add:

- reflection-based plugin scanning,
- a heavyweight composition framework,
- runtime-downloaded native assemblies.

A small built-in DI container may be used if useful, but its selection is not architecture-significant enough to require a separate ADR.

## Logging

Use structured application logging behind standard .NET logging abstractions where useful.

Do not log:

- cryptographic secrets,
- recovery material,
- full foreign sensitive payloads.

## Formatting and Analysis

Repository bootstrap should enable:

- nullable reference types,
- warnings appropriate for production code,
- deterministic formatting,
- analyzers that expose trimming/AOT issues early,
- iOS publish/build checks early enough to detect incompatible dependencies.

## Version Pinning

Pin major/minor technology baselines through project files and central package management where useful.

Patch dependencies may be updated through normal maintenance after tests pass.

Public integration contract versions are independent of NuGet/package versions.
