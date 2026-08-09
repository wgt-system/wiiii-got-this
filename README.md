# Wiiii Got This

Wiiii Got This is a cross-platform application and bounded context for integrating independently developed applications and services across the user's devices and platforms.

It is not merely a launcher.

Its purpose is to provide a coherent user experience across devices while preserving the independence and domain ownership of the services it integrates.

## Current project status

The repository contains the first implemented Wiiii Got This vertical baseline:

- Domain/Application baseline,
- WGT-owned SQLite persistence,
- Reference Integration,
- Avalonia Desktop/mobile presentation,
- Windows host,
- iOS host,
- successful `net10.0-ios` compilation on Windows,
- explicit known-integration registration,
- publication refresh observations with last-known publication retention,
- deterministic Capability snapshot reconciliation,
- per-integration publication refresh diagnostics.

Physical-device and simulator iOS runtime validation has not yet been performed.

## Product direction

Wiiii Got This should become the primary application through which the user accesses capabilities provided by independent services.

On a mobile device, the intended experience is that the user may need to install only Wiiii Got This while capabilities from services such as Vocation or Illumination appear as integrated parts of the Wiiii Got This experience.

The user should not normally need to think about which independent bounded context provides a particular capability.

This integrated presentation must not remove the architectural independence of the contributing services.

Service integrations should be enableable and disableable in a plugin-like manner.

Opening a separate application is not the intended default interaction model, but it is not prohibited as a permanent architectural rule.

## Wiiii Got This domain responsibility

Current candidate responsibilities include:

- devices,
- platforms and execution environments,
- service identity,
- service registration and discovery,
- service integration,
- capabilities exposed by services,
- service and capability availability,
- integration configuration,
- platform- and device-dependent capability presentation,
- navigation and invocation of published capabilities.

These are candidate concepts for the domain language.

They are not yet assumed to be entities, aggregates, network services, or independently deployable components.

## Independent bounded contexts

Wiiii Got This does not own the business domains of applications it integrates.

### Vocation

Vocation owns the personal job-market domain, including concepts such as:

- Job Opportunities,
- Postings,
- Companies,
- Research,
- Assessments,
- Decisions,
- job-market-specific projections and workflows.

### Illumination

Illumination owns the personal learning domain, including concepts such as:

- learning content,
- questions and tasks,
- reference solutions,
- reviews,
- scheduling,
- learning state,
- learning progress.

Wiiii Got This must not reproduce these concepts as its own domain model.

## Integration principles

Independent bounded contexts communicate only through explicit published contracts.

Forbidden coupling includes:

- shared databases between bounded contexts,
- direct access to foreign tables,
- cross-context imports of domain classes,
- shared domain entities,
- shared business-logic libraries used to bypass published boundaries,
- dependencies on another application's internal repository or persistence structure.

Expected integration mechanisms may include:

- Open Host Services,
- Published Languages,
- versioned read contracts,
- versioned command or capability contracts,
- explicit adapters and Anticorruption Layers,
- service-provided presentation contributions.

Provider-specific transport and deployment are allowed. The V1 host/composition and registration baselines are accepted; concrete foreign-Service contracts remain service-specific.

## Presentation

Wiiii Got This is a product and bounded context, not a single specific client application.

It may eventually expose multiple presentation adapters, for example:

- native mobile clients,
- native desktop clients,
- web clients,
- other future device-specific surfaces.

Not every client must support every capability.

The exact first supported platforms belong to implementation planning after the domain and architecture specification has stabilized.

## Local and remote operation

Wiiii Got This does not assume that all application data is stored remotely.

It also does not prohibit servers, containers, Docker, remote APIs, synchronization infrastructure, or hosted services.

Different bounded contexts and different classes of data may require different locality and confidentiality policies.

A service remains responsible for the meaning and ownership of its domain data.

Wiiii Got This may provide or coordinate device, connectivity, transport, replication, availability, and presentation mechanisms only where those responsibilities belong to its own domain or to explicitly separated integration contexts.

Sensitive data must not become remotely persisted merely because remote infrastructure exists.

## Microservice and bounded-context direction

DDD boundaries are determined before deployment boundaries.

A bounded context is not automatically a network microservice.

A domain concept is not automatically a separately deployed service.

The architecture should optimize for:

- clear ownership,
- independent development,
- independent testing,
- explicit contracts,
- controlled evolution,
- replaceable integration,
- fault isolation,
- maintainability.

It should not optimize for:

- the maximum number of repositories,
- the maximum number of containers,
- the maximum number of HTTP APIs,
- artificial distributed-system complexity.

The current Wiiii Got This boundary is provisional.

During specification, responsibilities such as synchronization, identity, service registry, notifications, shared spatial presentation, or other integration concerns may be identified as separate bounded contexts if their domain responsibilities justify independent ownership.

## Possible Shared Map context

A Shared Map bounded context may later exist if several independent services need to contribute spatial information to a common presentation.

In that model:

- the contributing service owns the meaning of its spatial data,
- the service publishes an explicit map contribution or projection,
- the Shared Map context owns service-independent composition and rendering,
- Wiiii Got This may integrate the resulting capability according to device and platform.

This is a design hypothesis, not an implementation decision.

## Relationship to service presentation

A service, capability, integration module, and presentation contribution are not assumed to be the same concept.

A service may expose multiple capabilities.

A Wiiii Got This integration may present those capabilities through one or more presentation mechanisms.

The concrete mechanism may later involve declarative presentation, native adapters, portable UI surfaces, dynamically loaded modules, remote rendering, or another design.

No mechanism is selected during the initial product specification.

## Specification-first workflow

Before implementation:

1. define the product and domain vision,
2. document concrete usage scenarios,
3. establish the ubiquitous language,
4. classify subdomains,
5. model the domain,
6. determine bounded-context boundaries,
7. define the context map,
8. specify application use cases,
9. design published contracts only where concrete scenarios require them,
10. define read models,
11. select architecture and technology through explicit ADRs,
12. define acceptance tests,
13. create an implementation plan.

Codex or Luna implementation work begins only after the relevant specification and contracts are stable enough to implement without inventing product decisions.


## Accepted V1 Architecture Baseline

The current accepted implementation baseline is:

- primary clients: Windows desktop + iPhone,
- C# / .NET 10,
- Avalonia 12,
- CommunityToolkit.Mvvm for presentation state,
- SQLite via Microsoft.Data.Sqlite for WGT-owned local state,
- WGT-native executable presentation,
- Integration Adapters shipped with WGT,
- provider-specific Published Contracts/transports,
- no arbitrary runtime-downloaded native plugins in V1,
- no mandatory WGT server,
- separate Synchronization / Relay bounded context/service,
- personal Device trust/pairing without a mandatory user account,
- hybrid recovery using trusted-Device approval plus separately stored emergency recovery material,
- local-only Services/data remain supported.

WGT is one bounded context with multiple presentation/runtime adapters. Windows and iPhone are not separate WGT bounded contexts.

See `docs/adr/` for accepted decisions and `docs/22_DEFERRED_DECISIONS.md` for decisions implementation agents must not invent.

## Foreign Context Alignment

Current Vocation and Illumination repository alignment is recorded in:

- `docs/23_FOREIGN_CONTEXT_ALIGNMENT.md`

Provider repositories remain authoritative for their own domain and published-contract semantics.
