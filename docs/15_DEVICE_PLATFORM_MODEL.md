# Wiiii Got This – Device and Platform Model

## Status

Accepted V1 modeling baseline for Device identity/lifecycle direction.

See `docs/adr/0004-v1-personal-device-trust.md` for the accepted trust model.

Exact enrollment, recovery, and cryptographic protocol details remain unresolved.

## 1. Why Device Needs Domain Identity

Wiiii Got This needs a stable Device reference for:

- Device-specific Service Integration overrides,
- capability resolution,
- diagnostics,
- future synchronization routing,
- future trust/revocation.

A raw operating-system identifier, hostname, network address, or hardware serial number is not a suitable domain identity.

## 2. V1 Device Definition

For V1, `Device` means:

> A Wiiii Got This installation on a user-recognizable computing device, with a WGT-owned stable Device Identity and user-facing name.

Examples:

- `Windows PC`
- `iPhone`

The physical hardware itself is not modeled as an aggregate.

This avoids relying on privacy-sensitive or unstable hardware identifiers.

## 3. Device Identity

Device Identity is generated/assigned by Wiiii Got This.

It must not be derived solely from:

- hostname,
- IP address,
- Apple hardware identifier,
- Windows machine identifier,
- account name,
- OS installation identifier.

The exact identifier encoding is an implementation detail.

A random opaque identifier is the expected direction.

## 4. Reinstallation

A fresh WGT installation normally receives a new installation/device identity and fresh credentials.

An explicit recovery/re-enrollment workflow may restore synchronized state and trust-domain participation without relying on hardware fingerprinting.

Consequences:

- an old Device may later be retired/revoked,
- a reinstall does not silently inherit old trust credentials,
- future Device recovery can be designed explicitly rather than inferred from hardware fingerprinting.

This is particularly important for synchronization/security.

## 5. Device Lifecycle

Initial lifecycle:

```text
known/active
    ↓
retired
```

Additional lifecycle states such as:

- lost,
- revoked,
- pending trust,
- replaced,

belong to later Identity/Trust work and are not required for the WGT Core model.

## 6. Device Name

Device has a user-facing name.

The name:

- is mutable,
- is not identity,
- may be suggested from platform information,
- must not be used as a synchronization key.

## 7. Device Integration Overrides

Service Integration enablement remains layered:

```text
global Service Integration state
            │
            ├── no Device override → inherit
            │
            └── Device override    → enabled/disabled
```

Overrides reference stable WGT Device Identity.

A Device rename does not change override identity.

## 8. Platform Model

`Platform` is not a separately managed aggregate.

For V1, capability resolution receives a `Platform Context` / `Client Environment` value describing the current WGT presentation/runtime environment.

At minimum, the first supported client environments are:

- Windows desktop,
- iPhone.

The implementation may represent this with structured values rather than a single string.

## 9. Platform Dimensions

Potentially relevant dimensions include:

- OS family/version,
- form factor,
- WGT client type/version,
- available platform features,
- runtime capabilities,
- presentation capabilities.

Only dimensions required by a concrete Capability should enter resolution logic.

Do not build a generic hardware inventory.

## 10. Platform Requirements

A Capability or Presentation Contribution may declare requirements against supported integration features.

Prefer capability-relevant requirements over hard-coded assumptions such as:

```text
if iPhone then impossible
```

For example, a future interaction may require:

- keyboard-oriented editor support,
- background execution,
- camera,
- secure local storage,
- network access,
- local capability runtime support.

The provider/presentation adapter must define the semantics that matter.

## 11. Windows and iPhone Are Not Separate Bounded Contexts

The architecture remains:

```text
Wiiii Got This bounded context
        │
        ├── Windows presentation/platform adapters
        └── iPhone presentation/platform adapters
```

Differences in UI, OS APIs, lifecycle, signing, notifications, or background execution do not create new business domains.

## 12. Future Browser Client

A future web client would add another Client Environment.

It would not require another Wiiii Got This bounded context.

Browser limitations would participate in Capability/Presentation Resolution.

## 13. Future Security Pressure

Synchronization/trust may later require a distinction between:

- user-recognizable Device,
- installation identity,
- cryptographic Device credential.

V1 intentionally does not merge these concepts into one security model.

If the security model requires separate identities, the domain language must be refined rather than overloading `Device`.
