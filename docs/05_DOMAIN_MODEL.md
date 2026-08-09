# Wiiii Got This – Domain Model

## 1. Purpose

This document defines the initial logical domain model for Wiiii Got This.

It distinguishes:

- domain ownership,
- identity,
- configuration,
- runtime observations,
- derived availability,
- foreign boundary data.

The model is intentionally independent of persistence, transport, process, package, and UI technology.

## 2. Modeling Principles

1. Wiiii Got This models integration semantics, not foreign business domains.
2. Service, Capability, Device, Platform, and Availability are not automatically separate aggregates or network services.
3. Foreign DTOs remain boundary types.
4. Runtime observations are not automatically authoritative domain state.
5. Derived availability must preserve reasons.
6. Global integration intent and current usability are distinct.
7. Device-specific exceptions must not duplicate the global configuration model.
8. Context boundaries remain more important than code-sharing convenience.

## 3. Aggregate Hypothesis: Service Integration

The first strong aggregate hypothesis is `Service Integration`.

It represents the user's Wiiii Got This-side relationship with one externally owned Service.

Conceptually:

```text
Service Integration
├── Service Identity
├── global Enablement
├── optional Device Overrides
├── accepted/known integration metadata
└── references to published Capability descriptors
```

### Responsibilities

A Service Integration is responsible for:

- identifying the foreign Service from the Wiiii Got This perspective,
- representing whether the Service is globally enabled,
- storing explicit Device-specific enablement overrides,
- retaining only Wiiii Got This-owned integration configuration,
- referring to the currently accepted published integration description.

### Invariants

- exactly one global enablement state exists for a Service Integration,
- zero or one enablement override exists per Device,
- a Device without an override inherits the global state,
- disabling the integration does not delete foreign domain data,
- Service Identity cannot silently change because an endpoint changes,
- foreign business state is not stored as part of this aggregate.

### Effective Enablement

Conceptually:

```text
effectiveEnablement(serviceIntegration, device):
    if explicit device override exists:
        return override
    return global enablement
```

This calculation is domain configuration logic.

It is not the same as Availability.

## 4. Entity / Identity Hypothesis: Service

`Service` is a Wiiii Got This-visible provider identity.

A Service may correspond to an independent application or bounded-context provider such as Vocation or Illumination, but the domain does not require one-to-one alignment with repositories, processes, containers, or executables.

### Candidate attributes

Only integration-facing information is legitimate, such as:

- Service Identity,
- human-readable name,
- published integration-contract identity/version information,
- capability descriptors,
- provider-location descriptors where required,
- optional presentation metadata explicitly intended for consumers.

These are boundary/integration semantics.

Internal persistence or domain-model metadata is forbidden.

### Open detail

Whether `Service` is modeled as a separate entity inside `Service Integration`, a referenced external identity, or a published descriptor plus integration-owned reference remains an implementation/domain-detail decision.

The important invariant is ownership separation.

## 5. Entity / Identity Hypothesis: Device

A `Device` is currently modeled as a user-recognizable computing device whose identity can be referenced by Wiiii Got This configuration and capability-resolution logic.

### Domain relevance

A Device may affect:

- enablement overrides,
- available presentation environments,
- capability support,
- local provider presence,
- locally available replicated state,
- reachability context.

### Candidate state

Potential Wiiii Got This-owned Device state includes:

- Device Identity,
- user-facing name,
- lifecycle status if later required,
- known/current platform observations,
- Device-specific integration overrides.

### Important boundary

Hardware telemetry, OS inventory, and runtime facts should not automatically become authoritative Device domain state.

Observed facts may be refreshed by adapters.

### V1 identity rule

`Device` represents one Wiiii Got This installation on a user-recognizable computing device.

A fresh installation normally receives a new Device/install identity and credentials.

An explicit trust enrollment/recovery flow may reconnect it to the personal trust domain; hardware fingerprinting is not identity.

## 6. Value-Object Hypothesis: Platform Context

`Platform Context` is the current working term for the execution/presentation characteristics relevant to capability resolution.

It may include dimensions such as:

- native mobile,
- native desktop,
- web,
- operating system,
- runtime/browser environment,
- feature availability.

This is currently better modeled as contextual input/value rather than an independently owned aggregate.

The exact decomposition is still open.

## 7. Published Boundary Object: Capability Descriptor

A Service publishes a `Capability Descriptor` describing an integration capability.

This is a boundary concept, not a foreign domain entity.

Conceptually a descriptor may eventually declare:

- Capability Identity,
- contract identity/version,
- supported operations,
- runtime/provider requirements,
- platform/presentation requirements,
- available Presentation Contributions,
- deprecation/version information.

The final schema is not yet defined.

### Ownership

The Service owns what the Capability means and what behavior its contract provides.

Wiiii Got This owns:

- whether the integration is enabled,
- whether the Capability can currently be used,
- which compatible presentation/invocation path to select.

## 8. Value Object: Capability Identity

A Capability needs stable identity within the publishing Service's integration contract.

The identifier must not rely on:

- UI labels,
- endpoint paths,
- route names,
- internal class names.

Exact encoding remains a published-contract concern.

## 9. Domain Policy: Capability Resolution

`Capability Resolution` is a core domain policy/process rather than a separate network service.

Conceptual inputs:

```text
Service Integration
Device
Platform Context
Capability Descriptor
Compatibility facts
Reachability/health observations
Required provider/data presence
```

Conceptual output:

```text
Capability Resolution Result
├── effective enablement
├── availability
├── unavailable reason(s)
├── selected presentation/invocation option
└── diagnostics needed for explicit degradation
```

### Core rule

An enabled Capability is not considered usable until all required conditions for the current context are satisfied.

## 10. Derived Model: Availability

Availability is derived from configuration plus current observations and compatibility.

It must not be persisted as an unquestioned truth when its inputs are volatile.

### Initial availability dimensions

The current model distinguishes at least:

- `disabled` — integration is not effectively enabled,
- `unknown` — insufficient current information exists,
- `unreachable` — required provider cannot be reached,
- `incompatible` — no supported contract semantics/version is shared,
- `unsupported` — current Device/Platform cannot support the Capability,
- `missingPrerequisite` — required provider/data/dependency is absent,
- `available` — required conditions are currently satisfied.

This list is an initial domain baseline and may later be refined.

### Rule

`available` is the positive conclusion.

The other values explain different non-usable conditions and must not be collapsed into one generic false value at the domain boundary.

### Service versus Capability Availability

Service-level availability may describe the provider in general.

Capability-level availability is evaluated separately because:

- a reachable Service may expose an unsupported Capability,
- one Capability may require live provider access while another may use local replicated state,
- version support may differ by Capability contract.

## 11. Runtime Observation Model

Technical adapters may produce observations such as:

- endpoint reachable,
- provider health result,
- local runtime detected,
- contract descriptor retrieved,
- current platform features,
- required local data present.

These observations are inputs to the domain/application layers.

They do not become foreign domain truth.

Freshness/timestamps may be needed because runtime observations age.

## 12. Presentation Contribution

A `Presentation Contribution` describes an allowed way for Wiiii Got This to integrate a Capability into navigation or UI.

It is associated with the Capability boundary.

Possible future forms include:

- descriptive metadata interpreted by Wiiii Got This,
- a service-specific adapter,
- a portable UI surface,
- a remote presentation mechanism,
- explicit external application invocation.

The domain model only requires that Wiiii Got This can reason about available presentation options.

The representation is an architecture decision.

## 13. Presentation Resolution Policy

Wiiii Got This selects a presentation/invocation option appropriate to the current Device/Platform.

Conceptual input:

```text
available Capability
Presentation Contributions
Device
Platform Context
Wiiii Got This client capabilities
```

Conceptual output:

```text
selected presentation path
or
explicit unsupported/unavailable result
```

Presentation selection must not redefine foreign business semantics.

## 14. Service Registration Record – Boundary/Supporting Model

Registration may require Wiiii Got This to retain information that a Service exists and how its published integration description can be refreshed.

A `Service Registration Record` is currently a supporting-model hypothesis.

It may conceptually contain:

- Service Identity,
- registration source,
- discovery/refresh locator,
- last-known published-contract metadata,
- observation timestamps.

### Important boundary

This must not become a copy of the foreign Service's domain or persistence model.

### Context-boundary warning

If registry lifecycle/trust/administration develops independently, Service Registration may be extracted from the Wiiii Got This bounded context.

## 15. Discovery Result

Discovery is modeled as an application/domain-boundary process that produces candidate or refreshed Service registration information.

Discovery does not itself:

- enable the Service,
- authorize the Service,
- prove reachability,
- prove compatibility.

These are separate decisions.

## 16. Foreign Read/Command Models

Published Vocation or Illumination DTOs are not Wiiii Got This domain entities.

Application adapters translate them into presentation-specific or integration-specific transient models where necessary.

Examples:

```text
Vocation Published Opportunity Summary
    ≠ Wiiii Got This JobOpportunity entity

Illumination Published Study Interaction
    ≠ Wiiii Got This LearningItem entity
```

Wiiii Got This should generally avoid introducing mirror aggregates for foreign domain models.

## 17. Synchronization / Replica State

No generic synchronization aggregate is currently accepted.

If Wiiii Got This later stores service-owned replicated state locally, that state must remain explicitly tied to:

- owning Service,
- published replication/sync contract,
- freshness/version/correlation metadata,
- conflict semantics owned by the appropriate domain.

A local replica is not automatically authoritative Wiiii Got This data.

## 18. Aggregate Boundary Summary

Current strongest aggregate/entity hypotheses:

```text
Service Integration
├── Service Identity reference
├── global Enablement
└── Device Enablement Overrides

Device
└── Wiiii Got This-owned device identity/configuration

Published/Boundary:
├── Service Descriptor
├── Capability Descriptor
├── Presentation Contribution
└── foreign Read/Command DTOs

Policies / Derived:
├── Capability Resolution
├── Availability Evaluation
└── Presentation Resolution
```

This model does not imply separate databases, repositories, processes, or services for each element.

## 19. Invariants Summary

1. Foreign domain ownership never transfers through presentation.
2. Service Integration enablement is global by default with optional Device overrides.
3. Missing Device override means inherit global enablement.
4. Enabled and Available are distinct.
5. Reachable and Available are distinct.
6. Registered and Enabled are distinct.
7. Service Identity is stable across endpoint changes.
8. Capability Identity is stable across UI-label or route changes.
9. A failing Service cannot make unrelated Service Integrations unavailable by domain rule.
10. Public contract compatibility must be checked before invocation.
11. Foreign internal persistence/domain types are never public integration contracts.
12. Runtime observations must remain distinguishable from durable configuration.

## 20. Remaining Domain Decisions

The core WGT domain model is sufficiently decided for repository bootstrap.

Still intentionally deferred:

- final Capability taxonomy/granularity beyond concrete integrations,
- detailed Requirement schema,
- production synchronization cryptographic/key semantics,
- future generic registry semantics if a registry context becomes justified,
- future multi-user/account semantics.

These deferred areas must be driven by concrete service scenarios rather than speculative generic frameworks.
