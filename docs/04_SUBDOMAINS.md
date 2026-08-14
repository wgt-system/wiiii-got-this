# Wiiii Got This – Subdomains

## 1. Purpose

This document classifies current Wiiii Got This responsibilities without turning them into network services or implementation modules prematurely.

A subdomain is a domain responsibility boundary.

It is not automatically:

- a repository,
- a process,
- a container,
- an API,
- a deployment,
- a microservice.

The current classification is a working model and must remain open to context extraction where stronger independent ownership emerges.

## 2. Core Domain Hypothesis: Contextual Capability Integration

### Responsibility

Determine which independently owned capabilities are relevant and usable in the user's current Wiiii Got This context and make them accessible through a coherent product experience.

### Includes conceptually

- resolving enabled Service Integrations,
- evaluating published Capabilities,
- combining capability requirements with Device/Platform context,
- distinguishing usable and unavailable capabilities,
- selecting supported presentation/invocation paths,
- preserving isolation between independently failing services.

### Why this is core

The distinctive value of Wiiii Got This is not merely storing a list of applications.

The central product problem is:

> Given independent services with different runtimes, contracts, availability, device support, and presentation needs, what can the user actually use here and now, and how should it appear in Wiiii Got This?

Without this behavior, Wiiii Got This becomes a launcher or static portal.

## 3. Supporting Subdomain: Service Integration Configuration

### Responsibility

Represent which external Services are known to Wiiii Got This and which integrations the user intends to participate in the product.

### Includes directionally

- Service Identity references,
- integration enablement,
- user-visible service metadata where needed,
- integration-specific configuration that genuinely belongs to Wiiii Got This.

### Important boundary

Service Integration Configuration does not own the foreign Service's domain state.

### Accepted enablement semantics

Integration enablement is layered:

- global Service Integration state by default,
- optional Device-specific override,
- inheritance of global state when no override exists.

## 4. Supporting Subdomain: Service Registration and Discovery

### Responsibility

Make integrable Services known to Wiiii Got This and refresh the metadata required to resolve their published Capabilities.

### Includes directionally

- registration semantics,
- discovery semantics,
- known provider identity,
- published capability descriptors,
- location/address metadata where legitimately required,
- refresh/re-registration semantics.

### Boundary warning

The existence of registration/discovery behavior does not yet prove that a generic Service Registry belongs inside the Wiiii Got This bounded context.

If registry concerns develop their own lifecycle, trust model, administration, or independent consumers, they may justify separate ownership.

## 5. Supporting Subdomain: Device and Platform Context

### Responsibility

Describe the aspects of the user's current computing environment that affect capability integration and presentation.

### Includes directionally

- Device identity,
- relevant Device properties,
- Platform/execution environment,
- supported presentation environment,
- capability-relevant platform features.

### Important boundary

Hardware inventory and operating-system telemetry must not expand into a generic device-management product unless concrete scenarios require it.

The V1 Device/Platform baseline is defined in `docs/15_DEVICE_PLATFORM_MODEL.md`.

## 6. Supporting Subdomain: Availability and Compatibility

### Responsibility

Explain whether a Service or Capability can currently be used and why.

### Includes directionally

- enablement state as an input,
- reachability observations,
- contract compatibility,
- capability support,
- prerequisite satisfaction,
- unavailable reasons,
- recovery/refresh of current availability.

### Important boundary

Health checks are technical observations.

Wiiii Got This owns the product-facing interpretation only where that interpretation is required to decide capability usability.

Availability must not become a generic observability platform.

## 7. Supporting Subdomain: Presentation Integration

### Responsibility

Connect resolved Capabilities to coherent Wiiii Got This navigation and presentation without absorbing foreign domain semantics.

### Includes directionally

- capability navigation contribution,
- presentation contribution selection,
- platform-specific presentation decisions,
- external-invocation fallback where explicitly supported,
- degradation when no suitable presentation exists.

### Important boundary

This subdomain does not own:

- Vocation job semantics,
- Illumination learning semantics,
- generic map-domain semantics,
- foreign workflow rules.

The concrete UI/plugin mechanism is an architecture decision and must not be embedded into the domain classification.

## 8. Candidate Concern: Synchronization and Replication

### Current status

Required product capability; the separate Conveyance bounded context is the accepted owner
of generic durable opaque cross-device delivery.

The product direction creates pressure for some service capabilities to remain usable across devices even when the original runtime is absent.

This may require:

- replication,
- synchronization,
- conflict handling,
- encryption,
- device-local copies,
- relay/server infrastructure.

### Accepted ownership boundary

- Conveyance owns generic durable opaque delivery.
- Wiiii Got This owns device/platform integration and presentation.
- Each affected domain owner retains synchronization eligibility, payload meaning,
  authority, consistency, merge, conflict, and reconciliation semantics.
- Conveyance's currently accepted and implemented delivery mode is Current Object.
- Ordered/change delivery is not automatically accepted; a missing generic delivery mode
  returns to the System Architecture Control Plane for an explicit decision.

Do not implement or model generic delivery as a Wiiii Got This aggregate. Domain-specific
synchronization semantics may remain open until the owning service defines and accepts them.

## 9. Candidate Concern: Identity and Trust

### Current status

Required product capability; separate bounded-context/service target accepted.

Service discovery, multiple devices, and remote infrastructure may eventually require:

- device identity,
- service authentication,
- user authorization,
- trust establishment,
- secrets/credentials.

These concerns may be infrastructure, security architecture, or a separate identity/trust context.

They are not currently part of the core domain.

## 10. Candidate Separate Context: Shared Map

A Shared Map bounded context remains a plausible later extraction if several independent services contribute spatial projections.

If introduced, it would conceptually own:

- generic spatial composition,
- rendering-oriented cross-service map projection,
- common map interaction semantics.

It would not own the meaning of service-specific spatial data.

Wiiii Got This may consume the composed presentation as a capability.

## 11. Generic / Infrastructure Concerns

The following are not currently product subdomains:

- programming language,
- runtime,
- database engine,
- HTTP/gRPC/IPC,
- Docker,
- container orchestration,
- reverse proxy,
- serialization library,
- logging,
- metrics collection,
- TLS implementation,
- filesystem layout,
- CI/CD,
- package format.

They may support domain responsibilities but do not become subdomains merely because implementation requires them.

## 12. Not Separate Subdomains or Services At This Stage

Do not create the following merely from nouns in the language:

### Capability Service

Capabilities are central integration concepts, not automatically a standalone network service.

### Device Service

Device context does not justify an independent service by itself.

### Platform Service

Platform evaluation does not justify an independent service by itself.

### Availability Service

Availability evaluation may be a domain component or supporting subdomain; it is not automatically a separately deployed service.

### Presentation Service

Presentation integration may be implemented across clients/adapters and does not automatically belong to a central network process.

## 13. Current Context Shape

The current working shape is:

```text
Wiiii Got This
├── Core: Contextual Capability Integration
├── Supporting: Service Integration Configuration
├── Supporting: Service Registration and Discovery
├── Supporting: Device and Platform Context
├── Supporting: Availability and Compatibility
└── Supporting: Presentation Integration

Unclassified / possible extraction:
├── Synchronization and Replication
├── Identity and Trust
└── Shared Map
```

This diagram does not imply separate deployables.

## 14. Boundary With Vocation

Vocation owns:

- job opportunities,
- companies,
- postings and sources,
- job-market research/evidence,
- assessments and decisions,
- Vocation-specific projections and workflows.

Wiiii Got This may consume published Vocation capabilities/contracts and decide how they are made available on the current device/platform.

## 15. Boundary With Illumination

Illumination owns:

- learning content,
- learning interactions,
- reviews,
- scheduling,
- learning state,
- decks,
- learning progress.

Wiiii Got This may consume published Illumination capabilities/contracts and decide how they are made available on the current device/platform.

## 16. Current Classification Blockers

The following decisions materially affect the later domain model and possibly the bounded-context split:

- the scope of Service Integration enablement/configuration,
- the exact Device identity/lifecycle model,
- the exact decomposition of Platform versus runtime/presentation environment,
- the minimum offline/mobile expectations,
- ownership of synchronization/replication semantics.

These questions should be resolved before the domain model is treated as stable.
