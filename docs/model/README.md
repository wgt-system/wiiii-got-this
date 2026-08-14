# WGT Architecture Model

## Purpose

This is a service-owned, derived design-time visualization of the accepted Wiiii Got
This runtime topology. The workspace extends the central `wgt-system/architecture`
workspace rather than duplicating its system model. WGT documentation and accepted ADRs
remain authoritative for internal architecture; the central repository remains
authoritative for system-wide ownership and cross-context integration. The model is not
consumed at runtime.

## C4 container meaning

In this model, a C4 Container is a separately executable/deployable application or a
data store. It is not a Docker Container. `Application`, `Domain`, `Infrastructure`,
`Presentation`, `Contracts`, and integration source projects are not automatically C4
Containers. The Container view shows runtime and data-store boundaries only.

## Current views

- `WgtContainers` — the accepted WGT Windows/iPhone application-host and device-local persistence boundaries.

## Source of truth

The relevant WGT sources are:

- [`../10_ARCHITECTURE.md`](../10_ARCHITECTURE.md)
- [`../adr/0006-wgt-local-persistence-stack.md`](../adr/0006-wgt-local-persistence-stack.md)
- [`../adr/0007-v1-client-host-composition-topology.md`](../adr/0007-v1-client-host-composition-topology.md)

System-wide ownership and integration authority remains in
[`wgt-system/architecture`](https://github.com/wgt-system/architecture). The active
development parent is the central `dev` workspace at
`https://raw.githubusercontent.com/wgt-system/architecture/dev/model/workspace.dsl`.
System-wide elements and relationships are not locally duplicated; this workspace adds
only WGT's service-internal container details and enables navigation from the System
Landscape through WGT's context view to `WgtContainers`. If this model conflicts with
accepted WGT documentation or ADRs, correct the diagram or involve the Architecture
Control Plane; never derive a new Architecture Decision from the diagram.

## Local usage

The official `structurizr/structurizr` image can be run from `docs/model/`:

```powershell
docker run --rm --mount "type=bind,source=$PWD,target=/usr/local/structurizr" structurizr/structurizr validate -w workspace.dsl
docker run --rm --mount "type=bind,source=$PWD,target=/usr/local/structurizr" structurizr/structurizr inspect -w workspace.dsl
docker run --rm -d --name wgt-wiiii-got-this-structurizr -e STRUCTURIZR_EDITABLE=false -p 127.0.0.1:18081:8080 --mount "type=bind,source=$PWD,target=/usr/local/structurizr" structurizr/structurizr local
```

Docker and Structurizr are design-time tooling only. They are not runtime dependencies,
runtime configuration, or a service registry. No CI workflow is defined here.
