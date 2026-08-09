# ADR-0008: V1 Service Registration, Publication, and Discovery

- Status: Accepted
- Date: 2026-08-09

## Context

Wiiii Got This must know which Services it can integrate and obtain the published information required to resolve their Capabilities.

The first integrated Services are controlled projects such as Vocation and Illumination.

There is no V1 requirement for:

- arbitrary third-party plugins,
- LAN-wide automatic discovery,
- a public Service marketplace,
- a universal registry,
- one universal transport protocol.

Different Services may use different runtimes and transports.

## Decision

V1 uses **explicit known Integration Adapters** as the starting point for Service integration.

Each shipped Integration Adapter owns the technical knowledge needed to:

- identify its supported Service family,
- locate/configure its provider runtime,
- obtain and validate its Service/Capability publication,
- interpret provider-specific contract/version information,
- translate it into WGT's normalized integration model.

## Registration

A Service becomes known to WGT when a shipped Integration Adapter successfully establishes a valid registration/publication relationship or when the user explicitly configures a supported provider.

Registration does not imply:

- enablement,
- reachability,
- trust,
- compatibility,
- availability.

WGT persists only the WGT-owned registration/integration metadata required by its own domain.

## Publication

There is **no mandatory universal wire-format publication protocol in V1**.

A provider may publish through a technology appropriate to its boundary.

Examples:

```text
Illumination
→ versioned .NET Published Contract / compatible local runtime

Vocation
→ versioned HTTP/JSON Published Contract

Future Service
→ another explicit interoperable Published Contract
```

The WGT Integration Adapter translates the provider publication into WGT's normalized internal Service/Capability descriptors.

## Discovery

V1 discovery is intentionally narrow and adapter-specific.

Supported directions may include:

- an in-process runtime already composed by the host,
- an explicitly configured local provider location,
- an explicitly configured remote provider location,
- a deterministic local provider convention where the adapter owns that convention.

Do not implement automatic LAN discovery merely because it is technically possible.

## Trust

Discovery/registration never implies trust.

Remote/protected operations use the accepted Device-trust/security architecture.

A provider-specific trust/auth requirement may also exist and must remain explicit.

## Future Registry

A future generic Service Registry becomes justified only if concrete use cases require multiple producers/consumers to manage:

- Service identity publication,
- provider locations,
- trust-aware registration lifecycle,
- dynamic integration catalog,
- remote discovery.

If that lifecycle becomes independently meaningful, extract it as its own bounded context/service rather than hiding it inside WGT Core.

## Consequences

### Positive

- smallest mechanism that satisfies known V1 integrations,
- supports polyglot Services,
- avoids premature service-mesh/registry infrastructure,
- permits provider-specific transport without contaminating WGT Domain,
- keeps future generic discovery extensible.

### Trade-off

Adding a completely new Service family initially requires:

- an Integration Adapter,
- WGT release/update,
- provider-specific publication support.

This is already accepted by ADR-0003.

## First Reference Integration

The WGT reference provider should use the simplest in-process/static publication path possible.

Its purpose is to test:

- registration,
- publication refresh,
- stable Service Identity,
- Capability changes,
- version compatibility,
- failure isolation.

It must not establish the wire protocol for all future Services.
