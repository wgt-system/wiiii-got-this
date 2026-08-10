# ADR-0009: WGT-Owned Presentation and Contract-Driven Service Integration

- Status: Accepted
- Date: 2026-08-10

## Context

Wiiii Got This presents capabilities from independently owned Services while preserving the meaning and authority of those Services. The first WGT implementation uses statically shipped Integration Adapters and WGT-native Avalonia presentation. Vocation now provides a concrete versioned read contract, while Conveyance provides a separate generic delivery context. Future compatible remote/read Services should not require WGT to absorb their business models or rebuild the client unnecessarily.

## Decision

### WGT owns the product experience

WGT is not a launcher and not a container for foreign application UIs. For capabilities presented through WGT:

- WGT owns navigation;
- WGT owns platform-specific layout;
- WGT owns visual composition;
- WGT owns integration-level status and availability presentation;
- WGT owns how a foreign capability fits into the coherent WGT product experience.

A foreign bounded context owns the meaning and business semantics of the data and actions it publishes.

The default model is:

```text
foreign Service semantics
        ↓
versioned Published Contract
        ↓
WGT integration boundary
        ↓
WGT-native presentation
```

Embedded foreign React/Avalonia/WebView UI is not the normal integration mechanism. A standalone foreign UI may still exist for provider administration, development, rich desktop workflows, or independent operation.

### Contract-driven extension is the preferred remote/read path

For remote, out-of-process, and read-oriented Services, WGT consumes explicit versioned semantic contracts through an integration boundary. The target architectural property is:

> Adding an ordinary compatible Service or Capability should not inherently require a new WGT/iOS build when existing WGT integration, invocation, and presentation capabilities suffice.

This is a target property, not a claim that the current v0.2.0 shipped-adapter implementation supports arbitrary runtime registration. Current V1 shipped Integration Adapters remain valid implementation scaffolding.

### A WGT rebuild remains legitimate

A new WGT build is expected when the WGT platform changes, including:

- a new native/platform capability;
- a new WGT presentation primitive;
- new WGT executable integration logic not expressible through an accepted contract;
- bundling or changing a local executable foreign capability runtime;
- platform, signing, or runtime changes.

The goal is to avoid unnecessary rebuilds for ordinary compatible Services, not to prohibit WGT rebuilds.

### Local executable runtimes are different

Illumination remains a valid example of a locally hosted executable capability runtime. A future iPhone WGT build may contain WGT presentation alongside an Illumination Application/Domain/Persistence runtime. The bounded-context boundary remains explicit even when process-local. Changing executable Illumination runtime code on iPhone can legitimately require a new signed WGT/iOS build.

Downloaded arbitrary native/.NET plugin execution is not introduced.

### Transport does not define presentation

The same provider contract should remain usable across transport mechanisms where its semantics permit it. The concrete Vocation direction is:

```text
Windows: WGT → local Vocation HTTP Published Contract
iPhone:  WGT → Conveyance-delivered copy of the same Vocation Published Contract
```

WGT presentation consumes the validated provider contract and does not depend on whether the bytes came directly from the provider or through Conveyance. Conveyance is not a UI or business-contract abstraction.

### No universal UI language now

Do not define a permanent universal `List / Detail / Form / Map / Action / ...` presentation schema or a mini-HTML/mini-Flutter framework. Implement concrete WGT-native capabilities, observe repeated integration pressure, and generalize only proven presentation/invocation primitives. Capability taxonomy and generic requirement schema remain deferred.

### Integration activation remains WGT-owned

WGT owns whether an integration is known, configured, or enabled for a Device. A Service does not become enabled merely because its contract exists. This ADR does not define a generic Registry or install-store lifecycle; future discovery/install mechanisms remain separate decisions.

### Mac availability is operational, not domain architecture

Rare Mac access does not force WGT into a browser/WebView architecture. WGT remains a real Windows/iPhone product, while the architecture should minimize unnecessary iOS rebuilds. The existing real Mac/Xcode/iPhone runtime gate remains mandatory before the first real provider integration is accepted on iPhone. Windows can implement and validate the first real WGT-native integration before that Apple runtime step.

### Web remains optional

A future Web client remains valid, but Web is not implemented or prioritized solely to avoid the Mac build requirement. It requires a concrete product scenario.

## Consequences

- WGT-native presentation is the default coherent product experience.
- Provider ownership remains behind explicit versioned Published Contracts.
- Vocation Published Opportunity Overview 1.0 is a valid first concrete read integration candidate.
- Conveyance may transport an opaque/protected copy of that contract without interpreting its semantics.
- Current V1 shipped adapters remain valid, while arbitrary dynamic executable plugins are not introduced.
- Future generic extension must be justified by concrete integrations and accepted separately where it changes architecture.
