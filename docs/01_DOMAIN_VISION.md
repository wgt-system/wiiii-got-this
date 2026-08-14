# Wiiii Got This – Domain Vision

## 1. Purpose

Wiiii Got This is the user's cross-platform application for accessing capabilities provided by independently developed applications and services through one coherent experience.

Its purpose is not to replace the business domains of those services. Its purpose is to make those independent capabilities discoverable, selectable, available, and appropriately presented on the user's current device and platform.

On mobile in particular, the intended experience is that the user can install and open Wiiii Got This as the primary application while capabilities from services such as Vocation and Illumination appear as integrated parts of Wiiii Got This.

## 2. Core Problem

Independent applications can own useful domain capabilities while differing in:

- supported operating systems,
- client technology,
- deployment model,
- local versus remote runtime,
- data locality,
- reachability,
- current health,
- version,
- device requirements,
- presentation needs.

Without an integration layer, the user must know:

- which application provides a function,
- whether that application is installed,
- where it is running,
- whether it is reachable,
- whether the current device supports it,
- which client or URL to open,
- whether its data is available on this device.

That friction becomes especially visible when several personal applications are used across desktop, phone, browser, local network, and optional personal-server infrastructure.

Wiiii Got This addresses the problem at the capability boundary.

Its central question is:

> Which independently owned capabilities are usable for the user here and now, and how should Wiiii Got This make them available on the current device and platform?

## 3. Target User

Wiiii Got This is initially a personal single-user product.

The initial architecture does not need to solve general multi-tenant SaaS administration, enterprise service catalogs, or third-party marketplace governance.

Future identity or multi-user requirements must not be inferred from the existence of multiple devices.

## 4. Primary User Experience

The user should normally experience Wiiii Got This as one coherent application.

For example, a mobile Wiiii Got This client may expose areas such as:

- job opportunities backed by Vocation,
- learning interactions backed by Illumination,
- future capabilities from other services.

The user should not need to open a separate application merely because a capability is owned by another bounded context.

This does not mean that Wiiii Got This owns the foreign domain or copies its business rules.

Opening or delegating to a separate application may remain a supported integration mechanism where a concrete service or platform requires it, but it is not the default product model.

## 5. Central Domain Capability

The core domain hypothesis is:

> Wiiii Got This resolves independently published service capabilities against the user's current device, platform, integration configuration, compatibility, and availability, and presents the usable result through a coherent Wiiii Got This experience.

This includes conceptually:

1. knowing which services are integrated,
2. knowing which capabilities those services publish,
3. determining whether an integration is enabled,
4. determining whether the service and capability can currently be used,
5. understanding relevant device/platform constraints,
6. selecting an appropriate presentation or invocation path,
7. degrading explicitly when a capability is unavailable,
8. isolating failures so unrelated capabilities remain usable.

## 6. Service Independence

An integrated service remains independently owned.

Examples:

- Vocation owns job-market semantics.
- Illumination owns learning semantics.

Wiiii Got This may consume explicit service contracts, but it must not:

- reproduce foreign aggregates,
- read foreign databases,
- depend on foreign persistence schemas,
- import foreign domain classes,
- share business-logic libraries as a substitute for contracts.

The visible unity of the product must not become hidden domain coupling.

## 7. Plugin-Like Integration

The user should be able to activate and deactivate service integrations in a plugin-like manner.

`Plugin-like` describes the product behavior, not the technical mechanism.

The specification does not yet decide whether an integration is distributed as:

- code,
- declarative metadata,
- a remotely discovered contract,
- a local adapter,
- a portable presentation module,
- a combination of mechanisms.

The domain must not depend on one packaging technique before the integration semantics are understood.

## 8. Capabilities

A service may expose one or more capabilities to Wiiii Got This.

A capability describes something the service makes intentionally available for integration.

Examples may eventually include:

- reading a bounded summary,
- browsing a service-owned projection,
- starting a service-owned workflow,
- submitting a command,
- presenting an interactive service workflow.

A capability is not a foreign domain entity.

The exact capability taxonomy is intentionally unresolved until the usage scenarios and published-contract design require it.

## 9. Device and Platform Awareness

Wiiii Got This must make device- and platform-dependent decisions.

The same service may be:

- fully usable on one device,
- read-only on another,
- temporarily unreachable on another,
- unsupported on a particular presentation environment.

`Device` and `Platform` are therefore domain-relevant concepts, but their precise identity and boundary must be established by the domain model rather than assumed from hardware or operating-system terminology.

## 10. Availability

Availability is not assumed to be one boolean.

A service or capability may be affected by distinct conditions such as:

- whether the service is known,
- whether its integration is enabled,
- whether an endpoint or local runtime is reachable,
- whether the published contract version is compatible,
- whether the capability is supported by the current environment,
- whether required data is present,
- whether an external dependency is healthy.

The final availability model must preserve enough information for Wiiii Got This to explain why something cannot currently be used.

## 11. Presentation Ownership

Wiiii Got This owns the coherent platform- and device-dependent presentation of integrated capabilities.

The contributing service remains responsible for the semantics of its capability.

For V1, the executable presentation boundary is accepted:

- Services publish versioned Read/Command semantics and bounded presentation metadata,
- WGT-shipped Integration Adapters translate those contracts,
- Wiiii Got This owns the executable native Avalonia presentation on Windows and iPhone,
- arbitrary runtime-downloaded native UI/plugin code is not part of V1,
- explicit external delegation remains possible for capabilities that require it.

Future declarative presentation contributions may be introduced only when repeated concrete integration patterns justify them.

The architecture preserves the distinction between domain ownership and presentation ownership.

## 12. Local and Remote Operation

Wiiii Got This may use:

- local processes,
- local data,
- remote services,
- a personal server,
- containers,
- Docker,
- synchronization or relay infrastructure.

No global rule requires all data to be local or all data to be remote.

Data locality and confidentiality requirements originate from the owning bounded context and explicit integration policies.

The existence of a server must not silently authorize remote persistence of sensitive foreign domain data.

## 13. Multi-Device Direction

The product should support a user moving between devices without having to understand each service's deployment details.

Some capabilities may require a running remote provider.

Other capabilities may eventually remain usable from synchronized or replicated state when the original service runtime is unavailable.

Conveyance is the accepted separate bounded context for generic durable opaque cross-device delivery. The exact domain-owned replication, synchronization, authority, conflict, and reconciliation semantics remain unresolved and must be defined by the affected service before a concrete contract is selected. Conveyance's technical delivery/security capability is provider-owned and separately gated.

## 14. Accepted V1 Supporting Responsibilities

The accepted V1 supporting responsibilities include:

- service identity,
- service registration,
- service discovery,
- integration enablement/configuration,
- device/platform description,
- compatibility evaluation,
- availability evaluation,
- navigation/invocation,
- presentation contribution resolution.

These are WGT responsibility boundaries, not implementation modules. Future generalized
scope naming or additional presentation primitives remain open.

## 15. Boundary Hypotheses To Challenge

The following concerns must not automatically be absorbed into Wiiii Got This:

- domain-specific synchronization and replication,
- future public/multi-user account and authentication architecture,
- generic service registry infrastructure,
- notifications,
- shared map composition,
- backup,
- remote storage,
- secrets management.

Each must be classified later as:

- Wiiii Got This domain responsibility,
- supporting subdomain,
- generic infrastructure,
- foreign-service responsibility,
- or separate bounded context.

## 16. Relationship to Vocation

Vocation remains an independent bounded context and application.

Vocation owns its job-market semantics and data.

The established integration direction is conceptually:

```text
Vocation
    │
    │ Published Read / Capability Contracts
    ▼
Wiiii Got This
    │
    └── device/platform-dependent integration and presentation
```

Wiiii Got This must never access Vocation persistence directly.

## 17. Relationship to Illumination

Illumination remains an independent bounded context and application.

Illumination owns learning content, review semantics, scheduling, and learning progress.

Illumination may publish versioned capabilities, commands, queries, or read contracts that Wiiii Got This can integrate.

Wiiii Got This may present Illumination workflows on platforms for which Illumination itself has no dedicated native client.

This does not transfer learning-domain ownership to Wiiii Got This.

## 18. Shared Map Direction

A later Shared Map bounded context may be appropriate if several services publish spatial contributions that should be composed across domains.

The current hypothesis is:

```text
Domain Service
    │
    │ explicit Map Contribution
    ▼
Shared Map
    │
    │ composed presentation
    ▼
Wiiii Got This
```

Shared Map would not read foreign databases.

This remains a design hypothesis.

## 19. Explicit Non-Goals

Wiiii Got This is not currently defined as:

- a generic public app store,
- a general-purpose enterprise service registry,
- an API gateway for arbitrary third parties,
- a replacement domain model for all integrated applications,
- a shared database,
- a mandatory cloud platform,
- a requirement that every service become remotely hosted,
- a requirement that every bounded context become multiple microservices.

It is also not assumed that every capability must be available on every platform.

## 20. Architectural Direction

DDD and bounded-context ownership come before deployment topology.

A future architecture may contain multiple independently deployable services, containers, local applications, clients, or server components.

Those boundaries must follow actual ownership, scaling, availability, security, or lifecycle requirements.

The design must not create network services merely because a concept has a name.

## 21. Success Direction

Wiiii Got This is successful when:

- the user can access independent service capabilities through one coherent application,
- the current device does not require the user to understand service deployment details,
- unavailable capabilities fail explicitly rather than mysteriously,
- one failing service does not disable unrelated functionality,
- services can evolve without exposing their internal domain models,
- integrations can be enabled or disabled without merging codebases,
- platform-specific presentation can evolve without moving foreign business logic into Wiiii Got This,
- local and remote infrastructure can coexist without weakening data ownership or sensitivity rules,
- new services can integrate through explicit contracts rather than bespoke hidden coupling.

## 22. Current Product Questions

The following remain deliberately open:

- exact capability taxonomy,
- concrete domain-owned synchronization/replication semantics for individual capability classes,
- offline expectations for individual capability classes,
- future generalized presentation-contribution mechanism,
- production security/interoperability completion and provider-owned gates,
- future public/multi-user account/auth architecture.
