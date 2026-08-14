# Wiiii Got This

Wiiii Got This (WGT) is a cross-platform application and bounded context for integrating independently developed applications and services across a user's devices and platforms. WGT provides a coherent product experience while the integrated services retain ownership of their domains and authoritative data.

## Current status

The current release baseline is **v0.5.0**. Wiiii Got This is the primary user-facing application, with an application-grade Desktop workspace and persistent Home, Jobs, and Settings navigation:

- Jobs is a direct Desktop product destination with polished browsing for the accepted Vocation Published Opportunity Overview 1.0 states: Loading, Loaded, Empty, Unavailable, InvalidContract, and IncompatibleContract;
- Settings is the secondary surface for technical Integration and Capability administration, including enablement, availability, refresh, and device overrides;
- WGT-owned Domain/Application boundaries, local SQLite persistence, and a Reference Integration remain isolated from foreign providers;
- the shared WGT visual foundation provides Fluent Light/Dark behavior, while Mobile follows the same product semantics with phone-appropriate layout;
- Mobile Jobs is exposed only when the required Vocation read seam is composed and effectively available.

The current real iOS composition does not wire a Vocation provider or read seam. Windows Desktop remains the validated runtime; the solution and the `net10.0-ios` target compile on Windows for regression validation only. Real Mac/Xcode/iPhone runtime validation remains outstanding, and Vocation is not accepted or wired as a provider on iPhone. Conveyance is the accepted separate bounded context for generic durable opaque cross-device delivery; its Current Object mode is available, while production interoperability and concrete domain-owned synchronization contracts remain gated. A generic Registry, Shared Map, and additional provider capabilities remain deferred.

## Architecture in brief

WGT owns integration concerns such as service and capability identity, registration, configuration, availability, and presentation. Vocation and Illumination remain independent bounded contexts; WGT does not import their domain models, access their databases, or take ownership of their business semantics. Integration uses explicit, versioned published contracts and provider-specific adapters.

WGT's presentation principle is that Wiiii Got This is the user's primary application: it should feel like a coherent, high-quality native product across Windows and iPhone, with platform-appropriate layouts and interactions rather than a technical dashboard or web homepage. Shared visual foundations may be reused across platforms, while product areas and technical Settings remain distinct presentation surfaces.

The implementation is organized into Domain, Application, Contracts, Infrastructure, Integration Adapters, shared Presentation, and platform Hosts. The Desktop host is the current validated runtime. The iOS host exists for shared-code and `net10.0-ios` compile validation; Apple runtime validation still requires Mac/Xcode/iPhone-capable tooling.

## Repository layout

| Path | Responsibility |
| --- | --- |
| `src/WiiiiGotThis.Domain` | WGT domain model and invariants |
| `src/WiiiiGotThis.Application` | Use cases and application ports |
| `src/WiiiiGotThis.Contracts` | WGT-owned contracts and read models |
| `src/WiiiiGotThis.Infrastructure` | SQLite persistence and technical adapters |
| `src/WiiiiGotThis.Integrations.Reference` | Trivial reference integration |
| `src/WiiiiGotThis.Integrations.Vocation` | Vocation published-contract adapter |
| `src/WiiiiGotThis.Presentation` | Shared Avalonia presentation |
| `src/WiiiiGotThis.Desktop` | Windows/Desktop composition root |
| `src/WiiiiGotThis.iOS` | iOS composition and compile target |
| `tests/` | Domain, application, infrastructure, and integration tests |
| `docs/` | Architecture, contracts, acceptance criteria, and implementation records |

## Prerequisites

- Windows for the currently validated Desktop workflow;
- .NET 10 SDK;
- the repository's .NET workloads and NuGet restore access;
- Mac/Xcode and an iPhone-capable environment only for the outstanding Apple runtime smoke.

## Build and test

From the repository root:

```powershell
dotnet restore WiiiiGotThis.sln
dotnet build WiiiiGotThis.sln
dotnet test WiiiiGotThis.sln
```

The Windows regression compile for the iOS target is:

```powershell
dotnet build src/WiiiiGotThis.iOS/WiiiiGotThis.iOS.csproj -p:BuildiOS=true
```

To inspect transitive package vulnerabilities:

```powershell
dotnet list WiiiiGotThis.sln package --vulnerable --include-transitive
```

## Run the Desktop app

```powershell
dotnet run --project src/WiiiiGotThis.Desktop/WiiiiGotThis.Desktop.csproj
```

The Reference Integration is available without a foreign service. The Vocation path additionally requires a compatible local Vocation runtime exposing its published HTTP contract; WGT does not use or inspect Vocation persistence.

## Documentation

Start with the accepted [architecture](docs/10_ARCHITECTURE.md), [acceptance tests](docs/11_ACCEPTANCE_TESTS.md), and [V1 technical baseline](docs/20_V1_TECHNICAL_BASELINE.md). The [context map](docs/06_CONTEXT_MAP.md), [published contracts](docs/08_PUBLISHED_CONTRACTS.md), [service-integration runtime](docs/16_SERVICE_INTEGRATION_RUNTIME.md), [iOS build tooling](docs/18_IOS_BUILD_TOOLING.md), and [foreign-context alignment](docs/23_FOREIGN_CONTEXT_ALIGNMENT.md) describe the corresponding boundaries in detail. Accepted decisions are recorded in [`docs/adr/`](docs/adr/); deferred decisions are listed in [docs/22_DEFERRED_DECISIONS.md](docs/22_DEFERRED_DECISIONS.md).

The [Architecture Model](docs/model/README.md) provides the derived service-local C4 runtime view.

## Current release gates

The first real Vocation integration is accepted for Windows. Before claiming the equivalent Apple runtime support, the real Mac/Xcode/iPhone smoke must verify startup, provider discovery, capability opening, usable data or empty state, provider-loss isolation, and recovery after restart. Shared-code tests and a Windows iOS compile do not satisfy that gate.
