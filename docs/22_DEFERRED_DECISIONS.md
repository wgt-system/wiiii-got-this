# Wiiii Got This – Explicitly Deferred Decisions

## Status

These decisions are intentionally **not** required before repository bootstrap.

They must not be silently invented by implementation agents.

## 1. Illumination Synchronization Contract

Illumination's WGT-primary-presentation/runtime architecture is now accepted in its own repository.

The concrete synchronization contract remains deferred until Illumination establishes:

- which learning state must exist on iPhone,
- which operations may occur offline,
- authoritative state model,
- change identity,
- merge/conflict semantics,
- replicated persistence shape.

WGT and Sync/Relay must consume Illumination-owned semantics rather than invent them.

## 2. Synchronization Cryptography and Key Hierarchy

The target properties are accepted:

- explicit Device trust,
- revocation,
- hybrid recovery,
- server control alone cannot take over trust,
- relay should be able to transport opaque/encrypted foreign payloads where appropriate.

Accepted/gated interoperability profile:

- Conveyance owns the provider-specific v0.2 Security Interoperability Profile;
- Go and Windows interoperability evidence is complete;
- the physical iPhone interoperability gate remains open;
- Production Security is not approved before the complete ADR-0007 gate.

Still production-deferred:

- production enrollment,
- production revocation/recovery implementation,
- production payload integration,
- final production key handling/nonce integration and other production-only details.

These remain provider-owned readiness items and do not mean that no interoperability profile
exists.

## 3. Vocation Published Contract Status

Vocation's `Published Opportunity Overview 1.0` is implemented on its `dev` branch and is the first real WGT Windows integration. Its canonical schema is `schemas/published-opportunity-overview-v1.schema.json`, and its local read-only endpoint is `/published/v1/opportunity-overview`.

It is client-neutral, versioned, and read-only. It excludes personal state, Availability/Freshness, URLs/navigation, maps, comparison, and opportunity detail. It is not a Vocation database, domain-class, or internal React API dependency.

Vocation's `Published Map Projection 1.0` is likewise accepted and implemented on `dev`
through its provider-owned schema and publication boundary.

Later Vocation contracts remain separate and deferred, including Opportunity Detail,
Groups/Waves, and Availability/Freshness.

Do not build a generic Vocation API inside WGT.

## 4. First Illumination WGT Interaction Contract

Illumination has confirmed WGT as its primary Windows/iPhone end-user presentation and permits local in-process capability hosting behind explicit Illumination-owned boundaries.

The concrete interaction contract remains deferred until the relevant Illumination application capabilities are stable.

Likely pressure includes:

- start/continue study interaction,
- current interaction state,
- submit learner action/assessment,
- next interaction,
- statistics/read views,
- later synchronization.

WGT must not infer scheduling semantics.

## 5. Capability Taxonomy

The generic concept `Capability` is accepted.

A universal taxonomy is not.

Do not invent categories such as:

- ReadCapability,
- CommandCapability,
- ScreenCapability,
- BackgroundCapability,
- MobileCapability,

as permanent domain types until repeated concrete integrations prove that these distinctions matter.

## 6. Generic Requirement Schema

Capability Resolution needs requirements, but the universal schema is deferred.

Only add requirement dimensions needed by concrete Capabilities.

Examples that may later matter:

- local runtime required,
- network required,
- specific interaction feature,
- synchronized replica required,
- camera/keyboard/background capability.

Avoid a general-purpose policy language in V1.

## 7. Generic Service Registry

V1 uses explicit shipped Integration Adapters and adapter-specific provider location/publication.

A generic Registry is deferred until it gains its own independent lifecycle and concrete consumers.

## 8. Web Client

A WGT web client remains optional.

Server/Sync infrastructure does not imply a browser UI.

Add web only for a concrete product use case.

## 9. Public/Multi-User Accounts

V1 is personal/single-user and uses Device trust without mandatory accounts.

If WGT later becomes a public/multi-user product, evaluate:

- account identity,
- authentication provider,
- authorization,
- hosted tenancy,
- account recovery,
- relationship to existing Device trust.

Do not build custom auth preemptively.

## 10. Mac Build-Host Provider

A Mac/Xcode build host is required before real iPhone deployment/signing.

The exact operational choice is deferred:

- own Mac mini,
- another accessible Mac,
- hosted macOS environment,
- CI runner.

This does not block WGT Domain/Application/bootstrap development on Windows.

## 11. Production Distribution

Deferred:

- TestFlight,
- App Store,
- ad-hoc development distribution,
- Windows installer/package format,
- release signing,
- update channel.

These are required before external release, not before architecture/bootstrap.

## 12. Future Additional Bounded Contexts

Standing hypotheses such as:

- additional geospatial contracts beyond the accepted Orientation boundary,
- future account/auth context,
- Vault,
- other services,

must not be created inside the WGT repository without a concrete domain case.

Synchronization / Relay is the only additional context already accepted by WGT architecture.

## 13. Rule for Agents

If implementation encounters one of these deferred decisions:

1. do not invent a permanent solution,
2. implement only a fake/reference seam if the current slice requires it,
3. document the blocked decision,
4. return to the control-plane chat before changing the architecture.
