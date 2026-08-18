# Wiiii Got This – Product Experience Direction

## Status

This document records the proposed post-v0.6 product-experience direction for Wiiii Got This.

It is intentionally separated from the active v0.6.0 release candidate. It describes a product and interaction direction, not an already released implementation and not a second source of system-architecture authority.

System-wide ownership remains authoritative in `wgt-system/architecture`. WGT remains authoritative for its product composition, service discovery/integration, device/platform presentation and the Atlas experience itself.

Tracked by GitHub Issue #46.

## 1. Product premise

WGT should not become a conventional desktop shell containing increasingly many pages, sidebar entries and reduced replicas of its services.

The target product is a lightweight spatial host for the user's WGT system.

The root experience is the **WGT Atlas**: a pannable and zoomable node world that exposes the real product topology of independently owned services and capabilities.

The Atlas should make the system understandable without requiring architecture documentation while preserving the actual bounded-context boundaries underneath it.

The current Home / Jobs / Map / Settings desktop shell is useful implementation history and release scaffolding, but it is not the intended long-term information architecture.

In particular:

- the current WGT Jobs list is a transitional Vocation integration slice, not the intended final Vocation experience;
- the current WGT Map workspace is a useful Vocation/Orientation composition proof, not the complete role of either service;
- the current large Settings surface should not define the future WGT shell;
- Illumination's absence from the current WGT UI is integration debt, not desired product scope;
- Orientation is a first-class product service as well as a provider of generic geospatial capabilities to other services.

## 2. Hard product principles

### 2.1 Atlas-first, not homepage-first

The Atlas is the WGT root state.

There is no permanent `Home` destination layered above it and no general-purpose `Back` button while navigating the Atlas. Panning to another service does not constitute page navigation.

Global WGT chrome should stay sparse. The service nodes and their contextual surfaces are the primary information architecture.

### 2.2 Full-service parity, not WGT-lite

WGT must not intentionally reduce an integrated service to a small read-only subset merely because the service is being used through WGT.

The target is full product capability parity on supported WGT platforms.

If Vocation supports research, opportunity analysis, personal profile/search strategy, triage, comparison, application workflows and documents, those capabilities should ultimately remain available when Vocation is entered through WGT.

If Illumination supports Decks, Learning Items, Study Sessions, Review history, insights, generation and lifecycle management, those capabilities should ultimately remain available through WGT.

If Orientation supports Discover, Explore, Navigate, Current Location and mobility planning, those capabilities should ultimately remain available through WGT.

Platform adaptation may change composition, density, controls and rendering quality. It must not arbitrarily remove product power.

### 2.3 Provider ownership survives presentation

Full-service parity does not mean moving foreign business semantics into WGT.

The provider remains authoritative for:

- business/domain semantics;
- application behavior;
- provider-specific workflows;
- persistence;
- provider-owned contracts;
- provider-specific presentation semantics where it contributes UI.

WGT remains authoritative for:

- Atlas navigation and composition;
- service/capability discovery and presentation;
- host chrome and transitions;
- device/platform integration;
- WGT-wide theme/effect behavior;
- safe integration boundaries.

### 2.4 Mobile is not a cut-down edition

Desktop landscape is the first implementation target because it is the currently validated platform and best environment for developing the Atlas.

Phone landscape and phone portrait may use different compositions, but supported service capability sets should remain equivalent.

Desktop may render a substantially richer world. Mobile may reduce effects, scene density and simultaneous visible detail to meet hardware constraints.

An iPhone 11-class device is the early performance/viewport design floor. Actual iOS support claims still require Mac/Xcode/physical-device validation.

### 2.5 Themes are presentation, not product forks

The Node/Connection model is the stable product model.

Technical, elegant, machine and miniature-world themes are different visual interpretations of the same semantic scene.

No theme may invent a dependency, capability or state that does not exist in the common model.

## 3. The Atlas model

At the conceptual center is **Wiiii Got This**.

Initial first-class service nodes should include:

- Vocation;
- Illumination;
- Orientation;
- Conveyance;
- additional future bounded contexts only when they become real WGT product participants.

The Atlas should answer questions such as:

- Which services are part of my WGT system?
- Which services are active or available on this device?
- What can each service actually do?
- Which capabilities depend on another service?
- What becomes possible if I activate or connect something?
- What data, network access, permissions or cross-device behavior does that introduce?
- Why is a capability unavailable or degraded?
- How do I enter the complete service experience?

The Atlas is therefore a product-level projection over accepted WGT/service facts, not decorative navigation and not a second architecture authority.

## 4. Node kinds

### 4.1 WGT Core

The central product/composition node.

It is primarily a spatial and conceptual anchor, not a dashboard full of controls.

Potential responsibilities:

- central visual anchor;
- initial camera focus;
- small WGT-level status summary when selected;
- entry to the deliberately small WGT-global settings set;
- semantic relation to known services.

The WGT Core does not need to contain service-specific settings or duplicate service dashboards.

### 4.2 Service node

A first-class WGT service/bounded-context presence.

Examples:

- Vocation;
- Illumination;
- Orientation;
- Conveyance.

A service node may expose high-level state such as:

- available;
- active/inactive;
- degraded/unreachable;
- incompatible;
- partially available;
- attention/update state if such semantics are later explicitly defined.

Selecting the node opens contextual information. Entering the node opens the service's complete WGT product surface.

### 4.3 Capability node

A user-relevant capability may appear as a child/satellite/port/module of its owning service.

Capabilities are useful when they help explain composition, activation or dependencies. The Atlas does not need to expose every internal application operation as a visible node.

Examples include:

- a Vocation spatial/map capability using Orientation;
- an Illumination study capability;
- Orientation Journey planning;
- future cross-device capability backed by an accepted Conveyance delivery scenario.

A capability node is a product read model. It does not imply that all capabilities share one plugin mechanism.

### 4.4 Future group/zone node

Do not group today's small service set merely for symmetry.

However, the scene/layout/navigation model must later support semantic hierarchy when service count makes a flat world unwieldy.

Possible future user-facing groups might include:

- Productivity;
- Learning;
- Spatial & Mobility;
- Infrastructure;
- other groupings discovered from the actual product catalog.

No group taxonomy is currently accepted.

Later semantic zoom may:

- collapse many services into high-level zones when zoomed far out;
- reveal services when entering a zone;
- reveal capabilities at a closer zoom level;
- allow search to jump directly through the hierarchy.

Groups are organizational presentation constructs, not automatically bounded contexts.

## 5. Connection model

Connections should communicate relationships that matter to the user.

Potential semantic connection kinds:

- WGT integration/presentation relationship;
- service ownership of a capability;
- capability composition dependency;
- required runtime dependency;
- optional enhancement dependency;
- local provider/read boundary;
- permission/resource dependency;
- cross-device/delivery relationship;
- unavailable/degraded/incompatible relationship.

The common model defines the meaning. Themes define whether that relationship appears as a precise edge, cable, road, bridge, light trail or another visual metaphor.

### Vocation + Orientation example

The Atlas should be able to show both of these truths simultaneously:

1. Orientation is an independent first-class WGT service with its own Discover / Explore / Navigate product space.
2. A Vocation capability may depend on Orientation for generic geospatial functionality.

The relationship should make clear that:

- Vocation owns opportunity/work-location/precision/job-market meaning;
- Orientation owns generic geospatial rendering/interaction and other generic spatial capabilities;
- WGT owns the composed product experience.

A service being used as a dependency does not reduce it to infrastructure and does not require its entire standalone product to be activated merely to satisfy one composed capability if the runtime semantics do not require that.

## 6. Root Atlas interaction

### 6.1 Spatial navigation

Desktop baseline:

- mouse/trackpad drag to pan;
- wheel/gesture zoom;
- keyboard directional navigation;
- accessible non-pointer navigation through semantic nodes;
- stable zoom limits;
- search/jump to avoid precision panning.

A small movement/control hint may live in the lower-right region.

On Desktop this can visually teach arrow/WASD, drag and zoom gestures. On phone it may become a compact circular movement affordance if that proves useful, while direct touch pan/pinch/tap remains first-class.

Do not require a permanent minimap, center button, zoom toolbar or other chrome unless testing establishes a real need.

### 6.2 Search/jump

A search/jump field near the top center is the primary persistent navigation instrument.

It may search:

- services;
- user-visible capabilities;
- later groups/zones;
- high-value provider destinations if the provider can publish/index them appropriately without leaking private content unexpectedly.

Selecting a result moves/focuses the Atlas to its target.

Search is essential to keeping a rich spatial interface efficient as the system grows.

### 6.3 Minimal WGT-global settings

WGT-global settings should be deliberately small and concern WGT itself.

Preferred interaction direction:

- one small circular Settings control in a corner;
- selecting it fans out a few nearby circular controls;
- selecting Theme can fan out theme choices laterally/around it;
- Sound and Effects/Reduced Effects may be separate controls if they are genuinely useful.

Service-specific configuration does not belong in a giant global WGT Settings page. It belongs with the relevant service/capability node or inside that service.

Do not add global Home, Back, Recent, Favorites, Refresh, Prompt, dependency-layer toolbar or similar controls merely because desktop software commonly has them.

A new global quick action must justify permanent Atlas chrome through a concrete frequent WGT-level workflow.

## 7. Node selection and contextual inspector

Selecting a node should reveal information in context without immediately leaving the Atlas.

Preferred behavior:

- selected node gains clear focus;
- relevant connected nodes/edges may highlight;
- unrelated scene content may de-emphasize slightly;
- an anchored panel/popover opens near the node where space permits;
- constrained layouts may use a stable overlay or bottom sheet.

A service inspector may expose tabs/sections such as:

### Overview

- service identity and human description;
- current availability/activation state;
- concise purpose;
- primary `Open`/`Enter` action.

### Capabilities

- important user-facing capabilities;
- availability;
- what they add;
- relevant composition relationships.

### Dependencies

- required relationships;
- optional enhancements;
- why another service is involved;
- missing/incompatible dependency explanation.

### Privacy & Data

Only explicit, defensible facts should be shown.

Potential facts:

- local authority;
- network use;
- whether data leaves the device;
- read/write direction;
- permission requirements;
- cross-device behavior;
- transport visibility/protection where formally known;
- retention/sensitivity facts where explicitly defined.

### Devices / Availability

- current device support;
- provider/runtime unavailable;
- unsupported platform;
- device-specific activation where such semantics are actually supported.

### Technical details

Diagnostics remain progressively disclosed rather than dominating normal usage:

- service identity;
- capability identity;
- contract version;
- provider/refresh state;
- technical failure details.

## 8. Activation and impact explanation

A major Atlas advantage is explaining what a new service/capability/connection changes before activation or update.

Where the underlying contracts/metadata support it, the UI should explain:

- capabilities gained;
- required dependencies;
- optional dependencies;
- local data introduced;
- network use;
- permissions;
- read/write behavior;
- cross-device implications;
- privacy/sensitivity implications;
- platform constraints.

This should answer the user-facing question:

> What am I bringing into my system by enabling this?

The Atlas must never fabricate privacy/security guarantees from theme art or inferred implementation details.

## 9. Entering a service: full product surfaces

Selecting a node and entering a service are distinct operations.

Node selection explains the service in the Atlas. `Open`/`Enter` transitions into the actual service product experience.

The intended visual transition can feel spatial rather than page-based: the camera focuses/zooms toward a service node and its product surface takes over the viewport.

The service surface should preserve the complete supported service product, not expose only a WGT-specific summary page.

### Return to WGT

When inside a focused service surface, a small WGT identity/core affordance may return to the Atlas.

This is semantically `Return to WGT`, not a generic browser-like Back stack.

Returning should preserve Atlas camera/selection context when practical so the system feels spatially continuous.

## 10. Embedded/full-service presentation boundary

Full-service parity introduces an important integration requirement that the existing WGT published-read slices do not solve by themselves.

WGT should avoid permanently rebuilding every provider's complete UI and workflow in the WGT repository.

Preferred direction to evaluate:

```text
WGT Atlas
    -> WGT service host
        -> explicit versioned provider presentation/application boundary
            -> provider-owned complete product surface
                -> provider-owned application/domain/runtime
```

WGT may own:

- host lifecycle;
- sizing and viewport composition;
- transition Atlas <-> service;
- platform host integration;
- outer focus/input lifecycle;
- loading/error boundary;
- WGT theme/effect tokens where the mechanism supports them;
- WGT identity/return affordance.

The provider may own:

- provider-specific product navigation;
- provider-specific interaction semantics;
- provider UI composition;
- domain/application semantics;
- provider-side accessibility semantics;
- provider presentation artifacts behind an explicit boundary.

WGT-native rendering remains valid for capabilities where data/command contracts are the cleaner integration mechanism. Provider-owned UI is not mandatory for every capability.

The architectural goal is **full product composition without duplicated domain/UI ownership**.

### Architecture-control-plane requirement

The existing cross-context integration policy permits WGT to integrate/present foreign capabilities while preserving domain ownership, but it does not yet define a common cross-context provider-UI contribution contract.

Before implementing a reusable provider-owned full-service presentation mechanism, the System Architecture Control Plane must determine whether such a common mechanism is warranted and define its system-level constraints.

Do not silently invent a shared UI/business library, import provider internals, or make one provider's current hosting technology the universal system contract.

Concrete service integrations may still use service-specific boundaries where justified.

## 11. First-class service expectations

### 11.1 Vocation

The current WGT Opportunity Overview/Map Projection integration is transitional.

Target WGT access should ultimately cover the Vocation product surface supported by Vocation itself, including current/future workflows such as:

- research planning/import/update;
- opportunity browsing/detail;
- personal assessment and explainable fit;
- Candidate Profile and Search Profiles;
- tracking/triage/availability/freshness;
- Groups and Application Waves;
- comparison;
- Vocation-owned map use and Orientation-backed spatial interaction;
- Application Cases;
- Application Materials and Documents;
- future application workflows when Vocation implements them.

WGT must not take ownership of those semantics.

### 11.2 Illumination

Illumination is a first-class WGT service, not hypothetical future decoration.

Its accepted direction already makes WGT the primary end-user presentation on Windows/iPhone while retaining an optional standalone/admin/dev host.

The WGT integration should eventually expose the complete supported Illumination product surface, including:

- Decks;
- Learning Items;
- Study Sessions;
- Reviews/scheduling;
- learning insights/history;
- generation/import workflows;
- lifecycle/content-management capabilities.

The exact WGT-facing presentation/application boundary still requires deliberate design.

### 11.3 Orientation

Orientation is an independent first-class WGT service and also a generic capability provider for other services.

The Orientation node should be able to lead to its own product space, including supported:

- Discover;
- Explore;
- Navigate;
- Current Location;
- place/geocoding workflows;
- route/journey/mobility capabilities as they mature.

Its dependency relationships with Vocation or future contexts remain visible independently of its standalone product presence.

### 11.4 Conveyance

Conveyance should become user-visible only to the extent concrete product scenarios require it.

Its node may initially be more infrastructural than Vocation/Illumination/Orientation, but any visible state must preserve its role as generic durable opaque delivery rather than inventing ownership of transported domain semantics.

## 12. Theme system

The same semantic scene should support multiple visual treatments.

### 12.1 Architecture / Technical

Likely first implementation.

Visual language:

- precise spatial graph;
- subtle dot/grid canvas;
- crisp nodes and ports;
- restrained dependency edges;
- status through shape/text/color rather than color alone;
- directional pulses where meaningful;
- modern system/CAD quality rather than hacker-terminal cliché.

This theme exposes the microservice/capability structure directly and can optionally reveal more technical detail than other themes.

### 12.2 Elegant / Minimal

A calmer interpretation:

- large negative space;
- refined typography;
- soft depth/translucency;
- fewer persistent labels;
- restrained connection animation;
- low visual noise.

### 12.3 Machine / Systems Engine

WGT appears as an engineered machine:

- WGT Core as central processor/control module;
- services as attached systems/modules;
- capabilities as ports/submodules;
- dependencies as conduits/cables/energy routes;
- activation propagates through relevant connections.

The mechanics remain visual metaphors over common semantics.

### 12.4 Miniature World / Living Atlas

A premium isometric/2.5D diorama interpretation.

Possible motifs:

- WGT — central hub/plaza/control structure;
- Vocation — city/business/opportunity district;
- Illumination — library/academy/observatory/knowledge garden;
- Orientation — cartography/navigation/mobility district;
- Conveyance — relay/bridge/signal/transport station.

Connections may render as:

- roads;
- bridges;
- rails;
- light trails;
- signal paths;
- moving packets.

The theme should feel alive but remain readable. Decorative world geometry must remain separate from semantic hit targets/node identity.

Prefer 2.5D/isometric presentation before unrestricted 3D.

### 12.5 Theme composition model

Themes should be assembled from common presentation primitives such as:

- world/background layer;
- node renderer;
- connection renderer;
- iconography;
- materials;
- typography accents;
- motion tokens;
- sound set;
- effect quality/LOD policy.

Changing theme must not alter service capability state or navigation semantics.

## 13. Sound, motion and feedback

Subtle feedback can make the Atlas feel responsive and alive.

Potential cues:

- node selection;
- capability activation;
- connection established/removed;
- unavailable/degraded state;
- entering/leaving a service;
- search jump/zoom transition.

Potential motion:

- node wake/sleep;
- connection pulses;
- selected-node emphasis;
- inspector unfolding from a node;
- camera travel after search;
- activation visibly propagating through a dependency.

Rules:

- sound can be disabled independently;
- reduced-motion/system accessibility preferences are respected;
- no critical state is sound-only or motion-only;
- no constant attention-grabbing animation;
- no casino/reward-loop behavior;
- animations never deliberately delay expert work.

Future phone implementations may add restrained haptics.

## 14. Desktop-first, mobile-equivalent strategy

### Desktop landscape first

Build the first complete high-quality Atlas on Windows/Desktop landscape.

Desktop may use:

- richer world detail;
- larger spatial composition;
- more ambient motion;
- higher effect quality;
- more simultaneous visible labels/details;
- larger anchored inspectors.

### Mobile-safe from the first implementation

Desktop design must avoid assumptions that later make mobile a rewrite:

- no hover-only essential action;
- touch-sized semantic hit areas;
- no right-click requirement;
- inspector can recompose to overlay/bottom sheet/fullscreen sheet;
- node labels remain readable at useful zoom levels;
- search/jump avoids precision navigation;
- scene geometry is independent of fixed desktop pixels;
- decorative effects support quality levels/LOD;
- service product surfaces can recompose for compact viewports;
- input abstraction supports pointer, keyboard and touch semantics.

### Phone landscape

When real Apple tooling/hardware is available, adapt the same Atlas semantics to the smaller landscape viewport.

Effects may reduce. Product capability parity remains.

### Phone portrait

Portrait should be intentionally composed rather than produced by shrinking desktop geometry.

Likely adaptations include:

- more aggressive scene LOD;
- contextual bottom-sheet inspector;
- compact search;
- radial/global controls repositioned around safe areas;
- reduced simultaneous node detail;
- service product surfaces using provider-specific responsive composition.

The same service/capability graph and product meaning remain underneath.

## 15. Accessibility

The Atlas cannot be pointer-only.

Required direction:

- semantic node collection exposed to platform accessibility/UI automation;
- logical traversal independent of visual coordinates;
- keyboard focus/navigation between nodes;
- search/jump direct navigation;
- inspector fully keyboard accessible;
- textual equivalent of important dependency/privacy relationships;
- color-independent states;
- reduced-motion/effects support;
- service product surfaces retain their own accessibility obligations.

A visual world is an enhancement, not a barrier to operating WGT.

## 16. Migration strategy

The current v0.6 shell should not be mistaken for the final IA, but it should remain intact until the Atlas replacement is actually usable.

### Phase A — Atlas semantic read model

Define WGT-owned presentation/application models such as:

- AtlasNode;
- AtlasConnection;
- node state/availability;
- hierarchy/group readiness without enabling grouping yet;
- dependency/privacy/platform metadata only where legitimately known;
- search index;
- selection/camera state.

Do not create a second system architecture database.

### Phase B — Atlas root surface

Implement the Technical/Elegant Desktop Atlas baseline:

- WGT Core;
- Vocation, Illumination, Orientation and Conveyance service nodes;
- meaningful current capability/dependency relationships;
- pan/zoom;
- centered search/jump;
- node selection;
- anchored inspector;
- sparse WGT-global Settings control;
- keyboard/accessibility path;
- movement hint.

Do not add permanent Home/Back/sidebar navigation.

### Phase C — in-context configuration and explanation

Move relevant integration/configuration understanding out of the giant Settings page and into service/capability inspectors:

- active/effective state;
- dependency explanation;
- privacy/data facts;
- device/platform availability;
- diagnostics where needed;
- activation/configuration actions where current WGT semantics support them.

### Phase D — full-service integration completeness

The Atlas must not ossify today's narrow Vocation contracts as the permanent product model.

Establish complete WGT entry paths for:

- Vocation;
- Illumination;
- Orientation.

For each service, decide the correct mix of:

- provider-owned embedded/full-service presentation;
- WGT-native presentation from provider contracts;
- service-specific host/application boundary.

Preserve domain ownership and full supported product capability parity.

### Phase E — reusable presentation-host decision

If multiple services genuinely require a common provider-owned presentation contribution model, return that requirement to the System Architecture Control Plane and define the smallest appropriate versioned host contract.

Do not generalize prematurely from Vocation's current WebView or Illumination's Avalonia technology.

### Phase F — theme renderer separation

Stabilize the common scene/theme contract.

Ship a high-quality Technical/Elegant baseline first, then implement Machine and Miniature World against the same semantics.

### Phase G — real mobile work

With Mac/Xcode/physical-device access:

- validate iPhone landscape;
- implement/validate portrait composition;
- validate full service capability parity;
- validate touch/haptics/accessibility;
- tune theme LOD/effects on iPhone 11-class hardware;
- do not claim runtime support before evidence exists.

## 17. Visual recommendation for the first Atlas

The first Atlas should be visually distinctive without depending on expensive world art.

Recommended Technical/Elegant baseline:

- dark neutral spatial canvas plus equivalent light treatment;
- WGT Core as a visually dominant central node;
- large service nodes with substantial breathing room;
- smaller capability satellites/ports only where informative;
- thin animated dependency links;
- centered floating search;
- one compact radial Settings control in a corner;
- compact movement hint in the lower-right region;
- node-anchored translucent inspector;
- camera transitions rather than page transitions while staying in the Atlas;
- spatial transition into complete service surfaces.

Once the interaction model is stable, the same world can become a premium miniature diorama with service-specific districts, paths and ambient detail.

## 18. What not to do

- Do not build another conventional Home page above the Atlas.
- Do not keep a permanent sidebar merely because desktop applications usually have one.
- Do not make the current Jobs list the definition of Vocation in WGT.
- Do not create a cut-down mobile edition.
- Do not treat Illumination as optional future decoration.
- Do not reduce Orientation to Vocation's map renderer.
- Do not expose every implementation dependency as a user-facing node.
- Do not make Orientation own the Atlas merely because it pans/zooms.
- Do not create service groups before scale warrants them.
- Do not force every provider into one UI technology.
- Do not duplicate foreign business logic/UI ownership in WGT.
- Do not make theme art authoritative for dependencies.
- Do not invent privacy/security claims from visuals.
- Do not add permanent global quick actions without a concrete high-frequency WGT-level need.
- Do not claim iPhone quality/support from desktop simulation.

## 19. Success criteria

The direction succeeds when WGT feels like a coherent personal system rather than a launcher or collection of reduced dashboards.

A user should be able to:

- see Vocation, Illumination, Orientation and other real services as parts of one system;
- understand important capability/dependency relationships spatially;
- inspect what activation adds and what it depends on;
- discover privacy/data/platform implications without opening developer diagnostics;
- enter the complete supported service product rather than a WGT-lite substitute;
- navigate efficiently through nodes or search;
- change the visual personality of the Atlas without changing its semantics;
- use the same product capability set on supported Desktop and phone platforms through platform-appropriate composition.

The desired character is architecture-native, lightweight at the WGT shell level, rich inside services, and optionally playful through themes.