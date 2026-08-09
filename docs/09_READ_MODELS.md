# Wiiii Got This – Read Models

## 1. Purpose

This document defines Wiiii Got This-owned read models used by its presentation clients.

Read models may combine Wiiii Got This integration state with bounded data obtained through foreign published contracts.

They do not become authoritative copies of foreign domain models.

## 2. Read-Model Principles

1. Read models are presentation/query projections, not aggregate roots.
2. Foreign DTOs may be reshaped for presentation.
3. Foreign domain terminology is preserved where semantically necessary but not re-owned.
4. Volatile availability must expose freshness/observation context where useful.
5. Read models may differ by client/platform without changing domain ownership.

## 3. Service Integration List Item

Purpose:

Show one known Service Integration in settings/management.

Conceptual fields:

- Service Identity,
- display name,
- global enablement,
- current Device override: enabled / disabled / inherit,
- effective enablement,
- registration/discovery state,
- compatibility summary,
- current Service availability summary,
- available Capability count,
- unavailable Capability count,
- last publication refresh/observation time.

## 4. Service Integration Detail

Purpose:

Inspect one Service Integration.

Conceptual sections:

### Configuration

- Service Identity,
- global enablement,
- current Device override,
- effective enablement.

### Publication

- published service name/metadata,
- publication contract version,
- known Capability descriptors,
- deprecation information.

### Runtime

- reachability/health summary,
- compatibility state,
- last observation time.

### Capabilities

For each Capability:

- Capability Identity,
- user-facing label/description,
- effective availability,
- unavailable reason,
- selected/supported presentation option,
- contract version information.

## 5. Capability Navigation Item

Purpose:

Contribute a usable Capability to Wiiii Got This navigation.

Conceptual fields:

- Service Identity,
- Capability Identity,
- user-facing title,
- optional icon/visual metadata,
- navigation grouping/placement as resolved by WGT,
- availability,
- selected presentation target.

### Rule

Navigation metadata does not grant Wiiii Got This ownership of the foreign domain.

## 6. Capability Availability View

Purpose:

Explain whether and why a Capability can be used.

Conceptual fields:

- Capability Identity,
- effective enablement,
- availability state,
- one or more unavailable reasons,
- provider reachability observation,
- compatibility summary,
- platform/device support summary,
- prerequisite summary,
- observation timestamp,
- optional remediation actions.

## 7. Device Overview

Purpose:

Show the user's known Wiiii Got This Devices where needed.

Conceptual fields:

- Device Identity,
- user-facing name,
- current/last-known platform information,
- current Device status if later defined,
- number of explicit integration overrides,
- last-seen/observation information where meaningful.

### Open detail

Device lifecycle/identity semantics must be finalized before persistence/read-model fields are frozen.

## 8. Current Context Capability Catalog

Purpose:

Provide the presentation layer with all capabilities relevant to the current client.

Conceptual shape:

```text
Capability Catalog
├── available
│   ├── Capability Navigation Item
│   └── selected Presentation Contribution
└── unavailable
    └── Capability Availability View
```

The client may choose to hide or expose unavailable entries according to presentation policy.

## 9. Foreign Capability Read Models

A concrete foreign Capability may define a dedicated Wiiii Got This presentation read model.

Example direction:

```text
Vocation Opportunity Overview Contract
    ↓ adapter
WGT Vocation Opportunity Overview View
```

The WGT view may:

- select fields,
- reformat presentation,
- add WGT navigation metadata,
- add Availability context.

It must not:

- become the authoritative Vocation opportunity store,
- invent Vocation assessments,
- persist foreign business state as WGT truth.

## 10. Interactive Capability View State

For interactive service-owned workflows such as future Illumination study:

```text
published foreign interaction state
        ↓
WGT presentation view state
        ↓
user action
        ↓
published foreign command
```

The WGT view state is transient/presentation-oriented.

It does not own the foreign workflow transition rules.

## 11. Diagnostics Read Model

Purpose:

Support development/administration without leaking technical noise into normal user views.

Potential information:

- Service/Capability identity,
- publication version,
- endpoint/adapter source where safe,
- last registration/discovery time,
- last reachability/health observation,
- compatibility decision,
- selected adapter/presentation mechanism,
- failure correlation identifiers.

Diagnostics must not expose sensitive foreign domain payloads by default.

## 12. Cached Read Models

Caching is an architecture optimization, not domain ownership.

If read models are cached:

- cache freshness must be explicit where correctness depends on recency,
- foreign authoritative state must remain foreign,
- stale cache must not be confused with current Availability,
- sensitive-data policies of the owning context must be respected.

## 13. Future Replicated State

Replicated service-owned state is not the same thing as a Wiiii Got This read-model cache.

A future replication model may support offline commands and authoritative reconciliation.

Such data requires separate synchronization semantics and must not be implemented as an opaque UI cache.

## 14. Read-Model Versioning

Internal Wiiii Got This read models do not necessarily require public versioning.

Published cross-context DTOs do.

If a WGT client communicates with a separately deployed WGT backend through a public/stable contract, that client/backend contract will also require explicit version semantics.

This is an architecture decision, not assumed by the domain model.

## 15. Initial Read-Model Priority

A sensible implementation order after architecture selection would likely be:

1. Service Integration List,
2. Service Integration Detail,
3. Current Context Capability Catalog,
4. Capability Availability View,
5. first concrete foreign read Capability,
6. interactive Capability view state later.

This is directional and not yet the implementation plan.
