# Wiiii Got This – Identity, Device Trust, and Enrollment Options

## Status

Accepted V1 direction:

- Option B — personal Device trust / pairing without a mandatory user account,
- hybrid recovery — trusted Device approval normally, separate recovery material in an emergency.

See:

- `docs/adr/0004-v1-personal-device-trust.md`
- `docs/adr/0005-hybrid-device-trust-recovery.md`

A future public/multi-user product may evolve toward account + Device trust.

## 1. Why This Decision Is Needed

Wiiii Got This must support optional synchronization between Windows and iPhone through always-available infrastructure.

That creates several distinct questions:

- Who is allowed to connect to the synchronization infrastructure?
- How does a new Device become trusted?
- How is a Device distinguished from an attacker pretending to be that Device?
- How can a lost Device be revoked?
- How can the user recover after reinstalling a Device?
- Who may decrypt synchronized payloads?
- Does the system require a conventional online user account?

These are not the same as the ordinary WGT `Device` concept.

## 2. Concepts That Must Remain Distinct

### User

The human owner of the personal system.

WGT is initially single-user, so a rich multi-user domain model is not required.

### Device

A user-recognizable WGT installation/device used for configuration and capability resolution.

### Installation Identity

A technical identity associated with one installed WGT instance.

It may be replaced on reinstall.

### Device Credential

A cryptographic credential proving that a particular installation is trusted to communicate.

### Trust Enrollment

The explicit process by which a new Device/installation obtains trusted credentials.

### Recovery

The process used when existing trusted credentials are unavailable or a Device is reinstalled/replaced.

These concepts must not be collapsed merely to simplify persistence.

## 3. Option A – Conventional Central User Account

### Shape

```text
WGT Windows ──┐
              ├── login → central auth/account service
WGT iPhone ───┘
```

The user signs in with a conventional account credential.

The server uses the account identity to authorize Devices and synchronization.

### Advantages

- familiar UX,
- straightforward account recovery,
- easier future multi-user expansion,
- established OAuth/OIDC-style tooling is available.

### Disadvantages

- introduces an account/auth service before the product otherwise needs one,
- creates password/passkey/recovery/email/security administration,
- increases cloud/server centrality,
- may imply more remote identity data than a personal single-user system needs,
- does not by itself provide end-to-end encryption of service payloads.

### Assessment

Reasonable for a public SaaS product, but potentially oversized for the current personal architecture.

## 4. Option B – Personal Device Trust / Pairing

### Shape

```text
Trusted Personal System
        │
        ├── Windows Device credential
        ├── iPhone Device credential
        └── personal Sync/Relay trust authority
```

A new Device is explicitly enrolled into the personal trust set.

Possible enrollment UX:

- one-time code,
- QR code,
- approval from an already trusted Device,
- recovery/enrollment secret controlled by the user.

After enrollment, the Device receives or creates its own cryptographic credential.

### Advantages

- matches a single-user personal system,
- no mandatory conventional username/password account,
- clean Device revocation,
- local-only mode remains completely independent,
- pairs naturally with a personal server,
- allows the relay to authenticate Devices without becoming owner of foreign domain data,
- supports future end-to-end encrypted service payloads.

### Disadvantages

- recovery must be deliberately designed,
- initial setup is less familiar than ordinary login,
- losing all trusted Devices/keys without recovery material can be serious,
- key lifecycle and rotation must be implemented carefully.

### Assessment

**Preferred V1 direction.**

It matches the current product more closely than introducing a general account system.

## 5. Option C – Account Plus Device Pairing

### Shape

A conventional user account identifies the owner, while each Device also has a distinct cryptographic credential and explicit enrollment lifecycle.

### Advantages

- strongest conventional recovery story,
- clean Device-level revocation,
- easier future hosted/multi-user operation,
- account identity and Device trust remain separate.

### Disadvantages

- highest initial complexity,
- requires both account infrastructure and Device-key infrastructure,
- probably premature for a personal V1.

### Assessment

Architecturally strong for a larger/public product, but likely excessive now.

## 6. Recommended V1 Direction

Adopt **Option B – Personal Device Trust / Pairing**.

Core principle:

> Synchronization is enabled for a personal trust domain made of explicitly enrolled Devices. A conventional online user account is not required for V1.

Conceptually:

```text
Personal Trust Domain
├── Trust / Recovery Authority
├── Windows
│   └── Device Credential
├── iPhone
│   └── Device Credential
└── future Devices
```

The synchronization relay authenticates the Device/installation credential.

It does not infer trust from:

- IP address,
- LAN presence,
- hostname,
- Service discovery,
- WGT Device display name.

## 7. Enrollment

A new Device must not become trusted automatically.

Recommended semantic flow:

1. install/start WGT,
2. WGT creates a fresh installation/device identity and local key material,
3. user explicitly begins enrollment,
4. an existing trusted authority approves the enrollment through a short-lived enrollment mechanism,
5. the new Device receives/establishes the credentials needed for the personal trust domain,
6. the Device becomes eligible for synchronized Services according to their own policies.

The concrete QR/code/protocol is an implementation/security decision.

## 8. Personal Server Role

When synchronization is enabled, the personal server is a natural always-available location for:

- Sync/Relay,
- enrollment rendezvous,
- trusted Device registry / public credential information,
- revocation information,
- optional encrypted synchronization storage.

The server does not automatically receive the right to decrypt service-owned domain payloads.

The server's exact trust role must be explicit.

## 9. End-to-End Encryption Direction

Preferred security direction:

> Sync/Relay should be able to transport/store service-owned payloads without needing to understand their plaintext contents.

This suggests:

```text
Service Sync Adapter
    ↓ encrypt/authenticate
opaque envelope
    ↓
Sync / Relay
    ↓
opaque envelope
    ↓ decrypt/verify
Service Sync Adapter
```

However, the exact key hierarchy is deliberately deferred until the first real synchronized Service contract.

Potential designs include:

- trust-domain shared data-encryption keys,
- per-Service keys,
- per-Device key wrapping,
- service-specific key derivation.

Do not choose one before the Illumination sync semantics are concrete.

## 10. Server Compromise Model

The target should distinguish:

### Transport/auth metadata the relay must know

Potentially:

- Device routing identity,
- Service stream identity,
- delivery state,
- envelope size/timestamps,
- sequence/correlation metadata.

### Foreign payload the relay need not know

Potentially:

- Learning Item content,
- Reviews,
- Vocation personal assessments,
- other sensitive domain state.

The amount of visible metadata should be minimized where practical.

## 11. Revocation

The architecture must support removing trust from a Device.

After revocation:

- the Device may remain a historical WGT Device record,
- it must not receive new synchronized payloads,
- server-side credentials/tokens are rejected,
- future key rotation may be required depending on the encryption model.

`retired` and `revoked` are not necessarily the same state.

WGT domain lifecycle and cryptographic trust lifecycle should remain distinct.

## 12. Reinstallation

Recommended consequence of Option B:

A fresh installation creates a new installation identity and credential.

The user may:

- retire/revoke the old installation,
- enroll the new installation,
- restore synchronized service state according to each Service's rules.

Do not silently recover trust from hardware fingerprints.

A future explicit recovery workflow may make this smoother.

## 13. Recovery

V1 uses hybrid recovery.

### Normal path

An already trusted Device explicitly approves/enrolls the new Device.

### Emergency path

Separately stored recovery material can re-establish the personal trust domain when no trusted Device remains.

The personal server alone is not a recovery authority.

Exact cryptographic format, key hierarchy, and recovery UX are deferred until production synchronization security is designed.

## 14. Local-Only Operation

Identity/Trust infrastructure is required only for remote/multi-device synchronization and other protected remote capabilities.

A local-only WGT + local Service must remain usable without:

- central account,
- Sync/Relay,
- remote login,
- remote Device enrollment.

## 15. Future Public/Multi-User Product

If Wiiii Got This later becomes a multi-user/publicly hosted product, conventional account identity may be added.

That would not require discarding Device credentials.

A future model may evolve toward Option C:

```text
User Account
    └── enrolled trusted Devices
```

This is not required for V1.

## 16. Accepted Decision

V1 uses personal Device trust/pairing without a mandatory account plus hybrid recovery.

Future evolution to account + Device trust remains explicitly supported.
