# Wiiii Got This – Published and Integration Contracts

## 1. Purpose

This document defines the semantic requirements for contracts at the Wiiii Got This boundary.

It intentionally does not yet define concrete JSON Schema, OpenAPI, Protobuf, IPC, package, or programming-language types.

Concrete schemas must be created only after the relevant capability scenario is selected for implementation.

## 2. Contract Principles

All cross-context contracts must:

- be explicit,
- be versioned,
- expose only required semantics,
- remain independent of foreign persistence schemas,
- remain independent of internal domain classes,
- support independent service evolution,
- have executable contract tests once implemented.

A public contract version is not the same thing as:

- application release version,
- database migration version,
- internal domain-model revision.

## 3. Contract Families

The current model expects several distinct contract families rather than one giant Service DTO.

### 3.1 Service Publication Contract

Purpose:

Describe the Service and the Capabilities it intentionally publishes to Wiiii Got This.

Conceptual content:

- Service Identity,
- publication contract version,
- human-readable metadata intended for consumers,
- Capability descriptors,
- capability-specific contract/version references,
- supported presentation/invocation contributions,
- deprecation information,
- requirements relevant to integration.

Must not contain:

- internal database metadata,
- arbitrary internal configuration,
- internal domain object graphs.

### 3.2 Capability Read Contract

Purpose:

Expose bounded read data required by one Capability.

Examples:

- Vocation opportunity summary/read model,
- Illumination study-session/read state.

Each Capability may define its own published language.

Wiiii Got This must not force all services into one generic business-data schema.

### 3.3 Capability Command Contract

Purpose:

Invoke a service-owned behavior without moving the rule into Wiiii Got This.

Examples may include future Illumination actions such as recording a review-step action.

The provider owns:

- validation,
- business rules,
- resulting authoritative state.

Wiiii Got This owns:

- invocation eligibility,
- transport/adaptation,
- platform presentation.

### 3.4 Presentation Contribution / Metadata Contract

Purpose:

Expose bounded presentation metadata and supported invocation information required by a WGT Integration Adapter.

V1 does not use arbitrary service-supplied executable UI code.

Executable presentation is delivered with Wiiii Got This through WGT-native Avalonia views.

A Service may publish bounded metadata such as:

- title/description,
- semantic labels,
- supported actions,
- content payloads,
- status,
- presentation requirements.

A future declarative UI contract is deferred until repeated integrations establish stable common primitives.

### 3.5 Registration / Discovery Contract

Purpose:

Make Service publication information locatable/refreshable.

The exact contract depends on whether registration/discovery remains internal to Wiiii Got This or moves to a separate registry context.

Do not finalize this schema before that boundary is resolved.

### 3.6 Replication / Synchronization Contract

Purpose:

Allow service-owned state to be transferred between Devices or runtimes where a Capability supports offline/replicated operation.

The generic Synchronization / Relay context boundary is accepted, but the first concrete Service synchronization contract is intentionally not yet defined.

It must be authored from a real synchronized Service flow and must specify:

- service-owned merge/conflict semantics,
- identity/correlation model,
- offline command behavior where relevant,
- locality/sensitivity policy,
- encryption/key requirements.

The first likely driver is Illumination after its architecture review establishes the required replicated learning semantics.

## 4. Identity Rules

### Service Identity

Must be stable across:

- endpoint changes,
- process restarts,
- container redeployments,
- UI renames.

It must not be derived solely from network location.

### Capability Identity

Must be stable across:

- UI label changes,
- route changes,
- internal implementation changes.

Capability Identity is scoped by or otherwise associated with Service Identity.

### Device Identity

A Device identity will be required by Device-specific integration overrides and possibly future synchronization.

Its exact lifecycle is not yet fixed.

No schema should freeze Device identity semantics prematurely.

## 5. Versioning Requirements

The following require explicit version semantics once implemented:

- Service Publication Contract,
- each Capability Read Contract,
- each Capability Command Contract,
- Presentation Contribution Contract where provider/consumer compatibility depends on it,
- Registration/Discovery Contract when cross-process/context,
- future Replication/Sync Contracts,
- future Shared Map Contribution Contract.

## 6. Compatibility

A consumer must be able to determine whether it supports the semantics of a published contract before invoking it.

The versioning strategy may use:

- major/minor semantics,
- discrete named contract versions,
- negotiated feature sets,
- another explicit approach.

No strategy is selected yet.

The required property is deterministic compatibility behavior.

## 7. Backward Compatibility

A provider should be able to evolve independently without forcing immediate simultaneous upgrade of all Wiiii Got This clients where practical.

Potential methods include:

- serving more than one contract version,
- additive compatible fields,
- adapter translation,
- controlled deprecation windows.

The exact policy belongs to architecture and service contracts.

## 8. Error Contract

Cross-context calls must distinguish domain/integration outcomes sufficiently for Wiiii Got This to avoid generic failure handling.

Conceptually relevant categories include:

- unsupported contract version,
- malformed request,
- unauthorized/untrusted request,
- unavailable provider/dependency,
- invalid foreign-domain command,
- stale/conflicting state where applicable,
- deprecated/removed capability,
- unexpected provider failure.

Transport status codes alone must not become the domain language.

## 9. Availability Contract Semantics

Providers may publish facts that contribute to availability.

Wiiii Got This must still own the final contextual interpretation for its current Device/Platform.

For example:

```text
Provider says:
- capability exists
- provider currently healthy
- requires live provider

WGT adds:
- integration enabled?
- compatible contract?
- current Device supported?
- presentation available?
```

A provider must not dictate Wiiii Got This global/device enablement.

## 10. Data-Minimization Rule

Contracts should expose only what the consumer scenario needs.

Private Vocation application material is not public Published Contract data. CVs, cover letters, and personal application documents must remain outside public contracts, fixtures, examples, logs, repository artifacts, and other publicly exposed surfaces. Any future cross-device contract must preserve the private end-to-end trust boundary described in ADR-0010.

Examples:

- a Vocation overview capability should not expose full internal research history unless needed,
- an Illumination study capability should not expose arbitrary Review history merely because it exists.

This reduces coupling and sensitivity exposure.

## 11. Foreign DTO Rule

Generated or handwritten contract DTOs are boundary types.

They must not be used as internal domain entities by either side merely for convenience.

Adapters map between:

```text
provider domain
↔ provider boundary DTO
↔ transport
↔ Wiiii Got This adapter/read model
```

## 12. Contract Testing

Every implemented published contract should have tests covering:

- valid examples,
- rejected malformed/unsupported inputs,
- compatibility behavior,
- required identity fields,
- deprecation behavior where supported,
- consumer/provider fixtures or snapshots as appropriate.

Contract tests do not replace provider domain tests.

## 13. Vocation Contract Direction

The first likely low-risk integration is a read-only Vocation capability.

Potential initial contract pressure:

- Service Publication,
- Opportunity Overview Read Model,
- Opportunity Detail Read Model,
- explicit external-navigation metadata where later required.

No concrete schema is created here because Vocation's actual first WGT integration milestone has not yet been selected.

## 14. Illumination Contract Direction

Illumination will likely require both read and command semantics for an interactive study workflow.

Potential contract pressure:

- current study interaction state,
- reveal/assist action,
- learner response/action,
- assessment submission,
- next interaction/result,
- later offline/replication support.

Wiiii Got This must not infer Illumination's command model before Illumination publishes it.

## 15. Shared Map Contract Direction

If Shared Map is introduced, source services publish a Map Contribution / Projection through a versioned contract.

Shared Map consumes that contract.

Wiiii Got This should consume the Shared Map capability rather than directly reading source-service spatial internals.

## 16. Contract Gate

Do not author a concrete public schema merely because a future integration is imaginable.

A concrete contract requires:

1. a named consumer,
2. a concrete user/application scenario,
3. decided ownership,
4. required semantics,
5. identity/cardinality decisions,
6. error semantics,
7. versioning expectations.

Until then, retain only semantic requirements.
