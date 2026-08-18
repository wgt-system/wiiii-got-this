# WGT Atlas – Full-Service Hosting Options

## Status

Design analysis for the post-v0.6 WGT Atlas direction.

This document is not an accepted runtime architecture decision. It identifies the pressure created by the product requirement that WGT expose complete service experiences rather than permanently reduced WGT-native replicas.

System-wide cross-context policy remains authoritative in `wgt-system/architecture`. Existing WGT ADR-0009 remains accepted until deliberately superseded or amended.

Tracked by WGT Issue #46 and Draft PR #47.

## 1. Problem

The current accepted V1 presentation baseline is:

```text
Provider semantics/data/commands
        -> Published Contract
        -> WGT Integration Adapter
        -> WGT-native Avalonia presentation
```

That is appropriate for small cross-context projections and has successfully produced the current Vocation Opportunity Overview and Vocation/Orientation Map composition.

It becomes problematic if interpreted as the permanent integration model for complete products.

Reimplementing full Vocation, Illumination and Orientation user experiences inside WGT would create:

- duplicate product UI;
- duplicate workflow orchestration;
- pressure to leak provider semantics into WGT presentation code;
- two UI implementations that must evolve together;
- a strong risk that WGT mobile becomes a deliberately reduced reader rather than the real product.

The Atlas direction therefore requires a distinction between:

1. **cross-service capability composition**, where WGT-native presentation may be ideal; and
2. **entering a complete service**, where provider-owned product surfaces may be preferable.

## 2. Existing architecture that should be preserved

The new product direction does not require discarding the useful V1 runtime model.

WGT already distinguishes:

- in-process foreign runtimes;
- local out-of-process providers;
- remote providers;
- future replicated local runtimes;
- provider-specific transports;
- WGT Integration Adapters;
- normalized capability/availability resolution.

Those deployment/runtime concepts remain useful.

The decision pressure concerns **presentation ownership and complete-service entry**, not the basic rule that bounded contexts may run in different places and technologies.

## 3. Required product distinction

### 3.1 Atlas composition surface

WGT owns the Atlas.

WGT may natively render:

- service nodes;
- capability nodes;
- dependency edges;
- availability;
- activation/configuration state;
- privacy/data summaries;
- search/jump;
- WGT-global theme/sound/effects controls.

This is genuinely WGT product semantics.

### 3.2 Composed cross-service capability

WGT may continue to render a native composition when it combines provider capabilities into a WGT-specific experience.

Example:

```text
Vocation Map Projection
        +
Orientation generic map capability
        -> WGT-composed map experience
```

A WGT-native surface remains legitimate here because the product composition itself is WGT-owned.

### 3.3 Complete service surface

When the user enters Vocation, Illumination or Orientation as a first-class service, WGT should not require a second complete implementation of that provider product.

Preferred direction to evaluate:

```text
Atlas service node
      -> WGT Service Host
          -> provider-specific presentation boundary
              -> provider-owned complete product surface
```

This is not the same as making WGT a generic arbitrary plugin shell. Initial providers are controlled WGT-system projects and may use explicit service-specific hosting adapters.

## 4. Candidate hosting models

### Model A — WGT-native complete reimplementation

Provider publishes data/commands; WGT recreates the whole service UI.

Strengths:

- maximum control over native WGT look/feel;
- straightforward WGT accessibility/platform composition;
- no foreign executable UI surface.

Weaknesses:

- duplicates complete product UI/workflows;
- high maintenance cost;
- pushes provider-specific presentation knowledge into WGT;
- encourages feature drift and WGT-lite/mobile-lite behavior;
- particularly poor fit for rapidly evolving services such as Vocation.

Recommendation:

- keep for small WGT-owned compositions and narrow capabilities;
- do not use as the universal complete-service strategy.

### Model B — Provider-owned web surface hosted by WGT

Provider ships/serves a complete web UI. WGT hosts it through a platform WebView and service-specific adapter.

Strengths:

- provider repository owns its product UI;
- same surface can potentially run on Windows and iPhone WebViews;
- excellent fit for existing React/TypeScript or browser-based provider products;
- avoids duplicating complex interaction flows in WGT.

Weaknesses:

- provider runtime/backend still needs a viable topology on each platform;
- focus, keyboard, accessibility, file handling, navigation and deep-link boundaries need explicit host behavior;
- theme integration is not automatic;
- a WebView must not become a universal architecture requirement merely because Vocation and Orientation already use browser UI.

Recommendation:

- strongest near-term Windows candidate for Vocation;
- strong Windows candidate for Orientation's complete standalone product surface;
- evaluate service-specifically rather than declaring a universal Web UI contract.

### Model C — Provider-owned native/static presentation module

A provider ships presentation code as part of the signed WGT application and WGT hosts it behind an explicit adapter boundary.

Strengths:

- provider owns its complete presentation;
- can feel fully native;
- compatible with iPhone rules when statically delivered with the signed application rather than downloaded as executable code;
- excellent potential fit where provider and WGT share a compatible UI/runtime stack.

Weaknesses:

- creates compile/package dependency at the outer host;
- needs strict boundary rules so foreign domain objects do not leak into WGT Core;
- provider update can require a WGT application release;
- technology-specific and unsuitable as a universal cross-language mechanism.

Recommendation:

- strong candidate for Illumination because Illumination and WGT are .NET/Avalonia-compatible;
- keep service-specific unless repeated patterns justify a small common host contract.

### Model D — Declarative provider presentation

Provider publishes constrained presentation metadata interpreted by WGT.

Strengths:

- transportable and potentially cross-platform;
- provider can influence presentation without executable UI plugins;
- can support simple dynamic capabilities without a WGT release.

Weaknesses:

- building a sufficiently powerful schema for complete Vocation/Illumination/Orientation risks creating a new UI framework;
- complex workflows quickly become awkward or provider semantics leak into generic primitives;
- easy to over-generalize prematurely.

Recommendation:

- potentially useful for small capabilities, forms or metadata;
- do not use as the complete-service solution unless repeated concrete integrations prove a limited common language.

### Model E — External provider application launch

Atlas node launches the provider's separate application/window.

Strengths:

- trivial ownership boundary;
- no embedded UI complexity.

Weaknesses:

- WGT becomes a launcher rather than a coherent product;
- poor spatial continuity;
- weak phone experience;
- makes WGT-level theme/host integration mostly irrelevant.

Recommendation:

- acceptable fallback/admin/developer path;
- not the desired primary Atlas service-entry experience.

## 5. Recommended hybrid direction

Do not select one presentation mechanism for all services.

Use the smallest provider-specific mechanism that preserves:

- full service capability parity;
- provider ownership;
- coherent WGT hosting;
- platform viability;
- accessibility;
- security;
- maintainability.

Conceptually:

```text
                    WGT Atlas
                        |
                WGT Service Host
                        |
        +---------------+----------------+
        |               |                |
   WGT-native      Provider Web      Provider Native
   composition       surface           surface
        |               |                |
   provider        provider          provider
   contracts       runtime           runtime
```

The choice belongs to each concrete integration until enough repetition exists to justify a common system-level contract.

## 6. Service-by-service analysis

### 6.1 Vocation

#### Current runtime

- Python 3.13 / FastAPI backend;
- local SQLite + private document store;
- React/TypeScript/Vite complete product UI;
- local HTTP production topology on Windows;
- current client-neutral WGT Published Contracts are intentionally narrow read projections;
- current architecture explicitly says Python/FastAPI does not run inside the iPhone WGT client.

#### Windows recommendation

The strongest full-service path is to evaluate hosting Vocation's **existing provider-owned React product surface** inside WGT while Vocation's local FastAPI process remains the authoritative runtime.

Conceptual topology:

```text
WGT Desktop Atlas
    -> Vocation Service Host
        -> NativeWebView/WebView2
            -> Vocation provider-owned React UI
                -> local Vocation FastAPI runtime
                    -> Vocation SQLite/documents
```

WGT should start/locate/observe the provider through its Vocation Integration Adapter rather than duplicate Vocation workflows.

This can coexist with WGT-native cross-service compositions such as Atlas metadata or a WGT-specific Vocation/Orientation capability view.

Important: the fact that WebView is a good Vocation adapter does not make WebView a universal provider contract.

#### iPhone pressure

The React surface itself can plausibly translate to a WKWebView-style host, but the current authoritative Vocation backend topology does not.

The existing two read-only Published Contracts cannot satisfy full-service parity because they expose neither the complete private Vocation state nor command/workflow semantics.

A later Vocation-owned decision is required among directions such as:

- a mobile-compatible local Vocation runtime;
- a replicated/local runtime backed by explicit Vocation synchronization semantics;
- secure remote access to an authoritative Vocation runtime with acceptable offline/product behavior;
- another provider-owned mobile topology.

Do not solve this by expanding read snapshots until they accidentally become an unowned synchronization API.

### 6.2 Illumination

#### Current runtime

- .NET 10 executable capability runtime;
- local SQLite/EF Core authoritative state;
- Avalonia optional standalone/admin/dev host;
- WGT is already designated primary end-user presentation on Windows and iPhone;
- architecture already permits in-process WGT hosting through explicit Illumination-owned boundaries.

#### Windows/iPhone recommendation

Illumination is the strongest candidate for a **provider-owned statically bundled native presentation module**.

A direction to evaluate:

```text
WGT signed application
    -> Illumination Integration Adapter / Service Host
        -> Illumination-owned reusable Presentation Surface
            -> Illumination Application boundary
                -> Illumination Domain/Persistence runtime
```

The reusable surface could also be consumed by the optional standalone Illumination host so the provider does not maintain two complete product UIs.

WGT Core/Application must still not import Illumination Domain types. The dependency can live at the outer host/integration layer.

Because the provider runtime is already .NET/local-first, this direction has a much clearer path to true phone-local operation than Vocation or Orientation.

A signed iOS WGT build may statically include Illumination executable/runtime/presentation code; arbitrary downloaded executable plugins remain unnecessary.

### 6.3 Orientation

#### Current runtime

- Java 25 / Spring Boot backend;
- local SQLite for Orientation-owned Discovery state;
- TypeScript/MapLibre browser package;
- provider-owned standalone `app.html` product surface;
- separate reusable Reference and Embed map hosts;
- external provider/routing infrastructure such as Photon/Valhalla/MOTIS depending on capability.

#### Windows recommendation

For the complete Orientation service, evaluate embedding the provider-owned standalone browser product rather than rebuilding Discover/Explore/Navigate inside WGT.

Conceptual topology:

```text
WGT Desktop Atlas
    -> Orientation Service Host
        -> WebView
            -> Orientation standalone product UI
                -> Orientation Java backend
                    -> Orientation SQLite/providers
```

For composed foreign maps, continue using the narrower provider-neutral Orientation Embed Host where that is the correct capability boundary.

This distinction is important:

- `Orientation service node -> complete Orientation product surface`
- `Vocation Map dependency -> reusable Orientation map capability`

They are not the same presentation use case.

#### iPhone pressure

The browser/map UI can plausibly run in a mobile WebView, but the current Java backend and local Discovery persistence do not automatically translate to iPhone.

Full mobile product parity therefore requires an Orientation-owned mobile/runtime topology decision rather than a WGT UI workaround.

The solution may differ per capability: some generic map functions can execute entirely in the browser surface, while discovery persistence, place/routing/journey services and provider adapters require explicit runtime access.

Do not infer that a working map WebView proves complete Orientation mobile support.

### 6.4 Conveyance

Conveyance differs from the three product-heavy services.

It is generic infrastructure for durable opaque cross-device delivery. A first-class Atlas node can explain availability, trust/connection state and which service capabilities use it without requiring a large standalone end-user application.

Its complete user-facing surface may remain relatively small unless later concrete scenarios justify more.

This is not a violation of full-service parity: parity means exposing the full meaningful Conveyance product capability set, not manufacturing workflows that Conveyance does not own.

## 7. Presentation contract pressure

A reusable cross-context presentation-host contract should be introduced only if concrete Vocation/Illumination/Orientation integrations reveal genuinely common host semantics.

Possible common host-level concepts worth observing include:

- service identity;
- presentation identity/version;
- supported Client Environments;
- required host features;
- preferred minimum viewport constraints;
- theme token support;
- lifecycle start/suspend/resume/close;
- deep-link/entry target;
- focus/accessibility bridge;
- WGT return/exit signal;
- error/health reporting;
- host-authorized file/clipboard/browser/permission actions.

These are **candidate repeated needs**, not an accepted universal wire schema.

Provider business semantics and navigation must not be normalized into this host layer.

## 8. Security and trust constraints

A provider-owned presentation surface must not become an escape hatch around existing boundaries.

Required principles:

- WGT Core does not receive provider Domain objects;
- provider UI cannot gain arbitrary WGT privileges merely because it is embedded;
- OS-sensitive operations should use explicit host/provider policy where appropriate;
- remote web surfaces require explicit trust/origin/version handling;
- service host failure remains isolated from other Atlas nodes;
- presentation version compatibility is explicit;
- downloaded arbitrary native executable code is not introduced on iPhone;
- private provider data stays within provider-authorized boundaries.

## 9. Theme integration

Full provider-owned product surfaces should visually belong to WGT without requiring WGT to own their entire styling.

A practical layered model:

- Atlas theme is always WGT-owned;
- WGT outer service host/transition/chrome is WGT-owned;
- provider surface owns its product layout;
- provider may opt into a small set of WGT theme tokens such as appearance mode, accent/surface hints, density/effect preference;
- provider retains authority over domain-specific visualization.

Do not require a miniature-world theme to transform the internals of Vocation into literal city buildings. The theme governs the Atlas world and host framing; the focused Vocation product may use a coherent but more conventional provider UI.

## 10. Mobile equivalence rule

Full-service parity means:

> If a service capability is supported by the WGT product on a platform, the phone edition must not deliberately replace it with a reduced read-only surrogate merely for convenience.

It does not mean every runtime topology is identical.

Desktop may use a local sidecar provider while phone later uses a provider-owned replicated runtime or trusted remote runtime, as long as:

- the service owns the semantics;
- behavior is explicit;
- privacy/authority are preserved;
- product capability is not arbitrarily removed.

Offline guarantees, synchronization and authority must be decided by each provider rather than inferred from the parity principle.

## 11. Recommended sequence after v0.6

1. Accept/refine the Atlas product direction in #46.
2. Revisit WGT ADR-0009 because its blanket WGT-native-presentation default conflicts with full-service parity for rich providers.
3. Use concrete providers to validate service-specific full-surface hosting rather than designing a universal plugin framework.
4. First Windows experiments should favor:
   - Vocation provider-owned web product surface;
   - Illumination provider-owned native/static surface;
   - Orientation provider-owned standalone web product surface.
5. Keep current narrow WGT-native Vocation/Orientation compositions as valid cross-service capability examples rather than deleting them solely because full-service hosting exists.
6. Record the repeated host semantics discovered across those three integrations.
7. Only then decide in the System Architecture Control Plane whether a reusable Presentation Contribution/Service Host contract is warranted.
8. Mobile implementation follows when real Apple tooling exists, but every Desktop decision must preserve a plausible iPhone path and identify provider runtime blockers explicitly.

## 12. Current recommendation

The best present direction is a **hybrid service-host architecture**:

- Atlas and cross-service composition remain WGT-native;
- full rich service products are preferably provider-owned;
- WGT hosts those products through explicit service-specific boundaries;
- no universal UI language is invented now;
- no universal WebView rule is invented now;
- no foreign domain ownership moves into WGT;
- full mobile parity remains a product requirement even where provider runtime work is still unresolved.

This direction should be used as decision input, not treated as accepted architecture until ADR/system-control-plane work resolves the conflict with the current V1 presentation baseline.