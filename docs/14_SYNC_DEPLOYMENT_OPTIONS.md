# Wiiii Got This – Synchronization and Deployment Decision Options

## Status

Architecture decision analysis.

Cross-device continuity between Windows and iPhone is accepted as a product requirement.

The ownership and deployment of synchronization infrastructure are not yet accepted.

## 1. Required Behavior

For synchronized services:

```text
Windows
    ↓
service-owned change/state
    ↓
always-available infrastructure
    ↓
Windows may go offline
    ↓
iPhone later synchronizes
    ↓
service-owned reconciliation
    ↓
workflow continues
```

The inverse iPhone-to-Windows flow must also work.

At the same time:

- synchronization must be optional,
- a Service or data class may remain local-only,
- remote infrastructure must not imply readable remote storage,
- foreign domain merge/conflict rules remain foreign-domain-owned,
- Wiiii Got This must not become the owner of Vocation or Illumination state.

## 2. Architecture Option A – Synchronization Inside Wiiii Got This

### Shape

```text
Wiiii Got This
├── Device / Capability Integration
├── Presentation
└── Synchronization
    ├── relay protocol
    ├── queues
    ├── retries
    ├── device routing
    └── durable server state
```

### Advantages

- fewer projects initially,
- WGT already knows Devices and enabled Services,
- simple first deployment ownership.

### Disadvantages

- WGT begins to own substantial cross-service infrastructure,
- synchronization lifecycle may evolve independently of UI/capability integration,
- Vocation, Illumination, and future Services become dependent on a WGT-specific mechanism,
- increased risk of WGT becoming a central infrastructure monolith,
- server-side relay concerns have little to do with presentation/capability resolution.

### Assessment

Possible for an initial prototype, but poor as the intended long-term ownership boundary.

## 3. Architecture Option B – Separate Synchronization / Relay Context

### Shape

```text
                 ┌─────────────────────┐
                 │ Sync / Relay Context│
                 │                     │
                 │ device mailboxes    │
                 │ envelopes           │
                 │ delivery state      │
                 │ retry / ack         │
                 │ retention           │
                 └─────────┬───────────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
   Illumination         Vocation       Wiiii Got This
   sync adapter         sync adapter    own-state adapter
```

### Responsibility

The Sync/Relay context owns generic cross-device delivery semantics such as:

- receiving an opaque service-owned synchronization envelope,
- addressing it to the correct user/device/service stream,
- durable storage until delivery/retention policy says otherwise,
- acknowledgement,
- retry,
- sequencing/correlation metadata where generic,
- transfer status,
- possibly encrypted-at-rest/opaque-payload handling.

It does **not** own:

- Learning Item merging,
- Review conflict semantics,
- Vocation opportunity merging,
- business-level last-write-wins rules,
- interpretation of encrypted payload contents.

### Domain ownership

Each source Service owns a Sync Adapter / Published Sync Contract that defines:

- what state/change is synchronizable,
- change identity,
- authority,
- service-level ordering constraints,
- conflict detection,
- merge/reconciliation,
- whether payload may leave the device,
- whether the relay may read the payload.

### Advantages

- clear independent lifecycle,
- naturally reusable by several bounded contexts,
- WGT remains focused on integration/presentation,
- server runtime can evolve independently,
- local-only Services simply do not publish/send sync envelopes,
- opaque encrypted relay is possible,
- technology can be selected for network/service requirements rather than WGT UI requirements.

### Disadvantages

- introduces another deployable and contract family,
- requires explicit Device/Service addressing semantics,
- trust/key management must eventually be solved,
- must avoid becoming a generic distributed-data platform before concrete service needs exist.

### Assessment

**Recommended long-term boundary.**

There is already concrete pressure from at least:

- Illumination learning state across Windows/iPhone,
- Vocation mobile/read state,
- Wiiii Got This's own cross-device integration configuration.

This is enough to treat Sync/Relay as a serious separate-context candidate rather than merely hypothetical infrastructure.

## 4. Architecture Option C – Every Service Synchronizes Itself

### Shape

```text
Illumination Server / Sync
Vocation Server / Sync
WGT Server / Sync
Future Service Server / Sync
```

### Advantages

- maximum domain autonomy,
- each Service can implement exactly its own conflict semantics,
- no generic protocol beyond service-specific contracts.

### Disadvantages

- repeated device routing, retry, transport, authentication, retention, and relay infrastructure,
- each new Service needs its own always-available server path,
- larger operational footprint,
- duplicated infrastructure code,
- harder unified device management.

### Assessment

Useful only for service-specific semantics above the generic relay layer.

Not recommended for generic delivery infrastructure.

## 5. Recommended Split

Recommended architecture:

```text
Service-owned synchronization semantics
            │
            │ published sync/change contract
            ▼
Service Sync Adapter
            │
            │ opaque/generic envelope
            ▼
Synchronization / Relay Context
            │
            │ durable asynchronous delivery
            ▼
Target Device
            │
            ▼
Service Sync Adapter
            │
            ▼
Service-owned reconciliation
```

This deliberately separates:

```text
transport/delivery semantics
        ≠
foreign domain merge semantics
```

## 6. Local-Only Mode

A Service or data class can explicitly opt out of synchronization.

Then:

```text
Service
├── local state
├── local capability runtime
└── no Sync Adapter publication for that state
```

WGT may still integrate the Service locally.

The consequence is explicit:

- no cross-device continuity for that local-only state,
- no hidden server copy,
- no automatic downgrade of privacy merely to support convenience.

## 7. Remote Storage Modes

The architecture should permit several policies.

### No remote state

Nothing is sent.

### Opaque relay

Server stores encrypted/opaque envelopes temporarily or durably without understanding foreign domain contents.

### Encrypted replicated store

Server retains encrypted service-owned replication data so Devices can synchronize while peers are offline.

### Service-readable server state

Only if the owning Service explicitly chooses a server-hosted runtime/storage model.

These modes must not be globally imposed by Wiiii Got This.

## 8. Illumination Example

A plausible future flow:

```text
Illumination on Windows
    ↓
Review / Learning State change
    ↓
Illumination Sync Adapter
    ↓
encrypted Illumination envelope
    ↓
Sync / Relay
    ↓
encrypted Illumination envelope
    ↓
Illumination Sync Adapter on iPhone
    ↓
Illumination reconciliation
    ↓
local Illumination replica updated
```

WGT presents the workflow, but does not interpret the learning-state payload.

## 9. Vocation Example

Vocation may initially synchronize only selected read-oriented projections or service-owned changes.

For example:

```text
Vocation
    ↓
published mobile/read synchronization state
    ↓
Sync / Relay
    ↓
Vocation adapter/runtime available to WGT on iPhone
```

The exact Vocation data classes and privacy policy remain Vocation-owned.

## 10. Wiiii Got This Own State

WGT can use the same relay for its own synchronized state, such as:

- known Devices,
- global Service Integration enablement,
- Device overrides where appropriate,
- non-sensitive user configuration.

WGT remains the domain owner of that state.

The fact that WGT consumes Sync/Relay does not make Sync/Relay part of the WGT bounded context.

## 11. Deployment Direction

A likely eventual topology is:

```text
Windows PC
├── WGT Windows Client
├── local capability runtimes/adapters
└── local databases

iPhone
├── WGT iPhone Client
├── local capability runtimes/adapters
└── local databases/replicas

Personal Server
├── Sync / Relay service
├── optional service-specific server components
└── optional future registry/trust components
```

Docker is appropriate for server-side components where useful.

A browser UI is not required.

## 12. Repository Boundary

If Sync/Relay is accepted as a separate bounded context/service, it should **not** live inside the Wiiii Got This repository merely as an internal folder long-term.

Recommended eventual ownership:

```text
wiiii-got-this/
illumination/
vocation/
<sync-relay-project>/
```

The project name and implementation technology are deliberately not selected here.

A temporary reference/fake relay may exist in WGT tests, but it must not become the production implementation by accident.

## 13. Security Decisions Deferred

The following must be solved before real remote synchronization:

- Device identity,
- user identity if needed,
- device trust,
- service authentication,
- key establishment,
- encryption ownership,
- key storage,
- server compromise model,
- payload metadata visibility,
- revocation,
- lost Device handling.

These concerns may justify a separate Identity/Trust context or security subsystem.

## 14. Recommended Decision

The accepted system architecture is **Option B**:

> Cross-device delivery uses the separate Conveyance bounded context for generic durable opaque delivery, while each domain Service owns synchronization eligibility, payload semantics, authority, conflict detection, and reconciliation.

WGT must not implement a competing relay or make Conveyance interpret foreign business payloads. Conveyance's accepted V1 delivery mode is Current Object; further delivery modes still require concrete contracts and system decisions.

Before any domain-changing synchronization is integrated, define one concrete synchronized Service flow—most likely Illumination—and use it to finalize the provider-owned contract and reconciliation semantics.

## 15. Consequence for Wiiii Got This

WGT remains responsible for:

- Device/platform-aware integration,
- Service enablement,
- Capability availability,
- presentation,
- coordination of whether a synchronized capability can be used locally.

WGT does not own:

- generic relay queues,
- foreign sync payloads as business data,
- foreign merge/conflict semantics.

This keeps Wiiii Got This from becoming the central infrastructure monolith.
