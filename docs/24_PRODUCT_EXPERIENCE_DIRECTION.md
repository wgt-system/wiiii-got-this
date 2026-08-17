# Wiiii Got This – Product Experience Direction

## Status

This document records the proposed post-v0.6 product-experience direction for Wiiii Got This.

It is intentionally separated from the active v0.6.0 release candidate. It describes a product and interaction direction, not an already released implementation and not a second source of system-architecture authority.

System-wide ownership remains authoritative in `wgt-system/architecture`. WGT remains authoritative for its own product composition, device/platform integration and presentation semantics.

Tracked by GitHub Issue #46.

## 1. Why the current product should evolve

The current Desktop product has reached a useful conventional baseline:

- a persistent Home / Jobs / Map / Settings shell;
- a product-card Home surface;
- a searchable and sortable Vocation Jobs workspace;
- an Orientation-backed Vocation Map workspace with a WGT-owned inspector;
- user-facing Integration settings with progressively disclosed diagnostics;
- Fluent Light/Dark behavior;
- keyboard, focus, accessibility and compact-window hardening.

This is a good productivity baseline, but it does not yet make the distinctive nature of Wiiii Got This visible.

WGT is not merely a collection of pages. It is the product-composition context of a system of independently owned services and capabilities. The user should be able to understand, explore and operate that system as a coherent whole.

The proposed next experience is the **WGT Atlas**: a spatial, node-based product surface that turns the real service/capability relationships of WGT into an understandable and explorable interface.

## 2. Core idea: the WGT Atlas

The Atlas is a zoomable and pannable 2D system world.

At its conceptual center is a **WGT Core node**. Around it are service nodes such as:

- Vocation;
- Illumination;
- Orientation;
- Conveyance;
- future bounded contexts and integrations.

Capabilities may appear as child nodes, satellite nodes, ports, modules, districts or other theme-specific visual forms. Connections show meaningful composition or dependency relationships.

The user should be able to answer questions visually:

- What is installed or known to WGT?
- What is active on this device?
- Which capabilities does a service provide?
- What becomes available if I enable something?
- Which capability depends on another service?
- Why is something unavailable?
- Which data, permissions, network access or cross-device behavior does this introduce?
- Where can I open the actual workspace for this capability?

The Atlas is therefore not decorative navigation. It is a product-level read model over WGT integration/capability/availability state.

## 3. Semantic model first, themes second

The stable product model must be independent of its visual theme.

The same semantic Atlas should be renderable as:

- a technical architecture graph;
- an elegant minimal spatial canvas;
- a machine/system engine;
- a miniature living world;
- future visual themes.

Themes may change geometry, materials, animation language, iconography, ambience and visual metaphor. They must not change the meaning of nodes, connections, availability, dependencies or actions.

This distinction is the main scalability and maintainability rule:

> Node/connection semantics are product behavior. The world metaphor is presentation.

A theme must never require a separate set of domain rules.

## 4. Node model

Initial node kinds should remain deliberately small.

### WGT Core

The central product node.

Potential responsibilities in the Atlas:

- center/reset target;
- overview of this device and WGT health;
- entry point to global settings;
- current integration count and availability summary;
- search and navigation anchor;
- selected theme / presentation controls where appropriate.

### Service node

Represents a WGT-known service/integration/bounded context in user-facing product language.

Examples:

- Vocation;
- Illumination;
- Orientation;
- Conveyance.

A service node can show high-level state without exposing protocol details by default:

- available;
- enabled globally;
- enabled/disabled on this device;
- degraded/unreachable;
- incompatible;
- partially usable;
- update/attention state where such semantics later exist.

### Capability node

Represents a concrete capability that WGT can expose, compose or invoke.

Examples in the current system include:

- Vocation Opportunity Overview;
- Vocation Map Projection-based product surface;
- future Illumination study capability;
- future Orientation discovery/routing/current-location surfaces;
- Conveyance-backed capabilities only when a concrete WGT product scenario exists.

Capabilities must not imply a universal plug-in architecture. The visual node is a product read model, not proof that every capability uses the same runtime mechanism.

### Future group/zone node

The Atlas must be able to scale beyond a flat ring of services, but grouping should not be invented while the system is still small.

Future examples might include:

- Productivity;
- Learning;
- Spatial & Mobility;
- Infrastructure;
- Personal Data;
- other user-facing groupings.

No taxonomy is currently accepted. The requirement is only that layout/navigation semantics do not make later grouping impossible.

Possible future behavior:

- semantic zoom: zooming out collapses services into zones;
- zooming in reveals capabilities;
- search jumps through groups directly to a service/capability;
- groups can be user-facing organizational constructs without becoming bounded contexts.

## 5. Connection model

Connections should expose relationships that matter to a user.

Potential categories:

- WGT integration/presentation relationship;
- service -> capability ownership;
- capability composition dependency;
- required runtime dependency;
- optional enhancement dependency;
- local provider/read boundary;
- permission dependency;
- cross-device/delivery relationship;
- degraded/unavailable/incompatible relationship.

Connection rendering can be themed, but the meaning is common.

### Example: Vocation + Orientation

A Vocation Map experience can visually show:

- Vocation as the owner of opportunity/work-location/precision meaning;
- a Vocation map capability associated with Vocation;
- a capability connection to Orientation for generic geospatial rendering/interaction;
- WGT as the product composition owner.

Orientation does not have to be promoted as an enabled standalone top-level destination merely because one Vocation capability uses its renderer.

This is exactly the kind of relationship that is difficult to communicate through a traditional sidebar but natural in a node Atlas.

## 6. Primary Atlas interaction

### Spatial navigation

Desktop should support:

- mouse drag pan;
- trackpad pan;
- mouse wheel / gesture zoom;
- keyboard equivalent navigation;
- zoom in/out buttons for discoverability/accessibility;
- reset or `Center on WGT` control;
- stable zoom limits;
- optional minimap only if later usability testing justifies it.

The world may be effectively infinite or use a generous bounded scene. The user should never feel trapped inside a fixed dashboard grid.

### Search and jump

A persistent search/jump control is important as the system grows.

It should search product-visible objects such as:

- services;
- capabilities;
- later groups/zones;
- potentially user-facing actions where useful.

Selecting a result should animate/focus the Atlas to the target without requiring manual panning.

Search is also the main scalability escape hatch: even a visually rich Atlas remains efficient for expert use.

### Quick actions

Conventional controls remain desirable.

Potential floating or corner controls:

- search;
- center/reset;
- settings;
- prompt generator;
- recent/favorite capabilities;
- refresh where meaningful;
- theme switcher;
- dependency/privacy layer controls.

They may visually resemble game HUD quick slots, radial controls, floating glass buttons or restrained desktop controls depending on theme.

The Atlas must not force every operation through direct manipulation of nodes.

## 7. Node selection and anchored inspector

Selecting a node should normally reveal more information **in context**, not immediately navigate away.

Preferred pattern:

- selected node receives a strong but restrained focus treatment;
- connected relevant nodes/edges can be emphasized;
- unrelated content can dim slightly;
- an inspector appears anchored near the selected node or in a stable side/bottom region when space is constrained.

The inspector may use tabs or progressive sections.

Potential sections:

### Overview

- name;
- short human description;
- current state;
- what this service/capability does;
- primary `Open` or `Use` action.

### Capabilities

- capability list;
- availability;
- what each capability adds;
- direct open/use actions.

### Dependencies

- required relationships;
- optional enhancements;
- why another service appears in the graph;
- dependency unavailable/incompatible explanation.

### Privacy & Data

Only facts that WGT legitimately knows should be shown.

Potential facts:

- local-only authority where published/accepted;
- network use;
- data leaves device or does not;
- cross-device delivery;
- permission needs;
- read/write direction;
- sensitivity/retention information where explicitly defined;
- whether a transport sees plaintext where known by accepted contracts/architecture.

### Devices / Availability

- current device support;
- unsupported platform;
- provider unavailable;
- capability not composed on this device;
- later device-specific overrides.

### Diagnostics

Technical details remain available but secondary:

- service identity;
- capability identity;
- contract version;
- provider refresh state;
- technical failure details.

## 8. Activation and "what am I bringing into the system?"

One of the Atlas's strongest product opportunities is to explain activation or updates before they happen.

When a user enables/connects/updates something, the UI should be able to present a concise impact view:

- features/capabilities added;
- features removed or changed where later version semantics support this;
- required dependencies;
- optional dependencies;
- local data introduced;
- network use;
- permissions;
- cross-device behavior;
- privacy/sensitivity implications;
- unavailable platforms;
- estimated complexity only if an explicit product metric is later defined.

The UI must not invent security/privacy guarantees from visual inference. Facts must be derived from accepted WGT/system/provider metadata or remain unspecified.

## 9. Theme concepts

### 9.1 Architecture / Technical

The most direct expression of the product concept.

Visual language:

- dark or light precision canvas;
- subtle dot/grid background;
- crisp nodes;
- visible ports/connection anchors;
- animated directional data/availability pulses;
- compact labels;
- status encoded through shape + text + color, never color alone;
- dependency edges can visually resemble architecture diagrams without becoming raw developer diagrams.

Possible aesthetic references:

- high-end node editor;
- network operations visualization;
- circuit/graph design tool;
- modern CAD/system architecture rather than hacker-terminal cliché.

This should likely be the first implementation because it maps most directly onto the semantic model.

### 9.2 Elegant / Minimal

A calmer version for users who dislike visual density.

Visual language:

- large negative space;
- translucent or softly elevated nodes;
- subdued animated connections;
- fewer labels until selection/zoom;
- typography carries hierarchy;
- refined spatial easing;
- very restrained glow/depth.

This could become the default if the technical theme feels too developer-oriented.

### 9.3 Machine / Systems Engine

WGT appears as a functioning engineered system.

Visual metaphors:

- WGT Core as central processor/reactor/control hub;
- services as attached modules or machines;
- capabilities as ports/submodules;
- dependencies as conduits/cables/belts/energy lines;
- active capabilities visibly "power" relevant routes;
- degraded relationships flicker or idle rather than turning into noisy error animations.

This theme can feel game-like without becoming a literal game.

Important constraint: mechanics are visual metaphors only. No semantic dependency may exist only because a theme renders a gear or conduit.

### 9.4 Miniature World / Living Atlas

The most playful theme.

Instead of abstract graph nodes, each service occupies a small recognizable zone or structure.

Possible metaphors:

- **WGT Core** — central plaza/control tower/hub;
- **Vocation** — compact city/business district, skyline, notice board or opportunity terminal;
- **Illumination** — library, observatory, academy, archive or luminous knowledge garden;
- **Orientation** — cartography tower, terrain station, compass observatory or mobility hub;
- **Conveyance** — relay station, bridge network, courier terminal or signal tower.

Connections become:

- roads;
- bridges;
- light trails;
- rails;
- signal links;
- flowing packets;
- paths between districts.

A capability can appear as a building/module within its service district.

The world should remain stylized rather than photorealistic. The strongest direction is likely a premium isometric/2.5D miniature diorama with controlled depth and clear labels.

Risks:

- visual metaphor can overpower usability;
- too many decorative elements reduce scanning speed;
- 3D can complicate hit testing/accessibility/mobile performance;
- literal scenery can make new services difficult to place consistently.

Mitigation:

- keep the semantic node positions/hit boxes independent of decorative art;
- allow labels and inspector to remain conventional;
- use 2.5D/isometric art rather than unrestricted 3D initially;
- support reduced-effects mode;
- retain search/jump and keyboard navigation.

### 9.5 Theme composition rather than total replacement

Themes may share interaction primitives while varying ambience:

- node shell;
- connection renderer;
- background/world layer;
- iconography;
- sound set;
- motion tokens;
- surface materials;
- typography accents.

This avoids rewriting the Atlas for each visual concept.

## 10. Sound, motion and stimulation

High-quality feedback can make the Atlas feel alive.

Potential sound cues:

- subtle node focus/select;
- capability activation;
- successful connection;
- dependency unavailable;
- opening an embedded workspace;
- zoom threshold/zone transition where useful.

Motion concepts:

- connection pulses;
- node wake/sleep transitions;
- smooth camera travel after search;
- small hover/focus response;
- inspector unfolding from the selected node;
- capability activation visually propagating across the dependency edge.

Rules:

- sounds are optional and independently disableable;
- system accessibility preferences and reduced motion are respected;
- no critical information is sound-only/motion-only;
- no constant attention-grabbing animation;
- no variable-reward/casino-style loops;
- animations must not delay expert workflows.

Future mobile hosts may map selected events to restrained haptics.

## 11. Embedded capability experience

The Atlas is most valuable if it can open capabilities without making every service feel like an unrelated external application.

WGT should therefore define an **embedded capability presentation convention**.

This is not one universal UI framework and not shared business-domain code.

WGT may own:

- host surface/chrome;
- title/navigation region;
- sizing/responsive rules;
- theme tokens where the embedding mechanism supports them;
- lifecycle/focus/back behavior;
- loading/error boundary;
- platform-specific host integration;
- transition from Atlas node into focused workspace and back.

The provider may own:

- provider-specific interaction semantics;
- provider-specific UI contribution where appropriate;
- provider-owned presentation artifact/code behind an explicit versioned boundary;
- provider-side accessibility semantics within its contribution.

Alternatively, the provider may publish only data/commands and WGT may render a native presentation, as Vocation already demonstrates.

Standalone provider applications remain valid for rich/admin/specialist workflows.

The goal is coherent composition, not forced UI centralization.

## 12. Atlas vs real-world maps

The WGT Atlas is **not a geospatial map**.

Pan/zoom/scene interaction does not make it Orientation-owned.

Ownership remains:

- WGT — system/product composition, service/capability navigation, device/platform presentation;
- Orientation — real-world generic geospatial capability;
- Vocation — job-market semantics;
- Illumination — learning semantics;
- Conveyance — generic durable opaque delivery.

The Atlas should be implemented as a WGT presentation/application read model, not by repurposing Orientation's geospatial renderer.

## 13. Desktop-first implementation strategy

The first real Atlas should be a complete, high-quality Desktop landscape experience.

This is compatible with the current development reality: Windows is the validated runtime and no real Mac/Xcode/iPhone environment is currently available.

Implementation order:

1. Desktop landscape Atlas;
2. Desktop usability/performance/accessibility hardening;
3. later compact landscape adaptation on real mobile hardware;
4. dedicated phone portrait composition during the real mobile phase.

The Desktop implementation must already avoid creating mobile blockers.

### Mobile-safe constraints from day one

- no hover-only operation;
- practical touch-size hit targets;
- inspector can collapse or become bottom sheet/fullscreen sheet;
- node labels remain legible at bounded zoom levels;
- search/jump works without precision panning;
- no fixed side panel that assumes desktop width;
- no tiny connection endpoints required for essential actions;
- effects can be reduced substantially;
- scene complexity can be level-of-detail controlled;
- avoid assumptions that require a mouse/right click;
- layout engine can reflow/collapse hierarchy rather than relying on fixed pixel coordinates.

An iPhone 11-class performance target is a useful lower-end design constraint even before real device validation. Actual support claims still require a real Apple runtime test.

## 14. Accessibility model

The Atlas cannot be pointer-only.

Potential accessibility structure:

- semantic node collection exposed to UI Automation;
- logical traversal order independent of visual coordinates;
- keyboard next/previous node traversal;
- search/jump as direct navigation;
- selected node inspector fully keyboard accessible;
- equivalent textual dependency descriptions;
- high-contrast/reduced-motion themes;
- color-independent state indicators;
- conventional fallback navigation/workspaces remain available during migration.

The graphical world should enhance understanding, not become a barrier.

## 15. Migration from the current shell

Do not delete the current useful workspaces first.

### Phase A — Atlas read model

Define presentation/application read models for:

- AtlasNode;
- AtlasConnection;
- node state/availability;
- optional metadata suitable for dependency/privacy/device explanations;
- search index;
- selection state.

Derive from existing WGT Service Integration / Capability / Availability semantics where possible.

Do not create a duplicate authoritative service graph.

### Phase B — technical Atlas surface

Implement:

- WGT Core;
- current service nodes;
- current capability nodes where user-facing;
- pan/zoom;
- search/jump;
- selection;
- anchored inspector;
- dependency/availability edges;
- open existing Home/Jobs/Map/Settings/workspace actions;
- keyboard/accessibility equivalents.

The current conventional shell can coexist during this phase.

### Phase C — configuration in context

Move or duplicate appropriate current Settings actions into the selected service/capability inspector:

- enable globally;
- device override;
- effective state;
- refresh;
- health;
- diagnostics;
- open capability.

Settings may remain as a full administrative surface.

### Phase D — impact/privacy/dependency explanations

Add user-facing explanations only from accepted facts.

The Atlas can then become the natural place to answer "what do I unlock / what does this depend on / what does this change?"

### Phase E — theme renderer separation

Stabilize a renderer/theme contract.

Ship one strong technical/elegant baseline before building expensive art-heavy themes.

Then introduce Machine and/or Miniature World themes against the same semantic scene.

### Phase F — embedded capability convention

Define and validate the common WGT host behavior for embedded/provider-native capability surfaces.

Adopt selectively.

### Phase G — mobile

On real Apple tooling/hardware:

- validate compact landscape;
- design portrait hierarchy;
- map anchored inspector to appropriate sheet/panel behavior;
- validate touch, haptics, performance and accessibility;
- preserve semantic equivalence without forcing identical geometry.

## 16. Visual design recommendation

The strongest development path is **technical first, world-ready underneath**.

Recommended first visual baseline:

- dark neutral spatial canvas plus Fluent-compatible light equivalent;
- WGT Core as visually dominant central node;
- services arranged with ample spatial breathing room;
- capability satellites/ports;
- thin animated dependency links;
- floating search at top center;
- small circular quick controls in corners;
- selected-node glass/Fluent inspector anchored near the node when space allows;
- camera transitions rather than page transitions for Atlas navigation;
- open focused workspaces as layered/full product surfaces with a clear `Back to Atlas` path.

This already feels unique and architecture-native without requiring bespoke world art.

Once interaction is stable, the same layout can become a **premium miniature diorama** theme:

- WGT central hub;
- service districts around it;
- capability buildings/modules;
- routes/light trails carrying dependency meaning;
- soft ambient motion;
- clean labels suspended above districts;
- selected district subtly lifts/brightens and opens the inspector;
- distant zones simplify at lower zoom levels.

This approach captures the playful vision without making the first implementation dependent on expensive illustration or 3D engineering.

## 17. What not to do

- Do not turn the Atlas into a literal copy of the C4/system architecture diagram.
- Do not expose every technical dependency to normal users by default.
- Do not make Orientation own the Atlas.
- Do not make service categories/buckets mandatory before scale demands them.
- Do not require one common provider UI framework.
- Do not reimplement foreign business semantics in WGT just to make a node interactive.
- Do not make theme art the source of truth for capability relationships.
- Do not remove search, keyboard paths or conventional actions for visual purity.
- Do not claim mobile quality from desktop simulation alone.

## 18. Success criteria

The direction is successful when WGT can become both:

1. an efficient host for focused work; and
2. an explorable representation of the user's personal WGT system.

A user should be able to understand and operate the system without reading architecture documentation, while an advanced user can deliberately reveal technical relationships.

The experience should feel architecture-native, coherent and distinctive. Playfulness should be selectable through themes rather than baked into the product semantics.
