# Wiiii Got This

Wiiii Got This (WGT) is the system's cross-platform integration/product bounded context. It composes independently owned capabilities into one coherent application while provider services retain authority over their domains and authoritative data.

## Current status

The latest published release remains **v0.5.0**. Branch `dev` is the **v0.6.0 Windows/Desktop release candidate**.

The current Desktop product surface is:

- **Home** — product-first entry surface for actually available WGT product areas;
- **Jobs** — direct Vocation Opportunity Overview workspace with loading/empty/failure handling, local search, mechanical sorting and a virtualized result list;
- **Map** — Vocation-owned published opportunity locations rendered through the Orientation-owned map surface, with WGT-owned selection details and product composition;
- **Settings** — user-facing Integration enablement/device behavior and connection health, with technical publication/contract diagnostics progressively disclosed;
- **Reference Integration** — retained as an explicit development/diagnostic provider rather than presented as a normal end-user product area.

Desktop interaction is hardened for keyboard navigation, visible focus, recovery from provider failures, compact-window use and Windows UI Automation naming. The shared presentation continues to use Avalonia Fluent Light/Dark resources.

### Map ownership

WGT does **not** own a generic map engine.

The current Vocation Map path is:

```text
Vocation Published Map Projection 1.0
    ↓
WGT Vocation consumer/application seam
    ↓
WGT presentation adapter
    ↓
Orientation Host Bridge 1.0 / Spatial Scene
    ↓
Orientation map surface
```

Vocation remains authoritative for Work Location, Precision, opportunity identity and job-market meaning. Orientation owns generic geospatial rendering/interaction, basemap integration, clustering and related map capabilities. WGT owns product navigation, composition and host presentation.

The Desktop host currently packages an exact tested Orientation consumer artifact pinned by `src/WiiiiGotThis.Desktop/orientation-map/ORIENTATION_SOURCE_SHA.txt`. Do not replace that artifact merely because a newer Orientation release exists; update it only through an explicit tested consumer-artifact refresh.

### Platform scope

Windows/Desktop is the actively validated product target for the current milestone.

The iOS project remains in the repository, but the current real iOS composition contains only the Reference Integration. Vocation Jobs and Map are not claimed as iPhone capabilities. Real Apple runtime support remains deferred until Mac/Xcode/physical-iPhone validation is deliberately resumed; that deferred platform gate does not block Windows/Desktop releases.

## Architecture in brief

WGT owns devices/platforms, Service/Capability integration, registration/configuration, availability/compatibility interpretation, product navigation/invocation and WGT-native host presentation.

Vocation, Illumination, Orientation and Conveyance remain independent bounded contexts. WGT does not import their domain models, read their persistence, or take ownership of their semantics. Integration uses explicit provider-owned Published/Application Contracts and provider-specific adapters.

System-wide capability ownership is authoritative in `wgt-system/architecture`. In particular:

- Vocation owns job-market semantics and the Published Opportunity Overview / Map Projection contracts;
- Orientation owns the generic geospatial capability;
- Conveyance owns accepted generic durable opaque delivery;
- Illumination owns learning semantics and future learning-state synchronization/reconciliation semantics.

## Repository layout

| Path | Responsibility |
| --- | --- |
| `src/WiiiiGotThis.Domain` | WGT domain model and invariants |
| `src/WiiiiGotThis.Application` | WGT use cases and application ports |
| `src/WiiiiGotThis.Contracts` | WGT-owned contracts/read models |
| `src/WiiiiGotThis.Infrastructure` | SQLite persistence and technical adapters |
| `src/WiiiiGotThis.Integrations.Reference` | Development/reference integration |
| `src/WiiiiGotThis.Integrations.Vocation` | Strict Vocation published-contract consumer adapter |
| `src/WiiiiGotThis.Presentation` | Shared Avalonia product presentation |
| `src/WiiiiGotThis.Desktop` | Validated Windows/Desktop composition root and packaged Orientation consumer artifact |
| `src/WiiiiGotThis.iOS` | Deferred iOS composition target |
| `tests/` | Domain, application, infrastructure and integration regression coverage |
| `docs/` | Architecture, contracts, acceptance criteria and implementation records |

## Prerequisites

For the active Desktop workflow:

- Windows;
- .NET 10 SDK;
- NuGet restore access;
- WebView2 for the Orientation-backed Map host.

Mac/Xcode/iPhone infrastructure is required only when real Apple runtime work is deliberately resumed.

## Build and test

From the repository root:

```powershell
dotnet restore WiiiiGotThis.sln
dotnet build WiiiiGotThis.sln
dotnet test WiiiiGotThis.sln
dotnet list WiiiiGotThis.sln package --vulnerable --include-transitive
git diff --check
```

CI performs the Windows restore/build/test/vulnerability gates and a Desktop startup smoke.

## Run the Desktop app

```powershell
dotnet run --project src/WiiiiGotThis.Desktop/WiiiiGotThis.Desktop.csproj
```

The Reference Integration works without a foreign provider. Vocation product surfaces additionally require a compatible local Vocation runtime exposing its accepted published HTTP contracts. WGT does not use or inspect Vocation persistence.

## Documentation

Start with the accepted [architecture](docs/10_ARCHITECTURE.md), [acceptance tests](docs/11_ACCEPTANCE_TESTS.md), and [V1 technical baseline](docs/20_V1_TECHNICAL_BASELINE.md). The [context map](docs/06_CONTEXT_MAP.md), [published contracts](docs/08_PUBLISHED_CONTRACTS.md), [service-integration runtime](docs/16_SERVICE_INTEGRATION_RUNTIME.md), [iOS build tooling](docs/18_IOS_BUILD_TOOLING.md), and [foreign-context alignment](docs/23_FOREIGN_CONTEXT_ALIGNMENT.md) describe the corresponding boundaries in detail. Accepted decisions are recorded in [`docs/adr/`](docs/adr/); deferred decisions are listed in [docs/22_DEFERRED_DECISIONS.md](docs/22_DEFERRED_DECISIONS.md).

The [Architecture Model](docs/model/README.md) provides the derived service-local C4 runtime view.

## v0.6.0 release gate

`v0.6.0` is a Windows/Desktop candidate until Control Plane approval.

Before release:

- final `dev` restore/build/test/vulnerability/startup-smoke gates must be green;
- strict Vocation contract and capability-isolation regressions must remain green;
- Home / Jobs / Map / Settings Desktop behavior must remain coherent;
- Orientation must remain the sole generic map renderer owner and the packaged consumer artifact must remain explicitly pinned;
- documentation/repository state must match the candidate;
- `main`, the immutable `v0.6.0` tag and the GitHub Release must remain untouched until explicit release approval.

Apple runtime support is a separate deferred claim and is not part of the Windows v0.6.0 acceptance statement.
