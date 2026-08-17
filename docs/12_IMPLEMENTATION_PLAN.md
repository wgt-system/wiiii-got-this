# Wiiii Got This – Implementation Plan

## Status

Historical planning baseline plus current execution/release direction.

Released baselines through **v0.5.0** established WGT Core, registration/publication lifecycle, the first Vocation integration, and the application-grade Desktop shell. Branch `dev` now contains the **v0.6.0 Windows/Desktop release candidate**, including the second Vocation capability (`Published Map Projection 1.0`), Orientation-backed map presentation, and the Desktop UX/product-quality pass.

Real Apple runtime/provider acceptance remains deferred and is not part of the Windows v0.6.0 release claim.

The milestone shape below preserves the original planning sequence for context; it is not a statement that repository implementation has not started.

## 1. Release Model

Use semantic-style product milestones:

```text
vMAJOR.MINOR.PATCH
```

The exact pre-1.0 milestone count is not predetermined.

`v1.0.0` should represent a coherent first stable Wiiii Got This product baseline rather than merely the first runnable shell.

### GitHub Milestones and Issues

Milestones are named only `v0.1.0`, `v0.2.0`, `v0.3.0`, … without descriptive suffixes. GitHub Issues are the durable concrete work packages within a milestone; avoid issue fragmentation merely to imitate process.

Milestone scope, Issue scope, order, dependencies and release decisions belong to the Control Plane. Implementation workers/agents do not autonomously create, split, extend, close or reorder milestone scope.

## 2. Pre-Implementation Gates

Before creating implementation issues:

### Product/domain gate

- Domain Vision accepted,
- Scenarios coherent,
- Ubiquitous Language coherent,
- Subdomains/context hypotheses explicit,
- Domain Model coherent,
- Context Map coherent,
- Application Design coherent.

### Architecture gate

Explicitly decide:

- first presentation/client platforms,
- programming language/runtime,
- UI framework/client strategy,
- WGT persistence,
- initial process/deployment topology,
- first Service publication/transport,
- first registration/discovery mechanism,
- initial Presentation Contribution strategy.

### Contract gate

For each first concrete foreign integration:

- concrete consumer/provider scenario,
- published contract semantics,
- versioning rule,
- errors,
- contract tests.

## 3. Historical Milestone Shape

The following labels record the original pre-bootstrap planning sequence.

### Milestone A – Wiiii Got This Core

Goal:

- project/solution foundation,
- WGT-owned persistence,
- Device baseline,
- Service Integration aggregate,
- global enablement,
- Device overrides,
- Capability descriptors,
- deterministic Capability Resolution,
- Availability/Unavailable Reason,
- tests independent of real providers.

Use a fake/reference provider for generic integration behavior.

### Milestone B – Registration / Discovery Baseline

Goal:

- first concrete registration/discovery mechanism,
- Service publication refresh,
- stable Service Identity,
- Capability change/deprecation handling,
- diagnostics,
- contract/version compatibility.

Avoid speculative multi-protocol discovery.

### Milestone C – WGT Presentation Shell

Goal:

- primary selected client,
- Service Integration management,
- Capability navigation/catalog,
- availability/degraded states,
- generic presentation routing,
- no foreign business-domain duplication.

### Milestone D – First Real Provider Integration

Select real providers by **actual Published/Application Contract readiness**, not a fixed project preference.

Goal:

- narrow versioned provider Capability,
- adapter and contract tests,
- WGT-owned product presentation/composition,
- provider failure/version behavior,
- no foreign database/domain-class access.

### Milestone E – Interactive Illumination Integration

Goal when Illumination publishes the required consumer-ready boundary:

- narrowly selected Illumination study Capability,
- versioned read/command interaction contract,
- WGT-integrated presentation,
- Illumination-owned workflow transitions,
- failure and compatibility handling.

Do not invent the contract in WGT merely to start integration work.

### Milestone F – Multi-Device / Offline Architecture

Only when concrete provider requirements are ready.

Goal may include:

- synchronization/replication contract decision,
- trust/security model,
- service-owned replication semantics,
- selected offline capability,
- use of accepted Conveyance delivery modes where appropriate.

Do not implement a competing generic relay/sync owner.

### Milestone G – Additional Presentation Surfaces

Add additional platform/runtime surfaces only when concrete use cases and validation environments justify them.

## 4. Reference Provider Strategy

The Reference Integration remains a minimal development/diagnostic provider for generic integration behavior.

It exercises:

- Service Identity,
- publication version,
- several Capabilities,
- available/unavailable states,
- version incompatibility,
- presentation invocation.

It must not become a hidden sample business domain or a normal end-user product area.

## 5. Real Provider Integration Selection

Vocation Published Opportunity Overview 1.0 became the first real Windows integration because it was the first accepted, versioned, consumer-ready Published Contract.

Vocation Published Map Projection 1.0 is now the second consumed Vocation capability on Desktop. WGT consumes its provider-owned semantics and adapts the accepted spatial data into the separate Orientation renderer boundary.

Illumination remains deferred until it owns and publishes the concrete WGT interaction contract needed by an actual product slice.

Each integration must preserve:

- Service publication,
- registration/discovery,
- versioning,
- Availability,
- strict boundary adaptation,
- provider failure isolation,
- provider domain ownership.

## 6. Illumination Integration Order

Illumination is a stronger architecture test because it may require interactive commands, repeated state transitions, eventual offline operation and domain-specific synchronization/reconciliation.

Do not solve speculative Illumination offline requirements inside WGT Core. Wait for Illumination-owned application/published semantics.

## 7. Synchronization Work

Synchronization is not a generic WGT infrastructure task.

Before implementation:

1. select one concrete service/capability,
2. define state that must exist on the target Device,
3. define authority,
4. define commands while disconnected,
5. define conflicts,
6. define merge/reconciliation,
7. define sensitivity/encryption,
8. determine whether an accepted Conveyance delivery mode satisfies the transport requirement; if not, return the generic requirement to the System Architecture Control Plane.

Generic durable delivery ownership is assigned to Conveyance.

## 8. Orientation Integration

Orientation is the accepted system owner of generic geospatial capability. WGT must not build or retain a competing generic map renderer.

Current Desktop path:

```text
Vocation Published Map Projection 1.0
    ↓
WGT Vocation consumer/application seam
    ↓
WGT presentation adapter
    ↓
Orientation Host Bridge 1.0 / Spatial Scene
    ↓
packaged Orientation map surface in NativeWebView/WebView2
```

Ownership remains:

- Vocation: Work Location, Precision, opportunity/job meaning and publication semantics;
- Orientation: generic map/geospatial rendering and interaction;
- WGT: product navigation, composition, host integration and WGT presentation around the surface.

The exact packaged Orientation consumer artifact is pinned and must be refreshed intentionally through a tested artifact update rather than silently following Orientation `dev` or the latest release.

Apple/phone host work is a separate deferred platform track and does not block Windows/Desktop releases.

## 9. Execution Model

The Control Plane owns architecture, issue/milestone scope, repository review and release decisions.

Prefer direct repository/GitHub execution when the available tooling can safely perform and validate the work. Delegate to an implementation worker/agent only when a required task cannot reasonably be completed through the Control Plane tooling itself.

When delegation is necessary, the worker receives a bounded implementation task with known repository/branch/HEAD, outcome, ownership constraints and validation requirements. The worker does not reinterpret system architecture or autonomously plan milestones.

Local machine installations or workstation configuration remain explicit user-controlled actions.

## 10. Branch / Release Direction

Use:

- `main` for stable milestone releases,
- `dev` for ongoing integrated development,
- narrow feature branches/PRs where useful for risky or reviewable changes.

Release workflow:

1. validate the release candidate on `dev`;
2. obtain explicit Control Plane release approval;
3. advance `main` by a clean fast-forward to the approved release commit;
4. create the immutable version tag on that exact commit;
5. publish the corresponding GitHub Release;
6. close the release Issue and milestone only after validation and publication succeed.

`main` prevents force pushes/deletion while permitting intentional direct fast-forward release advancement. `dev` remains the active development branch.

Apple runtime support is a separate claim: absence of Mac/Xcode/physical-iPhone evidence blocks Apple-runtime acceptance, **not** a Windows/Desktop-only milestone release.

## 11. Done Criteria Per Slice

Each implementation slice should satisfy:

- relevant specification/issue named,
- ownership boundary preserved,
- tests green,
- no silent public-contract changes,
- no cross-context persistence access,
- migration/compatibility behavior tested where applicable,
- ADR updated when architecture changes,
- unrelated Service failure remains isolated where relevant.

Desktop product slices should additionally preserve coherent keyboard/focus/recovery behavior and avoid exposing protocol/diagnostic concepts as primary user interaction unless required.

## 12. Current Direction After v0.6.0

Do not predeclare the next milestone solely from historical planning.

After v0.6.0 release, the Control Plane should scan current WGT/system/provider readiness and choose the next slice from actual available capabilities and product value. In particular:

- do not resume iOS merely because a placeholder host exists;
- do not invent an Illumination contract;
- do not duplicate Orientation capability;
- do not create new generic infrastructure where Architecture already assigns ownership.

## 13. Repository Bootstrap

See `docs/21_REPOSITORY_BOOTSTRAP.md` for the implementation-ready repository bootstrap specification.
