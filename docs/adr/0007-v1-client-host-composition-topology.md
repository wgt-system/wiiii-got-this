# ADR-0007: V1 Client Host and Composition Topology

- Status: Accepted
- Date: 2026-08-09

## Context

Wiiii Got This is one bounded context with Windows and iPhone presentation/runtime adapters.

The first product does not require a dedicated WGT server.

Integrated Services may use different deployment forms:

- in-process portable runtime,
- local out-of-process provider,
- remote provider,
- later replicated local runtime.

The process topology must preserve bounded-context boundaries without forcing one process per context on every platform.

## Decision

For V1, each WGT client is one primary application host:

```text
Windows
Wiiii Got This process
├── WGT composition root
├── WGT Domain/Application
├── WGT Avalonia Presentation
├── WGT Infrastructure
└── statically shipped Integration Adapters

iPhone
Wiiii Got This process
├── WGT composition root
├── WGT Domain/Application
├── WGT Avalonia Presentation
├── WGT Infrastructure
└── statically shipped Integration Adapters
```

An Integration Adapter may:

- call an in-process foreign Published Contract/runtime,
- call a separate local process,
- call a remote provider.

WGT Core does not depend on the provider topology.

## In-Process Foreign Context Hosting

A foreign bounded context may be hosted in the same OS process when required by the platform/product.

Example direction:

```text
WGT iPhone process
├── WGT bounded-context code
└── Illumination capability runtime
```

This is allowed only when:

- the dependency is through Illumination's Published Contract/port,
- WGT Domain/Application does not import Illumination internal Domain/Persistence types,
- composition occurs at the outer host boundary,
- independent tests preserve the semantic boundary.

Same process does not mean same bounded context.

## Windows Providers

Windows may more often use out-of-process providers where those Services already have their own runtime.

Example:

```text
WGT Windows
    ↓
Vocation Integration Adapter
    ↓ HTTP/JSON published contract
Vocation local FastAPI process
```

WGT does not require Vocation to move into .NET.

## iPhone Providers

On iPhone, a Service requiring local/offline behavior may provide a compatible local capability runtime that is statically included in the signed WGT application.

A Service that only supports remote behavior may remain remote and becomes unavailable when its required provider cannot be reached.

## No Mandatory WGT Server

WGT V1 does not introduce a server merely for architectural symmetry.

Server-side components exist only where independently required, especially:

- Synchronization / Relay,
- possible future service-specific server runtimes,
- possible future registry/trust infrastructure.

## Composition

Use explicit composition/root registration rather than runtime reflection-based plugin discovery.

Reasons:

- Integration Adapters ship with WGT in V1,
- static composition is easier to reason about under iOS/AOT,
- arbitrary dynamic native plugin loading is outside the V1 model.

The exact dependency-injection library is not domain-significant and should remain minimal.

## Failure Isolation

A failing adapter must not prevent unrelated adapters from being composed where technically possible.

Runtime failures are translated into WGT Availability/diagnostic state.

## Consequences

- one WGT application process per client is the normal V1 topology,
- bounded contexts may share a process without sharing domain ownership,
- Services remain free to use other processes/languages/transports,
- no premature WGT backend is created,
- iPhone can host portable capability runtimes where necessary.
