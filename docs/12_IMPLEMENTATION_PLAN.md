# Wiiii Got This – Implementation Plan

## Status

Historical planning baseline through domain/application architecture. The implementation-ready technology and architecture decisions were accepted before repository bootstrap.

The v0.1.0 baseline, the v0.2.0 registration/publication lifecycle baseline, and the Windows-first Vocation Published Opportunity Overview 1.0 integration are released as v0.3.0. Production synchronization and the real iOS runtime smoke remain outstanding gates.

The milestone shape below preserves the original planning sequence for context; it is not a statement that repository implementation has not started.

## 1. Release Model

Use semantic-style product milestones:

```text
vMAJOR.MINOR.PATCH
```

The exact pre-1.0 milestone count is not predetermined.

`v1.0.0` should represent a coherent first stable Wiiii Got This product baseline rather than merely the first runnable shell.

### GitHub Milestones and Issues

Milestones heißen ausschließlich `v0.1.0`, `v0.2.0`, `v0.3.0`, … ohne beschreibenden Zusatz. GitHub Issues sind die dauerhaften konkreten Arbeitspakete innerhalb eines Milestones; unnötige Issue-Zerlegung für Kleinständerungen ist zu vermeiden. Luna-Chatnamen sind nur Ausführungskontexte und ersetzen Milestones/Issues nicht. Milestone-Scope, Issue-Scope, Reihenfolge und Parallelisierung werden vom Control-Plane-Chat festgelegt. Implementation Agents erzeugen oder erweitern Milestones/Issues nicht eigenmächtig.

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

For the first implemented foreign integration:

- concrete consumer/provider scenario,
- published contract semantics,
- versioning rule,
- errors,
- contract tests.

## 3. Proposed Milestone Shape

The version numbers remain provisional until architecture selection.

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
- tests independent of real Vocation/Illumination.

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

Select the first provider by **actual Published Contract readiness**, not by a fixed project preference.

Vocation is likely lower-risk when its Mobile/WGT Read Contract is ready.

Illumination may be selected first if its WGT interaction contract becomes provider-ready earlier.

Goal:

- one narrow versioned provider Capability,
- adapter and contract tests,
- integrated WGT-native presentation,
- provider failure/version behavior,
- no foreign database/domain-class access.

### Milestone E – Interactive Illumination Integration

Goal:

- one narrowly selected Illumination study Capability,
- versioned read/command interaction contract,
- WGT-integrated presentation,
- Illumination-owned workflow transitions,
- failure and compatibility handling.

Initial version may require a live provider if offline semantics are not yet designed.

### Milestone F – Multi-Device / Offline Architecture

Only when concrete service requirements are ready.

Goal may include:

- synchronization/replication context decision,
- trust/security model,
- service-owned replication contracts,
- encrypted transport/relay,
- selected offline capability.

Do not implement generic sync before one real service defines state and merge semantics.

### Milestone G – Additional Presentation Surfaces

Add desktop/web/other clients only when concrete use cases justify them.

The architecture should allow this, but V1 does not need every possible presentation surface.

## 4. Reference Provider Strategy

Before coupling WGT Core to real services, implement a minimal fake/reference provider used only for development and contract tests.

It should exercise generic features such as:

- Service Identity,
- publication version,
- several Capabilities,
- available/unavailable states,
- version incompatibility,
- presentation contribution selection.

It must not become a hidden sample business domain inside WGT.

## 5. First Real Integration Selection

The first real provider integration is selected by **actual Published Contract readiness**, not by a permanently fixed provider order.

Vocation Published Opportunity Overview 1.0 is the implemented first Windows integration because its accepted WGT use is read-oriented. Its iPhone provider acceptance remains behind the real Apple runtime smoke gate.

Illumination has already accepted WGT as its primary Windows/iPhone presentation, but its concrete WGT interaction contract is also intentionally deferred until the relevant application capabilities are stable.

Whichever later provider first supplies an accepted, versioned, consumer-ready contract can become the next real integration.

That first integration should validate:

- Service publication,
- registration/discovery,
- versioning,
- Availability,
- boundary DTO/command adaptation,
- integrated WGT-native presentation,
- provider failure isolation.

WGT Core and the Reference Integration do not wait for either provider.

## 6. Illumination Integration Order

Illumination should follow once the generic integration model has been tested.

It is a stronger architecture test because it may require:

- interactive commands,
- repeated state transitions,
- eventual offline/mobile operation,
- synchronization/replication.

Do not solve all future Illumination offline requirements in the first WGT Core milestone.

## 7. Synchronization Work

Synchronization is not an early generic infrastructure task.

Before implementation:

1. select one concrete service/capability,
2. define state that must exist on the target Device,
3. define authority,
4. define commands while disconnected,
5. define conflicts,
6. define merge/reconciliation,
7. define sensitivity/encryption,
8. decide whether generic transport deserves a separate context.

## 8. Shared Map Work

Do not build Shared Map as part of WGT Core.

Create it only when at least two independent services have concrete spatial contribution requirements that justify cross-service composition.

## 9. Codex / Luna Use

Codex/Luna receives implementation work only after:

- relevant specification is accepted,
- technology baseline is accepted,
- file/module scope is known,
- contracts are stable enough,
- acceptance tests are named.

### Suitable Luna work

Potentially:

- focused contract fixtures,
- isolated adapter implementations with fixed interfaces,
- deterministic domain tests,
- UI components against stable read models,
- documentation consistency checks.

### Avoid Luna parallelization for unstable areas

Do not parallelize while semantics are moving:

- Device identity model,
- Capability contract envelope,
- availability semantics,
- sync/conflict model,
- first presentation-contribution mechanism.

## 10. Branch / Release Direction

After repository creation, use the same general control-plane discipline as Vocation/Illumination:

- `main` for stable milestone releases,
- `dev` for ongoing integrated development,
- narrow feature branches only where useful for parallel/risky changes,
- milestone release merge/tag after integration gate.

The exact Git workflow may be recorded in repository AGENTS/README when the repo is created.

## 11. Done Criteria Per Slice

Each implementation slice should satisfy:

- relevant specification named,
- ownership boundary preserved,
- tests green,
- no silent public-contract changes,
- no cross-context persistence access,
- migration/compatibility behavior tested where applicable,
- ADR updated when architecture changes,
- unrelated Service failure remains isolated where relevant.

## 12. Immediate Next Step

Current progression after the v0.2.0 registration/publication lifecycle baseline:

1. architecture alignment is accepted;
2. harden the implemented Vocation Published Opportunity Overview 1.0 Windows integration;
3. use Conveyance later to transport the same semantic contract for offline/cross-device iPhone read;
4. keep iPhone acceptance behind the real Apple runtime smoke gate;
5. generalize service discovery/presentation only after concrete integration pressure proves the required primitives.


## 14. Repository Bootstrap

See `docs/21_REPOSITORY_BOOTSTRAP.md` for the implementation-ready repository bootstrap specification.
