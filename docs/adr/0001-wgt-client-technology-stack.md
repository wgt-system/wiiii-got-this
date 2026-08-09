# ADR-0001: Wiiii Got This Client Technology Stack

- Status: Accepted
- Date: 2026-08-09

## Context

Wiiii Got This is the primary cross-platform application shell for integrating independently owned capabilities.

The first required clients are:

- Windows desktop,
- iPhone.

The architecture must support:

- shared Wiiii Got This domain/application logic across both clients,
- platform-specific presentation and runtime adapters,
- local execution of Wiiii Got This logic,
- optional local hosting of foreign capability runtimes,
- future optional web presentation,
- no mandatory WGT server for core operation.

Illumination currently uses C#/.NET for its domain/application implementation. A future WGT iPhone client may need to host Illumination capability logic locally so learning can continue while the Windows PC is offline.

The technology choice must therefore consider not only UI portability but also local capability-hosting compatibility.

## Decision

Use:

- **C#**
- **.NET 10**
- **Avalonia 12**
- **SQLite** for Wiiii Got This-owned local persistence

as the baseline client technology stack.

Primary presentation targets:

- Windows desktop,
- iPhone.

A web client is optional and is not part of the initial required client scope.

Wiiii Got This does not require a mandatory server component merely because it supports multiple Devices.

## Consequences

### Positive

- WGT domain/application code can be shared across Windows and iPhone.
- Avalonia provides one UI technology across both primary clients while still allowing platform-specific adapters and layouts.
- .NET allows compatible foreign capability runtimes such as Illumination to be hosted locally without reimplementing their business logic in another language.
- SQLite fits the comparatively small WGT-owned local state.
- A later WGT web client remains possible without being required now.
- A later WGT server component can use .NET/ASP.NET Core if justified, but this ADR does not require one.

### Constraints

- iOS build, signing, and device testing require Apple/Xcode infrastructure.
- Shared .NET runtime must not become an excuse to share foreign domain internals.
- Illumination and other bounded contexts remain independent even if their runtime code is hosted in the same OS process as WGT.
- Platform-specific UI/OS behavior belongs behind explicit adapters.
- SQLite persistence must not contain foreign authoritative domain state merely because that state is visible through WGT.

## Persistence Note

The baseline prefers a relatively thin SQLite persistence layer for WGT-owned state.

EF Core is not selected for WGT by this ADR.

A concrete SQLite provider/library is an implementation detail to be validated before the persistence slice.

## Rejected Alternatives

### Kotlin / Compose Multiplatform

Technically strong for Windows+iOS, but would complicate local hosting of existing .NET capability runtimes such as Illumination.

### .NET MAUI

Viable and supported, but Avalonia is preferred for Wiiii Got This because Windows is a first-class rich desktop target alongside iPhone rather than merely a secondary desktop target.

### Flutter / Dart

Strong cross-platform UI option, but introduces another runtime/language without solving a WGT-specific problem better than the .NET path.

### Rust / Tauri

Potentially useful for future independent services or native/system components, but not preferred for the primary WGT shell because it would introduce a cross-runtime boundary to .NET-hosted capabilities and a webview-oriented presentation model.

## Follow-up

Separate ADRs are still required for:

- iOS build-host/tooling strategy,
- concrete persistence adapter/library,
- Service publication/transport,
- registration/discovery,
- presentation contribution mechanism,
- security/trust,
- any future WGT server component.
