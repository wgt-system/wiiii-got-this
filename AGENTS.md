# AGENTS.md

## Project name

The canonical project name is **Wiiii Got This**.

Do not introduce or use another project name or historical alias.

Technical repository, package, or executable names may use an appropriate normalized form where required, but they do not replace the canonical product name.

## Purpose

This repository contains Wiiii Got This, a cross-platform application and bounded context for integrating independently developed applications and services across the user's devices and platforms.

Wiiii Got This is not merely a launcher.

It has its own domain responsibilities around device-, platform-, service-, capability-, availability-, integration-, and presentation-related concerns, subject to refinement through the domain specification.

Wiiii Got This is an accepted bounded context within the wider `wgt-system`; its own domain boundary and local architecture remain authoritative here. System-wide ownership and cross-context policy are maintained by `wgt-system/architecture`.

Do not force a responsibility into Wiiii Got This merely because it is required by more than one application.

## Source of truth

Repository documentation is the durable source of truth.

Before proposing or implementing changes:

1. read `README.md`,
2. read the relevant files in `docs/`,
3. identify accepted decisions and explicitly unresolved questions,
4. inspect relevant published contracts and ADRs,
5. do not infer product behavior from implementation convenience,
6. do not infer domain ownership from deployment topology.

Chat history is not the durable architectural source of truth once decisions have been recorded in the repository.

`docs/model/workspace.dsl` is a derived service-local visualization, not normative architecture authority. If it conflicts with accepted WGT documentation/ADRs, treat the model as drift; system-wide conflicts must be returned to `wgt-system/architecture`.

## WGT System Architecture

The system-level architecture source of truth is `wgt-system/architecture`.

Before introducing or changing cross-context integration, synchronization/replication, generic relay or storage infrastructure, service discovery/registry infrastructure, shared cross-context infrastructure, or another system-wide capability, consult its `CAPABILITY_CATALOG.md`, `ARCHITECTURE_PRINCIPLES.md`, and `INTEGRATION_POLICY.md`.

Generic durable opaque cross-device delivery is owned by Conveyance. Conveyance does not own WGT, Vocation, or Illumination business semantics; domain-specific publication, commands, authority, merge, conflict, and reconciliation remain with the affected domain owner. If an existing generic capability is conceptually correct but insufficient, return the concrete requirement to the System Architecture Control Plane instead of creating a competing subsystem. WGT runtime code must not depend on the Architecture Repository.

## Product boundaries

Current candidate Wiiii Got This responsibilities include:

- devices,
- platforms and execution environments,
- service identity,
- service registration and discovery,
- service integration,
- capabilities,
- service and capability availability,
- integration configuration,
- device- and platform-dependent presentation,
- navigation and invocation of published capabilities.

These terms are part of the current design space.

They are not automatically:

- entities,
- aggregates,
- application services,
- network services,
- repositories,
- independently deployable components.

The specification must determine their actual role.

## Foreign bounded contexts

Wiiii Got This does not own the business domains of the applications and services it integrates.

### Vocation

Vocation owns its personal job-market domain, including its opportunities, postings, research, assessments, decisions, job-market workflows, and domain-specific projections.

Wiiii Got This must not recreate Vocation domain entities or business rules.

### Illumination

Illumination owns its personal learning domain, including learning content, learning interactions, review history, repetition and scheduling state, and learning progress.

Wiiii Got This must not recreate Illumination domain entities or business rules.

The same ownership rule applies to future bounded contexts.

## Integration rules

Independent bounded contexts communicate through explicit published contracts.

Do not introduce:

- shared databases between bounded contexts,
- direct access to foreign tables or persistence models,
- shared domain entities,
- direct imports of foreign domain classes,
- shared business-logic libraries that bypass published boundaries,
- dependencies on another context's internal repository structure,
- accidental serialization of internal domain models as public contracts.

Valid integration patterns may include:

- Open Host Services,
- Published Languages,
- versioned read contracts,
- versioned command contracts,
- capability contracts,
- explicit adapters,
- Anticorruption Layers,
- explicitly designed presentation contributions.

The exact mechanisms must follow concrete scenarios and documented architecture decisions.

## Integrated presentation

Wiiii Got This may present capabilities from independent services so that they appear as coherent parts of the Wiiii Got This user experience.

Integrated presentation does not transfer domain ownership to Wiiii Got This.

Do not assume that:

- a Service is a Plugin,
- a Plugin or Integration Module is a Capability,
- a Capability is a Presentation Contribution,
- presentation integration requires shared domain code,
- a service must provide its own UI,
- Wiiii Got This must implement every foreign workflow itself.

The concrete presentation mechanism remains an architecture decision until explicitly accepted.

Opening a separate application is not the intended default integration model, but it is not permanently prohibited.

## Context-boundary discipline

WGT's local boundary must still be challenged during specification, but generic delivery ownership is already assigned to Conveyance at system level.

Potential concerns such as:

- domain-specific synchronization or replication semantics,
- identity and authentication,
- service registry,
- shared map composition,
- notifications,
- remote connectivity,
- backup or storage infrastructure,

must not automatically become Wiiii Got This subdomains.

For each substantial responsibility, determine whether it is:

1. part of the Wiiii Got This domain,
2. an internal technical component,
3. generic infrastructure owned elsewhere,
4. a separate bounded context,
5. owned by another existing service.

Create a new bounded context only when the domain and ownership boundary justify it.

Do not create new network services merely to obtain a "microservice architecture."

## WGT deployment discipline

DDD and ownership boundaries come before WGT process, repository, container, and network boundaries. The system-wide principle is maintained in `wgt-system/architecture/ARCHITECTURE_PRINCIPLES.md`; the following rules are WGT-specific consequences.

Do not assume:

- one bounded context equals one HTTP service,
- one aggregate equals one microservice,
- one domain noun equals one service,
- every independently deployable component needs a separate repository,
- every service requires Docker,
- local execution excludes server infrastructure,
- server infrastructure implies remote authoritative persistence.

Docker, containers, remote services, personal servers, hosted infrastructure, and local processes are all permitted when justified by the architecture.

Do not introduce distributed-system complexity without a concrete requirement.

## Data locality and sensitivity

There is no global Wiiii Got This rule that all data must be local or that all data must be remote.

Different bounded contexts may define different locality, confidentiality, replication, and synchronization requirements.

A foreign bounded context remains responsible for the meaning and authority of its domain data.

Wiiii Got This must not silently decide that foreign data may be:

- uploaded,
- replicated,
- cached,
- stored remotely,
- decrypted remotely,
- retained,
- shared between devices.

Such behavior requires explicit published semantics and architecture decisions.

Sensitive information must not become remotely persisted merely because remote infrastructure is technically available.

## Platform discipline

Wiiii Got This is a product and bounded context, not one specific UI executable.

It may eventually have multiple presentation adapters such as:

- mobile,
- desktop,
- web,
- other device-specific clients.

Do not assume that every platform supports every capability.

Do not equate `Platform` with an operating-system string until the domain specification defines the concept.

Device, operating environment, runtime environment, presentation platform, and physical hardware may represent different concepts and must not be collapsed prematurely.

## Independent service operation

Vocation, Illumination, and other independent bounded contexts must not require Wiiii Got This for their domain correctness or authoritative state unless a future explicit product decision changes that relationship.

Wiiii Got This may provide additional platform reach, presentation, discovery, synchronization, or integration without absorbing the service's domain.

Failure or absence of one integrated service must not make unrelated Wiiii Got This capabilities unusable.

Exact degradation and availability semantics belong to the specification.

## Shared Map

A future Shared Map bounded context is a design hypothesis, not an accepted implementation.

Do not add a generic map domain to Wiiii Got This merely because several services may have spatial presentation needs.

If a Shared Map context is introduced:

- contributing services retain ownership of the meaning of their spatial data,
- contributions use explicit published contracts,
- Shared Map does not read foreign databases,
- Wiiii Got This may integrate the resulting presentation according to platform and device.

## Architecture decisions

Do not invent:

- a technology stack before architecture requirements justify it,
- a client framework before target presentation requirements are accepted,
- a service transport before communication semantics are known,
- a synchronization protocol before consistency and ownership requirements are defined,
- a plugin mechanism before capability and presentation semantics are understood,
- a remote persistence model because a server exists,
- a local-only architecture because some data is sensitive.

Significant accepted architecture decisions must be recorded as ADRs.

Programming language, runtime, UI framework, transport, persistence, packaging, containerization, and deployment choices must remain explicitly reviewable until accepted through the architecture/ADR process.

## Specification gate

Implementation must not begin before the first coherent specification and architecture baseline exists.

The initial specification set includes:

- `README.md`
- `AGENTS.md`
- `docs/01_DOMAIN_VISION.md`
- `docs/02_SCENARIOS.md`
- `docs/03_UBIQUITOUS_LANGUAGE.md`
- `docs/04_SUBDOMAINS.md`
- `docs/05_DOMAIN_MODEL.md`
- `docs/06_CONTEXT_MAP.md`
- `docs/07_APPLICATION_DESIGN.md`
- `docs/08_PUBLISHED_CONTRACTS.md`
- `docs/09_READ_MODELS.md`
- `docs/10_ARCHITECTURE.md`
- `docs/11_ACCEPTANCE_TESTS.md`
- `docs/12_IMPLEMENTATION_PLAN.md`
- `docs/adr/`

Published contracts must only be authored when concrete scenarios establish their required semantics.

Do not create speculative APIs merely to reserve future integration points.

## Agent behavior

When a product or architecture decision is genuinely missing:

- do not guess,
- identify the unresolved decision explicitly,
- preserve materially different alternatives where necessary,
- continue independent specification work where possible,
- stop only when further progress requires choosing between materially different product semantics.

Do not silently convert a design hypothesis into an accepted decision.

When repository documentation conflicts:

- identify the conflict,
- determine which decision is newer or explicitly authoritative where possible,
- do not choose based solely on implementation convenience.

## Agent Delegation Policy

Implementation agents execute the narrowly scoped task assigned by the control-plane chat.

Do not spawn, delegate to, or invoke additional subagents unless the current task explicitly authorizes it.

In particular, do not independently create or use:

* explorer subagents,
* implementation subagents,
* reviewer subagents,
* parallel Luna/Codex workers,
* background agent tasks.

Perform repository inspection, implementation, testing, and reporting directly within the current agent session.

The control-plane chat owns:

* task decomposition,
* architecture decisions,
* sequencing,
* parallelization decisions,
* cross-area coordination.

If the assigned task is too broad or requires an unresolved architectural decision, report the boundary instead of delegating the problem to another agent.

## GitHub Milestones and Issues

* Milestones heißen ausschließlich `v0.1.0`, `v0.2.0`, `v0.3.0`, … ohne beschreibenden Zusatz.
* GitHub Issues sind die dauerhaften konkreten Arbeitspakete innerhalb eines Milestones.
* keine unnötige Issue-Zerlegung für Kleinständerungen.
* Luna-Chatnamen sind nur Ausführungskontexte und ersetzen Milestones/Issues nicht.
* Milestone-Scope, Issue-Scope, Reihenfolge und Parallelisierung werden vom Control-Plane-Chat festgelegt.
* Implementation Agents erzeugen oder erweitern Milestones/Issues nicht eigenmächtig.

## Codex / Luna workflow

Codex or Luna implementation work begins only after the relevant specification, contracts, architecture decisions, and acceptance criteria are stable enough to implement without inventing product behavior.

Before implementation:

1. read the relevant specification and ADRs,
2. inspect surrounding bounded-context contracts,
3. identify contradictions and blockers,
4. state the intended vertical implementation slice,
5. identify affected contracts and ownership boundaries,
6. name the required tests and acceptance criteria.

During implementation:

- do not introduce silent schema or contract changes,
- do not cross bounded-context persistence boundaries,
- do not create speculative abstractions for hypothetical future services,
- do not expand task scope merely to generalize architecture.

After implementation:

1. run relevant tests and repository checks,
2. report documentation or contract deviations,
3. update ADRs when an architecture decision changed,
4. preserve independent bounded-context operation,
5. verify that no hidden coupling was introduced.

Parallelization is appropriate only for genuinely independent work with low overlap in files, contracts, and domain decisions.


## Explicit Deferred-Decision Gate

Before implementing a mechanism not already covered by an accepted ADR, inspect:

- `docs/adr/README.md`
- `docs/22_DEFERRED_DECISIONS.md`

Implementation agents must not silently decide:

- production Sync/Relay cryptography,
- foreign Service merge/conflict semantics,
- any new or unaccepted Vocation/Illumination contract shape (already frozen provider
  contracts, such as Vocation Published Opportunity Overview 1.0 and Published Map
  Projection 1.0, may be consumed according to the provider repositories),
- universal Capability taxonomy,
- universal requirement/UI schema,
- generic Service Registry,
- user-account/auth architecture,
- web-client requirement.

Use fake/reference seams until the relevant control-plane decision is accepted.


## Foreign Context Alignment

Before implementing real Vocation or Illumination integration, read:

- `docs/23_FOREIGN_CONTEXT_ALIGNMENT.md`
- the current owning provider repository/branch,
- the provider's accepted integration ADRs and Published Contracts.

Do not implement a WGT-side foreign contract from an earlier speculative WGT document if the provider repository has since accepted a different boundary.

## Local Worktrees

Canonical local worktrees:

P:\wgt-system\wiiii-got-this
→ main
→ stable milestone/release state
P:\wgt-system\wiiii-got-this\.worktrees\dev
→ dev
→ active development

Rules:

* implementation agents work in P:\wgt-system\wiiii-got-this\.worktrees\dev unless explicitly instructed otherwise;
* P:\wgt-system\wiiii-got-this is not used for ordinary feature implementation;
* release integration to main happens only on explicit Control-Plane instruction;
* agents must verify repository root and branch before modifying files.
