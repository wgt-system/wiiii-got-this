# ADR-0009: WGT-Owned Composition and Contract-Driven Service Integration

- Status: Accepted
- Date: 2026-08-10
- Updated: 2026-08-18

## Context

Wiiii Got This presents capabilities from independently owned Services while preserving the meaning and authority of those Services. The first WGT implementation used statically shipped Integration Adapters plus WGT-native Avalonia presentation for narrow provider contracts such as Vocation Opportunity Overview and Map Projection.

That baseline remains useful, but the later WGT Atlas/full-service direction exposed a second concrete need: rich bounded contexts such as Vocation, Illumination and Orientation must be usable through WGT without forcing WGT to rebuild each provider's complete product UI and workflow.

System Architecture Control Plane ADR-0005 now accepts **provider-owned Product Surfaces** as a valid WGT integration shape when justified by a concrete rich service. This service-local ADR is aligned to that binding system decision.

## Decision

### WGT owns composition, not every provider screen

For capabilities and products presented through WGT, WGT owns:

- Atlas and WGT-level navigation;
- platform-specific hosting and composition;
- integration-level status and availability presentation;
- service/capability/dependency presentation in the Atlas;
- transitions into and out of focused provider experiences;
- WGT-global appearance/accessibility/effect policy;
- WGT-specific cross-service compositions.

A foreign bounded context continues to own the meaning and business semantics of its data and actions.

WGT does **not** gain ownership of a provider workflow merely because it hosts or presents it.

### Two presentation shapes are valid

#### A. WGT-native composed capability

Use WGT-native presentation when the experience is genuinely WGT-owned composition or when a bounded provider contract maps naturally to a small WGT surface.

```text
foreign Service semantics
        ↓
versioned Published/Application Contract
        ↓
WGT integration boundary
        ↓
WGT-native composition
```

The existing Vocation Opportunity Overview and Vocation Map Projection integrations are valid examples. They are not a requirement to rebuild the complete Vocation application in WGT.

#### B. Provider-owned Product Surface

For a rich provider-specific product workflow, WGT may host a provider-owned presentation artifact through an explicit provider-specific boundary.

```text
provider-owned product/runtime
        ↓
provider-owned Product Surface
        ↓
WGT provider-specific host adapter
        ↓
WGT Atlas / platform composition
```

A Product Surface may be native or browser-based according to the provider and target platform. The presentation technology does not define bounded-context ownership.

The first concrete implementation direction is a Windows WGT host for Vocation's existing provider-owned React/FastAPI product.

### Contract-driven extension remains the preferred bounded integration path

For remote, out-of-process and read/command-oriented capabilities, WGT consumes explicit versioned provider contracts through an integration boundary.

The target architectural property remains:

> Adding an ordinary compatible Service or Capability should not inherently require a new WGT/iOS build when existing WGT integration, invocation and presentation capabilities suffice.

This does not mean every rich product must be represented as a generic data/form schema.

### A WGT rebuild remains legitimate

A new WGT build is expected when the WGT platform changes, including:

- a new native/platform capability;
- a new WGT presentation primitive;
- new executable provider-specific host integration;
- bundling or changing a local executable foreign capability runtime;
- platform, signing or runtime changes.

The goal is to avoid unnecessary rebuilds for ordinary compatible service data/metadata changes, not to prohibit WGT rebuilds.

### Local executable runtimes remain valid

Illumination remains a valid example of a locally hosted executable capability runtime. A future WGT build may contain WGT presentation/hosting alongside an Illumination Application/Domain/Persistence runtime while preserving explicit bounded-context boundaries.

Downloaded arbitrary native/.NET plugin execution is not introduced.

### Transport does not define presentation

Provider semantics should remain usable across transports where the contract permits it. Conveyance remains transport/delivery infrastructure and is not a UI or business-contract abstraction.

A provider-owned Product Surface likewise does not authorize cross-device writes or synchronization. Domain-changing cross-device behavior still requires provider-owned authority/merge/conflict/reconciliation semantics.

### No universal UI language or plugin host now

Do not define a permanent universal `List / Detail / Form / Map / Action / ...` schema, mini-HTML framework, generic downloaded plugin runtime, or one mandatory provider UI technology.

Concrete provider hosts may legitimately differ:

- Vocation can use a provider-owned web Product Surface;
- Illumination may use a statically bundled .NET/Avalonia Product Surface;
- Orientation may use its provider-owned browser product for full Orientation while retaining its narrow generic map Embed Host for foreign compositions.

A reusable Product Surface / Presentation Contribution / Service Host contract may be introduced only after repeated real integrations reveal stable common semantics and the Architecture Control Plane accepts them.

### Integration activation remains WGT-owned

WGT owns whether an integration is known, configured or enabled for a Device. A Service does not become enabled merely because its product identity or a Product Surface exists.

The Atlas may truthfully show a known first-class service before a client adapter exists, but it must not invent provider capabilities, availability or contracts.

### Full-service parity across supported platforms

WGT should not intentionally create reduced `Desktop full / mobile lite` editions.

Desktop landscape, phone landscape and phone portrait may differ in layout, density, effects and interaction composition. The supported provider capability set should remain equivalent unless a genuine provider/platform requirement makes a capability unavailable.

Vocation and Orientation do not yet have accepted full iPhone runtime topologies. This remains provider-owned architecture work and must not be hidden by expanding read-only contracts into ad-hoc write APIs.

### Mac availability is operational, not domain architecture

Limited Mac availability does not force WGT into one presentation technology. Real Mac/Xcode/iPhone runtime validation remains mandatory before claiming actual iPhone support for a provider integration.

## Consequences

- WGT owns the coherent product composition without needing to duplicate every rich provider UI.
- WGT-native presentation remains preferred for genuinely WGT-owned cross-service composition and bounded capability surfaces.
- Provider-owned Product Surfaces are valid for justified rich service experiences under Architecture ADR-0005.
- Provider domain ownership remains behind explicit provider boundaries; direct DB/domain coupling remains prohibited.
- Vocation Opportunity Overview 1.0 and Map Projection 1.0 remain valid narrow contracts and are not promoted into full-product APIs.
- Illumination and Orientation can become first-class WGT products using provider-appropriate presentation/runtime boundaries.
- Current shipped adapters remain valid; arbitrary dynamic executable plugins are not introduced.
- Future generic presentation-host abstractions require concrete repeated evidence and a separate architecture decision.
