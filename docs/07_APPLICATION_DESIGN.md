# Wiiii Got This – Application Design

## 1. Purpose

This document defines Wiiii Got This application use cases and orchestration responsibilities.

Application use cases coordinate domain policies and external adapters.

They must not absorb foreign business logic.

## 2. Application Boundary

The Wiiii Got This application layer coordinates:

- Service Integration configuration,
- Device context,
- Service registration/discovery,
- published Capability descriptors,
- compatibility checks,
- availability evaluation,
- presentation/invocation resolution,
- explicit user-visible degradation.

It interacts with foreign Services only through published integration ports/adapters.

## 3. Use Case: Register or Refresh a Service

### Goal

Make a Service known or refresh its published integration description.

### Inputs

- registration/discovery source,
- Service Identity or candidate descriptor,
- refresh locator/mechanism as defined by the adapter.

### Flow

1. obtain published integration metadata,
2. validate supported outer contract shape/version,
3. establish or match Service Identity,
4. update last-known registration metadata,
5. preserve existing user-owned Service Integration configuration,
6. update known Capability descriptors,
7. record refresh/observation metadata,
8. trigger or allow availability reevaluation.

### Rules

- re-registration must not silently reset global enablement or Device overrides,
- endpoint changes must not create a new Service identity automatically,
- unsupported descriptors are rejected or marked incompatible explicitly.

## 4. Use Case: Discover Services

### Goal

Find candidate or updated Services through configured discovery mechanisms.

### Flow

1. invoke one or more discovery adapters,
2. collect candidate Service descriptors,
3. validate minimum identity/publication information,
4. distinguish known from unknown Services,
5. do not auto-enable newly discovered Services,
6. optionally register/refresh according to accepted policy.

### Boundary

Discovery is not authorization.

## 5. Use Case: Enable Service Integration Globally

### Goal

Make a Service Integration active by default across the user's Devices.

### Flow

1. select a known Service Integration,
2. set global enablement to enabled,
3. preserve all Device-specific overrides,
4. reevaluate effective enablement and capability availability as needed.

### Result

Devices without explicit override inherit enabled.

Devices explicitly overridden to disabled remain disabled.

## 6. Use Case: Disable Service Integration Globally

### Goal

Disable a Service Integration by default without deleting foreign state.

### Flow

1. select Service Integration,
2. set global enablement to disabled,
3. preserve explicit Device overrides,
4. reevaluate affected capabilities.

### Result

Devices without overrides inherit disabled.

An explicitly enabled Device override, if such override values are permitted symmetrically, remains enabled.

## 7. Use Case: Set Device Integration Override

### Goal

Override the global Service Integration enablement for one Device.

### Inputs

- Service Integration,
- Device,
- override value: enabled / disabled.

### Flow

1. validate Device identity,
2. store or replace the override,
3. calculate effective enablement,
4. reevaluate affected capabilities.

## 8. Use Case: Clear Device Integration Override

### Goal

Return one Device to the global Service Integration configuration.

### Flow

1. remove explicit override,
2. effective enablement becomes inherited global state,
3. reevaluate affected capabilities.

## 9. Use Case: Resolve Available Capabilities for Current Context

### Goal

Produce the set of capabilities the current Wiiii Got This client can use or explicitly report as unavailable.

### Inputs

- current Device,
- Platform Context,
- Service Integrations,
- published Capability descriptors,
- current runtime observations,
- client presentation capabilities.

### Flow

For each relevant Service Integration:

1. compute effective enablement,
2. evaluate current Service compatibility/reachability,
3. evaluate each Capability's requirements,
4. evaluate presentation/invocation options,
5. derive Availability and Unavailable Reason,
6. return a stable application read model.

### Rule

Do not invoke a foreign Capability merely to render navigation if a bounded descriptor/read model is sufficient.

## 10. Use Case: Invoke Capability

### Goal

Execute or enter one published Capability after resolution.

### Preconditions

- Service Integration effectively enabled,
- Capability compatible,
- required provider/data available,
- supported presentation/invocation path selected.

### Flow

1. revalidate volatile prerequisites where necessary,
2. invoke the correct service adapter/contract,
3. translate transport/boundary failures into explicit application errors,
4. preserve foreign domain errors without converting them into Wiiii Got This business rules,
5. update runtime observations where useful,
6. return data required for the selected presentation.

## 11. Use Case: Present Foreign Read Capability

### Example

Vocation opportunity overview.

### Flow

1. resolve Capability,
2. query Vocation published read contract,
3. receive boundary DTOs,
4. map to a Wiiii Got This presentation/read model,
5. render according to current client.

### Boundary

Mapping may rename/reshape presentation fields.

It must not create a new authoritative Wiiii Got This job-market model.

## 12. Use Case: Execute Foreign Interactive Capability

### Example

Illumination study interaction.

### Flow

1. resolve Capability,
2. obtain the published interaction state required for the current step,
3. present it through Wiiii Got This,
4. translate the user's action into an explicit Illumination command,
5. receive the next published interaction state/result,
6. continue until the foreign workflow completes or the user exits.

### Boundary

Wiiii Got This must not decide Illumination scheduling, answer assessment semantics, or learning-state transitions.

## 13. Use Case: Explain Unavailability

### Goal

Tell the user why a Capability cannot currently be used.

### Flow

1. request current Capability Resolution Result,
2. expose user-appropriate reason,
3. optionally expose remediation action if one is legitimately known,
4. preserve technical diagnostics separately from user-facing explanation.

Examples:

- integration disabled,
- provider offline,
- incompatible version,
- unsupported on this Device/Platform,
- required synchronized state unavailable.

## 14. Use Case: Refresh Availability

### Goal

Reevaluate volatile Service/Capability state.

### Flow

1. refresh required runtime observations,
2. avoid unnecessary foreign domain reads,
3. derive new availability,
4. update read models,
5. notify presentation layer of meaningful change where the client architecture supports it.

## 15. Use Case: List Service Integrations

### Goal

Show the user's known integrations and their effective status.

### Read information may include

- Service identity/name,
- global enablement,
- current Device override,
- effective enablement,
- registration/discovery status,
- compatibility summary,
- available/unavailable capability counts,
- last observation/refresh information.

## 16. Use Case: Inspect Service Integration

### Goal

Inspect one integration without exposing foreign internals.

### Read information may include

- published Service metadata,
- known Capabilities,
- version/compatibility status,
- current Device-specific availability,
- configured enablement,
- unavailable reasons,
- supported presentation paths.

## 17. Use Case: Reconcile Published Capability Changes

### Goal

Handle a provider that adds, removes, or deprecates Capabilities.

### Flow

1. refresh Service publication,
2. compare by stable Capability Identity,
3. add newly published descriptors,
4. mark/remove disappeared descriptors according to contract semantics,
5. preserve Wiiii Got This-owned integration configuration where still applicable,
6. invalidate obsolete availability/presentation resolutions,
7. do not recreate removed capabilities from stale cached metadata.

## 18. Use Case: Use Replicated/Offline Capability State

### Status

Directional only; not yet implementable.

The future application flow must:

1. verify that the Capability explicitly supports local/replicated operation,
2. verify that suitable replicated state exists,
3. invoke only service-approved offline semantics,
4. retain ownership/version/correlation metadata,
5. later reconcile using service-owned merge/conflict rules.

The application layer must not implement generic merge rules for foreign data.

## 19. Application Ports – Directional

Likely ports include conceptual responsibilities such as:

- Service Registration Repository,
- Service Discovery Adapter,
- Service Publication Reader,
- Device Repository / Device Context Provider,
- Runtime Reachability/Health Observer,
- Foreign Capability Adapter,
- Presentation Capability Provider,
- optional future Replication Adapter,
- Clock.

These are responsibility names, not mandated interfaces or classes.

## 20. Transaction Boundaries

Wiiii Got This-owned configuration changes should be atomic at their own boundary.

Examples:

- changing global enablement,
- setting/clearing one Device override,
- replacing one validated Service publication snapshot.

Foreign commands use the foreign Service's transactional semantics.

Wiiii Got This must not attempt cross-context distributed transactions over foreign domain state.

## 21. Error Semantics

Application errors should distinguish at least:

- invalid configuration request,
- unknown Service,
- unknown Device,
- unsupported contract,
- incompatible contract,
- provider unreachable,
- Capability unsupported,
- missing prerequisite,
- foreign operation rejected,
- malformed provider response,
- unexpected infrastructure failure.

The final error-code scheme belongs to contracts/architecture.

## 22. Security Boundary

No discovery or invocation adapter may assume that discovered equals trusted.

Authentication/authorization/trust enforcement must be introduced explicitly when remote or multi-device architecture requires it.

The current application design reserves the boundary but does not select the security mechanism.

## 23. No Cross-Context Orchestration By Default

Wiiii Got This must not automatically orchestrate workflows between Vocation and Illumination merely because both are integrated.

A future cross-service orchestration requires:

- explicit user scenario,
- ownership decision,
- published contracts,
- failure semantics,
- context-map update.

## 24. Implementation Independence

These use cases can later be exposed through:

- native mobile UI,
- native desktop UI,
- web UI,
- local service API,
- remote service API,
- other adapters.

The application semantics must not depend on one presentation framework.
