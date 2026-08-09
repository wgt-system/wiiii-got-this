# Wiiii Got This – Ubiquitous Language

## 1. Purpose

This document defines the current Wiiii Got This domain language and explicitly marks terms whose semantics are still provisional.

Implementation terminology must not silently become domain language.

In particular, framework terms such as plugin, package, endpoint, container, process, route, component, widget, or microfrontend must not be treated as domain concepts unless the specification explicitly adopts them.

## 2. Accepted Core Concepts

### Wiiii Got This

The product and bounded context responsible for integrating independently owned capabilities across the user's devices and platforms and presenting them through a coherent experience.

Wiiii Got This is not synonymous with one specific executable or client.

### Service

A Wiiii Got This-visible provider identity that intentionally publishes capabilities through explicit integration contracts.

`Service` describes the provider boundary from the Wiiii Got This perspective.

It does **not** imply a one-to-one relationship with:

- bounded context,
- repository,
- process,
- container,
- HTTP server,
- executable.

The precise cardinality between external bounded contexts/applications and Wiiii Got This Services may vary and must not be assumed from deployment topology.

### Service Identity

The stable identity by which Wiiii Got This distinguishes one integrated service provider from another across discovery, configuration, compatibility evaluation, and capability resolution.

Service Identity must not depend solely on a temporary endpoint address.

The exact identifier format is unresolved.

### Capability

An explicitly published unit of functionality that a Service makes available for Wiiii Got This integration.

A Capability describes what may be used through the integration boundary.

It is not a foreign domain entity and does not transfer business-rule ownership to Wiiii Got This.

A Service may publish multiple Capabilities.

The exact capability taxonomy and granularity remain unresolved.

### Service Integration

The Wiiii Got This-side relationship/configuration that makes a Service eligible to contribute Capabilities to the user's Wiiii Got This experience.

Service Integration is distinct from the Service itself.

Disabling an integration does not delete or reset the foreign Service.

### Integration Enablement

The configured decision that a Service Integration may participate in Wiiii Got This.

Enablement is distinct from technical availability.

An enabled Service can still be unreachable, incompatible, or unsupported.

V1 uses layered enablement:

- the global Service Integration state is the default,
- a Device may explicitly override it,
- without a Device override, the global state is inherited.

Capability Availability is evaluated separately.

### Service Registration

The act or state by which a Service becomes known to an accepted registry or Wiiii Got This integration boundary together with the integration metadata required for later discovery/resolution.

Registration does not imply that the Service is currently reachable or enabled.

The technical mechanism is unresolved.

### Service Discovery

The process by which Wiiii Got This learns that a Service exists or refreshes information needed to resolve it.

Discovery may use registered, configured, local, remote, or future mechanisms.

Discovery does not imply trust or enablement.

### Availability

A derived judgment describing whether a Service or Capability can currently be used in a particular context.

Availability is not assumed to be a single stored boolean.

It may depend on distinct reasons such as:

- enablement,
- reachability,
- compatibility,
- platform support,
- device support,
- required provider/runtime presence,
- required data presence,
- dependency health.

The final set of dimensions and result states remains to be specified.

### Unavailable Reason

An explicit reason explaining why a Service or Capability cannot currently be used.

Unavailable Reason exists to preserve distinctions such as disabled, unreachable, incompatible, unsupported, or missing prerequisites rather than collapsing them into a generic failure.

The canonical reason set remains open.

### Presentation Contribution

The bounded presentation information associated with a Capability that allows Wiiii Got This to determine how that Capability can be presented or invoked.

In V1, executable presentation is WGT-native Avalonia code delivered with WGT Integration Adapters.

Services may publish bounded presentation metadata, but not arbitrary runtime-downloaded native UI code.

A future declarative Presentation Contribution language may be introduced only after repeated concrete patterns justify it.

### Invocation

A user- or application-initiated transition from Wiiii Got This into use of a published Capability through an accepted integration mechanism.

Invocation may be embedded or may delegate externally when explicitly supported.

## 3. Device and Platform Terms

### Device

A Wiiii Got This installation on a user-recognizable computing device, identified by a stable WGT-owned Device Identity.

The physical hardware itself is not the aggregate.

A fresh installation normally receives a new Device/install identity and credentials. Trust is restored only through explicit enrollment/recovery, not by hardware fingerprinting.

### Platform / Client Environment

The execution/presentation context relevant to whether Wiiii Got This and an integrated Capability can be presented and invoked.

The required V1 Client Environments are:

- Windows desktop,
- iPhone.

Platform is contextual input/value rather than a separately managed aggregate.

Only dimensions required by concrete capability/presentation resolution should be modeled.

### Current Context

A descriptive term for the Device, Client Environment, runtime observations, and other facts used during Capability Resolution.

It is not an additional aggregate.

## 4. Compatibility Terms

### Contract Version

The version identifier of a published integration contract.

Contract versions exist at the integration boundary and must not mirror internal persistence or domain-model versions accidentally.

### Compatible

A Service or Capability is compatible when Wiiii Got This and the provider share an explicitly supported contract semantics/version for the intended interaction.

Compatibility does not imply reachability or enablement.

### Requirement

A condition published or known at the integration boundary that must be satisfied for a Capability to be usable.

Examples may later include platform features, provider presence, or data availability.

The exact requirement model is unresolved.

## 5. Terms Explicitly Not Equivalent

### Service != Bounded Context

A Service is the Wiiii Got This-visible provider boundary.

A bounded context is a domain ownership boundary.

They may align, but the architecture must not assume they always do.

### Service != Process

A service may be implemented by one process, several processes, an in-process adapter, or remote infrastructure.

### Service != Plugin

`Plugin` currently describes desired enable/disable product behavior and possibly a future packaging mechanism.

It is not the canonical term for the Service.

### Capability != Domain Entity

A Capability is an integration-boundary concept.

For example, a Vocation capability may expose job information without making `JobOpportunity` a Wiiii Got This entity.

### Capability != Presentation Contribution

Capability describes functionality.

Presentation Contribution describes how Wiiii Got This may integrate or present that functionality.

### Enabled != Available

Enabled is configuration.

Available is a contextual runtime/compatibility judgment.

### Reachable != Available

A reachable service may still be incompatible or unsupported.

### Registered != Reachable

Registration makes a service known.

It does not prove current connectivity.

### Registered != Enabled

A known service may remain disabled.

### Device != Platform

A Device is a user-recognizable computing target.

A Platform is the relevant execution/presentation environment.

Their exact relationship is still under specification.

### Integrated Presentation != Domain Ownership

Presenting a foreign capability inside Wiiii Got This does not make the foreign business model part of the Wiiii Got This domain.

## 6. Foreign Domain Terms

### Vocation Domain Concepts

Terms such as JobOpportunity, Posting, Company, Assessment, Decision, and Research remain Vocation-owned.

They must not become Wiiii Got This domain entities.

Published Vocation DTOs or projections are boundary types.

### Illumination Domain Concepts

Terms such as Learning Item, Review, Learning State, Deck, scheduling, and learning progress remain Illumination-owned.

They must not become Wiiii Got This domain entities.

Published Illumination DTOs or projections are boundary types.

## 7. Architecture Terms, Not Domain Terms

The following should remain architecture/implementation language unless a later domain decision explicitly promotes them:

- Docker container,
- pod,
- HTTP endpoint,
- REST,
- gRPC,
- WebSocket,
- process,
- thread,
- package,
- DLL,
- NuGet package,
- npm package,
- microfrontend,
- WebView,
- IPC socket,
- mDNS,
- reverse proxy,
- API gateway,
- database,
- cache.

## 8. Candidate Terms Requiring Further Decision

### Integration Scope

Potential term for the scope in which enablement/configuration applies.

Possible scopes include user-wide, device-specific, platform-specific, or layered configuration.

No semantic model is accepted yet.

### Local Service

Candidate term for a Service whose required provider runtime is available on the current device or local environment.

The networking definition of `local` must not be assumed prematurely.

### Remote Service

Candidate term for a Service whose required provider runtime is reached outside the current device/process environment.

A remote Service does not imply remotely authoritative domain data.

### Replicated Capability

Candidate term for a Capability that can operate from intentionally transferred service-owned state without the original provider runtime being live.

This term must not be accepted before synchronization ownership and semantics are resolved.

### Service Registry

Candidate term for a catalog or component that stores registered Service identities and published integration metadata.

Whether this is part of Wiiii Got This, generic infrastructure, or a separate bounded context remains open.

## 9. Language Rule

When a term is unresolved, specifications must mark it as provisional rather than selecting familiar terminology from Kubernetes, service meshes, browser plugin systems, mobile app frameworks, or microfrontend architectures.

Domain language follows product semantics, not infrastructure fashion.
