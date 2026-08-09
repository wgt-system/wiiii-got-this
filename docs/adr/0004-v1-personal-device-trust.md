# ADR-0004: V1 Personal Device Trust Without Mandatory User Account

- Status: Accepted
- Date: 2026-08-09

## Context

Wiiii Got This is initially a personal, single-user system.

Its first synchronized Devices are:

- the user's Windows PC,
- the user's iPhone.

Cross-device synchronization requires authenticated and revocable Device trust, but a conventional multi-user account system would introduce substantial infrastructure that is not needed for the initial product.

A future public or multi-user version may still need account identity, authentication, authorization, recovery, and account administration.

The architecture must therefore solve V1 Device trust without blocking a later evolution toward an account-plus-device model.

## Decision

V1 uses **personal Device trust / explicit Device pairing**.

A conventional central user account is **not mandatory in V1**.

Each WGT installation has:

- a WGT-owned Device / installation identity,
- local cryptographic Device credentials,
- explicit enrollment into the user's personal trust domain when synchronization or protected remote capabilities are enabled.

A new Device does not become trusted merely because it is:

- on the same LAN,
- discovered,
- configured with the same server,
- named similarly,
- reachable over the network.

Trust requires explicit enrollment.

## Enrollment Direction

The semantic flow is:

```text
new WGT installation
    ↓
fresh Device / installation identity
    ↓
fresh local Device credential
    ↓
explicit pairing / enrollment
    ↓
personal trust domain
    ↓
eligible for configured synchronization
```

The concrete mechanism may use:

- QR code,
- one-time enrollment code,
- approval from an already trusted Device,
- recovery/enrollment material.

The exact protocol and UX are deferred.

## No Mandatory Account

Local-only operation remains possible without:

- online account,
- central login,
- Sync / Relay,
- remote enrollment.

When the user does not use remote/multi-device features, WGT and local Services remain usable locally.

## Future Evolution to Account + Device Trust

The target evolution path is:

```text
V1
Personal Trust Domain
└── trusted Devices

Future public/multi-user model
User Account
└── Personal Trust Domain
    └── trusted Devices
```

A future account system may add:

- user identity,
- authentication,
- authorization,
- account recovery,
- hosted service ownership,
- multi-user separation.

The existing per-Device credentials and revocation model remain useful.

The future account/auth layer may be:

- a dedicated bounded context/service,
- infrastructure backed by a standard identity provider,
- another explicitly selected architecture.

This ADR does not require building custom authentication infrastructure.

## Revocation

A trusted Device must be revocable.

Revocation affects cryptographic/remote trust, not necessarily the historical WGT Device record.

Therefore distinguish:

- WGT Device lifecycle,
- trust credential lifecycle.

A Device may remain known while its remote synchronization credentials are revoked.

## Reinstallation

A fresh WGT installation normally receives a new installation/Device identity and fresh credentials.

The user may then:

- enroll the new installation,
- retire/revoke the old one,
- restore synchronized service state according to each Service's rules.

Do not silently recover trust using hardware fingerprints.

## Recovery Requirement

V1 must not create a design where losing one Device necessarily destroys the whole personal trust domain.

At least one explicit recovery path must exist before real synchronization ships.

Possible mechanisms include:

- approval by another trusted Device,
- separately stored recovery material,
- protected administrative recovery through the personal server.

The exact recovery mechanism is deferred to a later security ADR.

## Encryption Direction

The preferred target is that Sync / Relay can authenticate routing/delivery while service-owned payloads may remain opaque/encrypted to the relay.

The exact key hierarchy is not selected here.

Each Service retains its own data-locality and synchronization policy.

## Consequences

### Positive

- matches the actual V1 single-user product,
- avoids premature account infrastructure,
- supports Device revocation,
- fits a personal server,
- remains compatible with future end-to-end encrypted synchronization,
- provides a clean evolution path toward account + Device trust later.

### Trade-offs

- enrollment and recovery UX must be designed,
- key lifecycle must be implemented carefully,
- future public/multi-user operation still requires a separate account/auth decision.

## Rejected Alternatives

### Conventional central account only

Rejected for V1 as unnecessary infrastructure for a personal single-user system.

### Account plus Device trust in V1

Architecturally strong but rejected as premature complexity.

It remains the preferred likely future direction if Wiiii Got This becomes a public or multi-user product.
