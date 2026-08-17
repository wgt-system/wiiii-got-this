# Wiiii Got This – Foreign Context Alignment

## Status

Repository-grounded WGT consumer-alignment baseline. System-wide ownership is authoritative in `wgt-system/architecture`; this document records only WGT-relevant consequences.

Sources of truth reviewed:

- `wgt-system/illumination` branch `dev`
- `wgt-system/vocation` branch `dev`
- `wgt-system/orientation` branch `dev`
- `wgt-system/conveyance` branch `dev`
- `wgt-system/architecture` branch `dev`

This is not a second Service Catalog. Foreign repository state and the system Architecture Source of Truth outrank stale WGT assumptions.

## 1. System ownership relevant to WGT

WGT is the integration/product host. It owns:

- Device/Platform capability resolution,
- Service Integration configuration and availability interpretation,
- WGT product navigation/composition,
- WGT host/platform presentation,
- platform permission/host adapters where required.

WGT does **not** acquire provider domain authority merely because it presents or composes a capability.

The relevant foreign owners are:

- **Vocation** — job-market semantics and its Published/Application Contracts;
- **Illumination** — learning semantics and future learning-state synchronization/reconciliation semantics;
- **Orientation** — generic geospatial capability;
- **Conveyance** — accepted generic durable opaque delivery.

## 2. Illumination alignment

Illumination remains an independent bounded context and owns:

- Learning Items, Reviews, study/session/progress semantics,
- learning-domain application/persistence rules,
- future domain-specific synchronization/conflict/merge semantics,
- the concrete WGT interaction contract when its product use cases are ready.

WGT may later host or invoke Illumination through an explicit Illumination-owned application/published boundary. WGT must not import Illumination domain objects or duplicate Illumination persistence/business logic.

Illumination has deliberately not frozen a speculative WGT interaction/synchronization contract yet. Therefore WGT must not invent one merely to begin integration.

Any future local Illumination execution on iPhone additionally requires Illumination to prove its actual Apple runtime/persistence viability. That deferred provider/platform gate does not affect the current Windows v0.6.0 candidate.

## 3. Vocation alignment

Vocation remains the authority for the personal job-market domain, including:

- Opportunities/Postings/Companies,
- Work Location and Precision,
- research/import/prompt/triage/assessment/decision semantics,
- private application-domain state,
- Vocation publication semantics.

Its standalone application remains valid for rich Vocation-specific workflows. WGT does not need to reproduce that entire surface.

WGT must not:

- import Vocation domain classes,
- read Vocation persistence,
- create a competing WGT JobOpportunity domain model,
- reproduce Vocation assessment/decision logic,
- infer or rewrite Work Location/Precision semantics from map behavior.

## 4. Vocation → WGT contracts

Current accepted/consumed Windows contracts:

### Published Opportunity Overview 1.0

Canonical Vocation schema:

```text
schemas/published-opportunity-overview-v1.schema.json
```

Local read endpoint:

```text
/published/v1/opportunity-overview
```

WGT consumes the client-neutral, versioned, read-only publication through its Vocation Integration Adapter and strict consumer contract.

### Published Map Projection 1.0

Canonical Vocation schema:

```text
schemas/published-map-projection-v1.schema.json
```

WGT consumes Vocation's published coordinates/supporting fields and adapts them into the separate Orientation scene/host boundary.

Neither contract transfers Vocation persistence or domain ownership to WGT or Orientation.

## 5. Vocation Windows runtime

The current natural Windows integration remains local out-of-process:

```text
WGT Windows
    ↓
Vocation Integration Adapter
    ↓
versioned local HTTP/JSON Published Contract
    ↓
Vocation runtime
```

Orientation does not replace this provider boundary. It only owns the separate generic geospatial capability used where WGT or Vocation needs map/geospatial behavior.

## 6. Vocation mobile direction

No concrete Vocation iPhone provider/data topology is accepted merely because the Desktop integration exists.

Vocation may later choose an appropriate provider-owned read/publication model, potentially using an accepted Conveyance delivery mode for opaque transport where that fits. WGT must not invent Vocation synchronization/publication semantics.

Current WGT iOS composition is Reference-only. Therefore Vocation Jobs and Vocation Map are **not** current iPhone capabilities.

## 7. Orientation alignment

Orientation is the accepted system owner of generic geospatial capability.

Its ownership includes, according to Orientation and `wgt-system/architecture`:

- spatial scenes/features/layers,
- map rendering lifecycle,
- basemap/provider integration,
- pan/zoom/selection,
- clustering and generic geospatial composition,
- place discovery/geocoding/reverse geocoding,
- routing and generic route representation,
- generic Current Location representation.

Orientation does not own:

- Vocation Work Location/Precision or job meaning,
- Illumination learning semantics,
- WGT navigation/integration enablement,
- foreign publication authority,
- generic durable delivery.

## 8. Orientation Host Bridge

`orientation.host-bridge` `1.0` is the accepted map-surface host boundary.

Current WGT Desktop path:

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

The current Desktop implementation hosts an exact packaged Orientation artifact through Avalonia `NativeWebView`/WebView2. The artifact source is explicitly pinned in the Desktop project.

The former WGT Mapsui renderer is removed. WGT must not reintroduce Mapsui, Leaflet or another fallback generic map renderer simply to avoid an Orientation integration issue.

If a generic desired map behavior belongs to rendering/geospatial capability—clustering, basemap behavior, generic selection, geocoding, routing, etc.—route it to Orientation rather than reimplementing it in WGT.

## 9. Transitive dependency rule

Repository/service dependency is not enough to expose a product capability. Availability follows the concrete seams needed on the current device.

| Product/capability | Required on the device |
| --- | --- |
| Reference capability | WGT only |
| Vocation Jobs | Vocation Opportunity Overview read seam/provider |
| Vocation Map | Vocation Map Projection read seam/provider **and** usable Orientation map host |
| Orientation Current Location | usable Orientation host plus WGT/platform-owned position acquisition/permission where applicable |
| Future Orientation place/routing product use | accepted corresponding Orientation capability/boundary plus presentation host where required |

Vocation's own use of Orientation does not make every Vocation capability transitively dependent on Orientation inside WGT.

## 10. Vocation/UI composition

Expected split:

```text
Vocation standalone UI
├── research/import
├── prompt workflows
├── administration/triage
├── other Vocation-specific desktop workflows
└── Orientation-hosted geospatial surface where needed

WGT Desktop
├── integrated Jobs product surface
├── integrated Vocation Map product surface
│   └── Orientation-hosted renderer
└── other accepted provider capabilities as contracts become ready
```

Vocation and WGT may host the same Orientation renderer without sharing Vocation domain UI/business logic.

## 11. Conveyance alignment

Conveyance owns accepted generic durable opaque delivery, including Current Object and its technical delivery mechanics.

Conveyance does not own:

- Vocation/Illumination/Orientation semantics,
- foreign reconciliation,
- WGT presentation,
- provider publication authority.

A future provider may use Conveyance for an accepted delivery scenario without making Conveyance domain-aware. If transported content is later rendered as a map, Orientation remains the geospatial owner.

Production security/interoperability and concrete provider adoption remain separately gated.

## 12. Shared invariants

WGT foreign-context integration preserves:

1. no shared database across bounded contexts;
2. no cross-context domain-class imports;
3. no shared business-logic library that bypasses contracts;
4. explicit Published/Application Contracts where frozen;
5. presentation does not transfer domain ownership;
6. process co-location does not transfer domain ownership;
7. transport and domain reconciliation are separate concerns;
8. generic geospatial capability belongs to Orientation;
9. generic durable opaque delivery belongs to Conveyance;
10. actual product availability follows concrete composed seams rather than transitive repository dependencies.

## 13. Current readiness

### Illumination

- architecture relationship: **accepted**;
- concrete WGT interaction contract: **deferred/not yet published**;
- concrete sync/reconciliation contract: **deferred/not yet published**;
- iPhone local runtime viability: **deferred provider/platform gate**.

### Vocation

- architecture relationship: **accepted**;
- Published Opportunity Overview 1.0: **implemented and consumed on Windows**;
- Published Map Projection 1.0: **implemented and consumed on Windows**;
- generic map/geocoding ownership: **Orientation-owned; duplicate Vocation renderer capability removed from the system ownership model**;
- Vocation iPhone provider/read topology in WGT: **not accepted/composed**.

### Orientation

- architecture relationship: **accepted generic geospatial bounded context**;
- current released system baseline: **v0.3.0**;
- Host Bridge 1.0: **accepted and consumed by WGT Desktop**;
- WGT Desktop Orientation host: **implemented and validated for the current candidate**;
- WGT packaged Orientation consumer artifact: **exact-source pinned; update only through an explicit tested artifact refresh**;
- WGT iPhone Orientation integration: **deferred; not part of active v0.6.0 Windows scope**.

### Conveyance

- architecture relationship: **accepted**;
- Current Object delivery: **available as the current generic mode**;
- production security/interoperability and concrete WGT foreign-context mobile delivery: **separately gated**.

## 14. Current WGT consequence

No WGT domain reversal is required.

The v0.6.0 Windows candidate correctly composes:

- Vocation-owned published job/map semantics,
- Orientation-owned geospatial presentation,
- WGT-owned product navigation/interaction/host composition.

Active work remains Desktop-first. Apple/provider mobile work stays deferred until an actual Apple validation environment and concrete provider-data topology justify reopening it.

## 15. Rule

When Vocation, Illumination, Orientation, Conveyance or `wgt-system/architecture` accepts a new relevant contract/ownership decision, WGT must consume that accepted boundary rather than preserve stale local assumptions.
