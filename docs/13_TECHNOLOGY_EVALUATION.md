# Wiiii Got This – Technology Evaluation

## Status

Decision proposal. Not yet accepted.

No programming language, UI framework, persistence technology, server technology, or synchronization implementation becomes final until explicitly accepted.

## 1. Decided Product Constraints

The technology stack must support:

- Windows desktop as a primary client,
- iPhone as a primary client,
- coherent integrated presentation,
- local execution of Wiiii Got This domain/application logic,
- optional local hosting of foreign capability runtimes where their bounded context permits it,
- asynchronous cross-device synchronization through always-available infrastructure when enabled,
- local-only operation when synchronization is disabled or prohibited,
- future optional web presentation without making web a V1 requirement,
- explicit published contracts between bounded contexts,
- clean platform adapters for Windows and iOS.

An additional architectural pressure now exists:

- Illumination currently uses C#/.NET for its domain/application implementation.
- A Wiiii Got This client may need to host Illumination capability logic locally, especially on iPhone when the Windows PC is off.
- Reusing an Illumination implementation in-process must preserve published-contract/port boundaries and must not turn its domain into Wiiii Got This domain code.

## 2. Runtime / Programming-Language Candidates

### C# / .NET 10

Strengths:

- direct fit for Windows and iOS,
- can share WGT domain/application code across both primary clients,
- permits local hosting of compatible .NET capability runtimes such as Illumination without reimplementing their business logic,
- mature server option through ASP.NET Core if WGT later needs its own backend,
- strong testing and package ecosystem,
- native iOS AOT support through .NET for iOS.

Risks:

- iOS builds/signing still require Apple/Xcode infrastructure,
- AOT/trimming requires discipline around reflection-heavy libraries,
- using one runtime for WGT and Illumination must not become an excuse for shared domain internals.

Current assessment: **preferred runtime**.

### Kotlin Multiplatform

Strengths:

- stable iOS and desktop targets,
- Compose Multiplatform can share UI,
- very good Java/JVM ecosystem,
- strong native/platform interoperability.

Risks for Wiiii Got This:

- Illumination's C# domain/application runtime could not be hosted locally without an additional interop/deployment boundary,
- introduces Gradle/Kotlin/Native complexity alongside the existing .NET service runtime,
- would make offline Illumination-on-iPhone substantially harder unless Illumination moved technology or ran remotely.

Current assessment: technically strong, but strategically weaker for this specific product.

### Rust / Tauri

Strengths:

- excellent native systems language,
- strong fit for security-sensitive/system-level components,
- Tauri supports desktop and mobile application targets,
- lightweight backend/runtime model.

Risks for Wiiii Got This:

- iOS development still requires macOS/Xcode,
- UI is webview-oriented rather than a direct fit for the current cross-platform native application model,
- hosting existing .NET capability runtimes locally becomes a cross-runtime integration problem,
- increases complexity without a clear WGT-specific payoff.

Current assessment: not preferred for the WGT shell; Rust remains an excellent candidate for future independent services/infrastructure.

## 3. UI Framework Candidates

### Avalonia 12

Strengths:

- C#/.NET,
- Windows and iOS support,
- WebAssembly path exists for a later web client,
- strong desktop orientation,
- shared XAML/UI model where useful,
- platform-specific adapters remain possible,
- aligns with .NET 10 mobile requirements.

Risks:

- current iOS support tiers vary by OS version,
- iOS build/sign/test still requires a Mac/Xcode host,
- shared UI must not force identical desktop/mobile interaction design,
- using Avalonia in both Illumination tooling and WGT could reduce technology variety, though that is not itself an architectural defect.

Current assessment: **preferred UI framework**.

### .NET MAUI

Strengths:

- Microsoft-supported Windows+iOS cross-platform UI,
- Windows uses WinUI 3,
- iOS is a first-class .NET target,
- strong platform API integration,
- same C#/.NET runtime advantages as Avalonia.

Risks:

- more mobile-first heritage,
- potentially less attractive for rich desktop-style WGT screens,
- no direct first-class browser target comparable to Avalonia WebAssembly,
- still requires a Mac for iOS build/sign/test.

Current assessment: **main alternative to Avalonia**.

### Uno Platform

Strengths:

- C#/.NET,
- Windows/iOS/WebAssembly support,
- WinUI-oriented programming model,
- broad platform coverage.

Risks:

- another abstraction/ecosystem to learn,
- less direct benefit than Avalonia/MAUI for this project,
- no compelling WGT-specific advantage currently identified.

Current assessment: viable but third among .NET UI candidates.

### Compose Multiplatform

Strengths:

- stable iOS and desktop UI,
- modern declarative UI,
- optional web target.

Main WGT-specific disadvantage:

- loses the simple same-runtime path for locally hosted .NET capabilities.

Current assessment: excellent general candidate, not preferred for WGT.

## 4. Proposed Client Stack

Current proposal:

```text
Wiiii Got This
├── C# / .NET 10
├── Avalonia 12
├── WGT.Domain
├── WGT.Application
├── WGT.Contracts
├── WGT.Infrastructure
├── WGT.Presentation
├── WGT.Windows
└── WGT.iOS
```

Shared UI should be responsive/adaptive rather than assumed identical.

Platform-specific code belongs in explicit Windows/iOS adapters.

## 5. Persistence

Wiiii Got This's own local state is comparatively small and integration-oriented:

- Device identity/configuration,
- Service Integrations,
- global enablement,
- Device overrides,
- publication metadata/snapshots,
- local synchronization metadata where WGT owns it.

Proposed local database: **SQLite**.

For WGT itself, prefer a thin SQLite persistence layer rather than automatically using EF Core.

Reason:

- WGT persistence is small,
- iOS uses AOT/trimming,
- EF Core has documented limitations on AOT platforms such as iOS,
- avoiding a heavy ORM reduces runtime/reflection pressure.

Exact library/provider remains to be validated in the implementation ADR/spike.

This decision is independent of Illumination's own persistence choice.

## 6. Server Components

A WGT server is not required merely because WGT has mobile/desktop clients.

If WGT later needs a server-owned component for its own domain, the natural default candidate is:

- ASP.NET Core on .NET 10,
- containerizable on the personal server,
- explicit versioned WGT contracts.

However, synchronization/replication may become a **separate bounded context/service** and can use a different technology.

Do not force synchronization into ASP.NET Core merely to keep one language.

## 7. Synchronization Technology

Cross-device synchronization is required at product level, but the owning bounded context and implementation technology remain open.

A separate generic Sync/Relay service is increasingly plausible because it may serve:

- Wiiii Got This,
- Illumination,
- Vocation,
- future bounded contexts.

Generic sync infrastructure may own:

- transport,
- durable relay,
- delivery/acknowledgement,
- retry,
- encrypted envelopes,
- device routing.

Each domain retains ownership of:

- change semantics,
- conflict semantics,
- merge rules,
- authoritative domain transitions.

Technology candidates for such a future service should be evaluated independently. Go and Rust are particularly plausible candidates.

## 8. iOS Build Constraint

Regardless of Avalonia, MAUI, Compose Multiplatform, or Tauri, final iOS development/build/signing requires Apple/Xcode infrastructure.

WGT therefore needs a deliberate Mac build-host strategy.

The developer may continue to use Windows as the primary development machine.

Possible later strategies:

- network-accessible Mac build host,
- dedicated Mac mini,
- suitable macOS CI/build infrastructure.

The exact choice is a deployment/tooling ADR and is not decided here.

## 9. Web

A web UI is optional.

Synchronization does not require a web UI.

If a future WGT web client becomes useful, Avalonia WebAssembly provides one possible same-stack path, but the future web client is not required to reuse the same UI technology if a dedicated web experience is better.

## 10. Recommendation

Unless new Illumination/Vocation architecture findings materially change the requirements:

1. select **C# / .NET 10** as the Wiiii Got This client/domain runtime,
2. select **Avalonia 12** as the first Windows+iPhone presentation framework,
3. use **SQLite** for Wiiii Got This-owned local state,
4. avoid committing WGT itself to a mandatory server,
5. treat **Sync/Replication** as a separate architecture decision and likely separate context/service,
6. retain a deliberate polyglot architecture across independent bounded contexts rather than forcing all future services onto .NET.

## 11. Decision Gate

Explicit user acceptance is required before turning this proposal into ADRs or implementation issues.

In particular, programming language/runtime and UI framework are not yet final.
