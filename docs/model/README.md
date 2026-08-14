# WGT Architecture Model

## Purpose

This is a service-owned, derived design-time visualization of the accepted Wiiii Got
This runtime topology. WGT documentation and accepted ADRs remain authoritative for
internal architecture; `wgt-system/architecture` remains authoritative for system-wide
ownership and cross-context integration. The model is not consumed at runtime.

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
[`wgt-system/architecture`](https://github.com/wgt-system/architecture). If this model
conflicts with accepted WGT documentation or ADRs, correct the diagram or involve the
Architecture Control Plane; never derive a new Architecture Decision from the diagram.

## Local usage

The official `structurizr/structurizr` image can be run from `docs/model/`:

```powershell
docker run --rm -v "P:\wgt-system\wiiii-got-this\docs\model:/usr/local/structurizr" structurizr/structurizr validate -w workspace.dsl
docker run --rm -v "P:\wgt-system\wiiii-got-this\docs\model:/usr/local/structurizr" structurizr/structurizr inspect -w workspace.dsl
docker run --rm -d --name wgt-wiiii-got-this-structurizr -p 127.0.0.1:18081:8080 -v "P:\wgt-system\wiiii-got-this\docs\model:/usr/local/structurizr" structurizr/structurizr local
```

Docker and Structurizr are design-time tooling only. They are not runtime dependencies,
runtime configuration, or a service registry. No CI workflow is defined here.
