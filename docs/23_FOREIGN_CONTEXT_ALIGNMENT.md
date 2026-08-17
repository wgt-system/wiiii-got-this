# Wiiii Got This – Foreign Context Alignment

## Status

Repository-grounded WGT consumer-alignment baseline. System-wide ownership is authoritative in `wgt-system/architecture`; this document records only WGT-relevant consequences.

Sources of truth reviewed:

- `wgt-system/illumination` branch `dev`
- `wgt-system/vocation` branch `dev`
- `wgt-system/orientation` branch `dev`
- `wgt-system/conveyance` branch `dev`
- `wgt-system/architecture` branch `dev`

This document records only integration facts already accepted or explicitly planned by the owning bounded contexts. It is not a second Service Catalog or system-wide ownership source.

## 1. Illumination Alignment

Illumination has explicitly accepted the revised Wiiii Got This relationship.

### Accepted Illumination position

Illumination:

- remains an independent bounded context,
- owns learning domain/application/persistence semantics,
- owns future domain-specific synchronization and merge semantics,
- remains locally executable without a mandatory server,
- uses C# / .NET 10,
- uses SQLite + EF Core for its authoritative local persistence,
- may retain Avalonia only as an optional standalone/admin/dev host.

Wiiii Got This:

- is the primary end-user presentation for Illumination on Windows and iPhone,
- may host an Illumination capability runtime locally in-process,
- must communicate through explicit Illumination-owned application/published-contract boundaries,
- must not use Illumination domain objects directly.

This aligns with the WGT V1 Integration Adapter and presentation ADRs.

## 2. Illumination Deployment Alignment

A valid future deployment is:

```text
WGT iPhone process
├── WGT bounded-context code
├── WGT Illumination Integration Adapter
└── Illumination capability runtime
    ├── Illumination Application
    ├── Illumination Domain
    └── Illumination Persistence
```

The process boundary does not erase the bounded-context boundary.

The WGT Integration Adapter sees only the explicit Illumination application/published integration surface.

## 3. Illumination iOS Runtime Gate

Illumination currently retains EF Core for its persistence implementation.

Before WGT depends on local Illumination execution on iPhone, the Illumination project must prove its **actual iOS runtime/persistence configuration** through a focused iPhone integration smoke test.

This is a provider-readiness gate, not a reason for WGT to change its own persistence stack or to duplicate Illumination state.

WGT must not add a fallback implementation of Illumination persistence/business logic merely to bypass a provider-side iOS compatibility problem.

If Illumination later changes its persistence adapter for iPhone, that remains an Illumination implementation decision behind its published boundary.

## 4. Illumination Contract Readiness

Illumination has deliberately **not** yet authored speculative WGT or synchronization APIs.

Its implementation plan places a concrete Integration Surface in a later milestone when an actual consumer and semantics require it.

Therefore WGT may:

- build a reference/fake integration now,
- design its Integration Adapter seam now,
- not invent the real Illumination contract yet.

The first real Illumination WGT contract should be designed jointly when Illumination's study/application use cases needed by WGT are stable enough.

## 5. Illumination Synchronization Alignment

The two contexts agree on the ownership split:

```text
Conveyance
owns:
- generic durable opaque delivery under accepted Conveyance modes
- Current Object as the currently accepted delivery mode
- provider-owned technical delivery/security seams as separately gated

Illumination
owns:
- which learning state synchronizes
- change semantics
- authority
- conflict detection
- merge/reconciliation
- offline learning-domain semantics
```

Illumination additionally requires an iPhone-side local copy of the learning data needed for study when the Windows PC is off.

The concrete replication contract remains deferred.

## 6. Vocation Alignment

Vocation remains an independent bounded context and remains authoritative for the personal job-market domain.

Its current implementation direction includes:

- Python 3.13 + FastAPI/Pydantic backend/application host,
- React + TypeScript + Vite standalone local UI,
- SQLAlchemy/Alembic/SQLite local persistence,
- local authority,
- independent startup and operation.

Its standalone UI remains useful for rich desktop-oriented workflows such as:

- research/import workflows,
- prompt workflows,
- administrative/triage operations,
- rich market views.

Vocation no longer owns a competing generic map renderer or generic geocoding implementation merely because those capabilities are used inside the Vocation UI. Its current `dev` baseline delegates generic map rendering and generic geospatial provider capability to Orientation while retaining all Vocation-specific Work Location, Precision, resolution and job-market meaning.

WGT does not need to replace the entire Vocation standalone surface.

## 7. Vocation → WGT Boundary

Vocation's Context Map defines:

- Open Host Service,
- Published Read Contracts,
- Customer/Supplier,
- Vocation ownership of job-market semantics,
- WGT ownership of device/platform product composition and presentation.

WGT must not:

- import Vocation domain classes,
- read the Vocation SQLite database,
- create a WGT JobOpportunity aggregate,
- reproduce Vocation assessment/decision logic,
- infer Work Location or Precision semantics from Orientation presentation behavior.

## 8. Vocation Runtime Direction

The current natural Windows integration is local out-of-process:

```text
WGT Windows
    ↓
Vocation Integration Adapter
    ↓
versioned local HTTP/JSON Published Contract
    ↓
Vocation FastAPI runtime
```

This is compatible with WGT's provider-specific transport strategy.

There is no requirement to port Vocation to .NET.

Orientation does not replace this provider boundary. For the Vocation Map product surface, WGT consumes Vocation's provider-owned projection and adapts it into the separate generic Orientation renderer boundary.

## 9. Vocation Published Contracts

Vocation has implemented `Published Opportunity Overview 1.0` on `dev`.

Its canonical schema is:

```text
schemas/published-opportunity-overview-v1.schema.json
```

Its local read-only publication endpoint is:

```text
/published/v1/opportunity-overview
```

The contract is client-neutral, versioned, and read-only. It intentionally excludes personal state and later Vocation capabilities. It can be consumed without accessing Vocation's database, domain classes, or internal React API.

Vocation also owns `Published Map Projection 1.0`:

```text
schemas/published-map-projection-v1.schema.json
```

WGT consumes this projection through the Vocation integration boundary and adapts accepted coordinates and supporting provider-published information into an Orientation Spatial Scene. Orientation renders the map but does not become the authority for Work Location, Precision, opportunity identity or other Vocation semantics.

Neither Vocation Published Contract transfers Vocation persistence or domain ownership to WGT or Orientation.

## 10. Vocation Mobile / Synchronization Direction

Initial iPhone Vocation use remains read-oriented, but the concrete provider/data topology is not yet accepted merely because Desktop integration exists.

Vocation may later choose among:

- read-only snapshots,
- replicated mobile read state,
- live remote read service,
- another Vocation-owned publication model.

WGT does not invent that mechanism for Vocation.

Conveyance may transport opaque protected Vocation-owned published data when an accepted delivery mode fits the concrete scenario. Conveyance does not invent the Vocation publication, authority or synchronization semantics.

This means Orientation iPhone renderer readiness and Vocation iPhone data readiness are distinct gates.

## 11. Vocation UI Ownership

The expected split is now:

```text
Vocation standalone UI
├── research/import
├── prompt workflows
├── administration
├── rich Vocation-specific desktop workflows
└── Orientation-hosted generic map surface where spatial presentation is needed

WGT
├── Windows capability/product presentation
├── iPhone capability/product presentation when provider/read seams exist
└── Orientation-hosted generic map surface where spatial presentation is needed
```

Vocation and WGT may host the same Orientation renderer without sharing Vocation domain UI or business logic.

Avoid duplicating every Vocation standalone screen inside WGT. A concrete Vocation Capability is added to WGT only when the integrated/cross-device use case justifies it.

## 12. Orientation Alignment

Orientation is the accepted system owner of generic geospatial capability.

Current accepted responsibility includes, according to the Orientation and system Architecture Sources of Truth:

- generic spatial scene and feature rendering,
- basemap/map-provider integration,
- map lifecycle,
- pan/zoom/selection and other generic map interaction,
- clustering and generic geospatial composition,
- place discovery/geocoding/reverse geocoding,
- routing and generic route representation,
- generic current-position representation.

Orientation does **not** own:

- Vocation Work Location or Precision semantics,
- opportunity/job meaning,
- WGT product navigation or device enablement,
- OS permission prompts or platform-specific location acquisition,
- foreign-domain publication authority.

### Orientation Host Bridge

`orientation.host-bridge` `1.0` is the accepted renderer-host contract.

WGT may transform a provider-owned spatial projection into an Orientation scene and consume generic Orientation interaction/lifecycle events. The current Vocation Map path is:

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

The current Desktop implementation uses the packaged Orientation map artifact in Avalonia `NativeWebView`/WebView2. The previous WGT Mapsui renderer is removed and must not be reintroduced as a fallback generic renderer.

### Platform and transitive dependency rules

Capability availability must be evaluated by actual composed seams rather than by repository dependency alone:

| Product/capability | Required on the device |
| --- | --- |
| Reference capability | WGT only |
| Vocation Jobs | Vocation Opportunity Overview read seam/provider; Orientation is not required |
| Vocation Map | Vocation Map Projection read seam/provider **and** usable Orientation map host |
| Orientation current position | usable Orientation host **and** WGT-owned OS permission/location acquisition |
| Future Orientation place/routing use | the corresponding Orientation application/backend capability, plus a renderer only where presentation requires it |

Vocation's own use of Orientation does not make every Vocation capability transitively dependent on Orientation inside WGT.

The WGT iOS composition currently contains only the Reference Integration. Therefore neither Jobs nor Vocation Map is presently available on the real iPhone host, independently of whether an Orientation WKWebView host can compile or render.

### Current-location ownership

Per Orientation's accepted ADR, WGT owns iOS/Windows permission prompts and platform-specific position acquisition. Orientation receives generic position fixes through its Host Bridge and owns generic validation/use/visualization. Platform SDK types must not leak into Orientation contracts or WGT domain/application semantics.

## 13. Conveyance Alignment

Conveyance is the accepted separate bounded context for generic durable opaque cross-device delivery.

Conveyance owns:

- generic durable delivery,
- Current Object delivery and transport/relay mechanics,
- opaque Current Object storage and delivery,
- later security/trust transport mechanisms as separately accepted.

Conveyance does not own:

- Vocation semantics,
- Illumination semantics,
- Orientation geospatial semantics,
- WGT presentation,
- foreign merge/reconciliation rules.

A previously accepted candidate read path remains conceptually valid where Vocation later chooses a suitable publication/synchronization model:

```text
Vocation
  ↓ provider-owned Published Contract
WGT/provider-side client
  ↓ protect/publish
Conveyance
  ↓ retrieve
WGT iPhone
  ↓ verify/decrypt/validate
WGT presentation
```

If that WGT presentation contains a map, Orientation remains the generic renderer owner; Conveyance does not become spatially aware.

Conveyance currently implements the generic Current Object delivery mode. Production authentication/cryptography interoperability remains separately gated. Retry, ordered/change, and other delivery semantics are not implied.

## 14. First Real Integration Ordering

Vocation Published Opportunity Overview 1.0 was the implemented first real WGT Windows integration because it was the first accepted, versioned, consumer-ready Published Contract.

Repository reality now additionally includes:

- Vocation Published Map Projection 1.0 as a second consumed Vocation capability;
- Orientation Host Bridge 1.0 as the accepted generic map-renderer boundary;
- completed Windows integration of the Vocation Map Projection through Orientation;
- outstanding iPhone provider/data and physical Orientation-host gates.

Future integrations remain selected by actual provider-contract readiness rather than project preference.

## 15. Shared Findings

Current foreign-context alignment preserves these invariants:

1. no shared database across bounded contexts,
2. no cross-context domain-class imports,
3. no shared business-logic library that bypasses contracts,
4. published/application contracts are explicit where frozen,
5. presentation does not transfer domain ownership,
6. physical/process co-location does not transfer domain ownership,
7. server/Docker infrastructure is optional and must not silently change data ownership,
8. synchronization mechanics and domain merge semantics are separate concerns,
9. generic map/geospatial capability belongs to Orientation rather than being duplicated in WGT or provider contexts,
10. transitive service use does not make every product capability depend on every nested service; availability follows the concrete required seams.

## 16. WGT Consequence

No WGT domain reversal is required.

WGT continues to own:

- Device/Platform capability resolution,
- Service Integration and availability interpretation,
- product navigation/composition,
- WGT presentation around integrated surfaces,
- platform permissions and host adapters.

WGT must not own a competing generic map renderer. The current Windows Vocation Map correctly composes Vocation-owned published semantics with the Orientation-owned renderer.

For iPhone, implement and validate the Orientation platform host separately from Vocation mobile-data/provider readiness. Do not expose dead Jobs or Map destinations when their actual provider/read seams are absent.

## 17. Current Readiness

### Illumination

Architecture relationship: **accepted**

Concrete WGT interaction contract: **deferred/not yet published**

Concrete sync contract: **deferred/not yet published**

iPhone local runtime viability: **must be proven by Illumination integration smoke test**

### Vocation

Architecture relationship: **accepted**

Standalone runtime/UI: **implemented direction remains valid**

Published Opportunity Overview 1.0: **implemented and consumed on Windows**

Published Map Projection 1.0: **implemented and consumed on Windows**

Generic map/geocoding duplication: **migrated toward Orientation ownership on current `dev`**

Vocation iPhone provider/read topology: **not yet accepted/composed in WGT iOS**

### Orientation

Architecture relationship: **accepted as separate generic geospatial bounded context**

Host Bridge 1.0: **accepted and consumed by WGT Desktop**

WGT Desktop Orientation host: **implemented/validated**

WGT iPhone Orientation host: **implementation + physical-device validation outstanding**

Current-position permission/acquisition on WGT iPhone: **WGT-owned implementation/runtime gate outstanding**

### Conveyance

Architecture relationship: **accepted as separate Conveyance bounded context**

V1 Current Object delivery: **implemented direction/contract available**

Production security interoperability and concrete WGT foreign-context mobile delivery: **separately gated/not implied by this document**

## 18. Rule

Foreign repository state and the current `wgt-system/architecture` Source of Truth outrank earlier WGT assumptions about foreign implementation details.

When Vocation, Illumination, Orientation or Conveyance accepts a new relevant integration ADR or published/application contract, WGT must consume that accepted boundary rather than preserve stale assumptions.
