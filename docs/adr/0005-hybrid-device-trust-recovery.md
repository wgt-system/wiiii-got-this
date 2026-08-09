# ADR-0005: Hybrid Device Trust Recovery

- Status: Accepted
- Date: 2026-08-09

## Context

Wiiii Got This V1 uses personal Device trust / explicit pairing without a mandatory conventional user account.

A recovery strategy is still required because:

- a Device may be lost, broken, reset, or reinstalled,
- the user may temporarily have only one trusted Device,
- losing every active Device must not irreversibly destroy access to the personal trust domain,
- possession/control of the personal server alone must not be sufficient to impersonate the user or enroll arbitrary Devices.

## Decision

Use a **hybrid recovery model**.

### Normal enrollment/recovery path

When at least one trusted Device remains available:

```text
new Device
    ↓
pairing request
    ↓
existing trusted Device approves
    ↓
new Device receives/establishes trust credentials
```

### Emergency recovery path

When no trusted Device remains available:

```text
new Device
    ↓
user supplies separately stored recovery material
    ↓
trust-domain recovery protocol
    ↓
new Device becomes recovery authority / re-establishes trust
```

The exact cryptographic format, number of words/characters, key-derivation scheme, and recovery UX are deliberately not fixed by this ADR.

## Server Trust Boundary

The personal server may store:

- trust-domain metadata,
- public Device credential material,
- revocation information,
- recovery protocol state that is safe to expose,
- encrypted synchronization envelopes.

The server must **not** be able to enroll a new trusted Device solely because an attacker has administrative access to the server.

Server control is not equivalent to ownership of the personal trust domain.

## Recovery Material

Recovery material must be:

- generated with sufficient entropy,
- exportable/storeable outside the ordinary Device set,
- usable without requiring another trusted Device,
- protected against accidental plaintext logging or repository inclusion.

The product should strongly encourage an offline or otherwise independently protected backup.

The V1 implementation must not rely on a memorized low-entropy password as the sole emergency trust root unless a later cryptographic ADR explicitly establishes a suitable password-hardening/recovery design.

## Device Loss

When a Device is lost:

1. revoke its Device credential as soon as possible,
2. prevent new synchronization deliveries to that credential,
3. retain historical Device records where useful,
4. rotate shared/per-service secrets if the chosen encryption model requires it.

The exact key-rotation implications are deferred until the synchronization encryption model is designed.

## Reinstallation

A reinstall creates fresh installation credentials.

The user may:

- enroll the new installation from another trusted Device, or
- use emergency recovery material if necessary.

Do not silently restore cryptographic trust from hardware fingerprints.

## Future Account System

If a future multi-user/public WGT adds account authentication, account recovery may become another recovery input.

That future system must not silently weaken the Device-trust model.

Possible future shape:

```text
User Account Recovery
        │
        ▼
Personal Trust Domain Recovery
        │
        ▼
Device Enrollment
```

This ADR does not require that future design.

## Consequences

### Positive

- convenient normal Device onboarding,
- survivable total Device loss,
- server compromise alone is insufficient for trust takeover,
- compatible with future end-to-end encrypted synchronization,
- does not require a central user account in V1.

### Cost

- emergency recovery material must be managed safely,
- real synchronization cannot ship until the recovery protocol and key hierarchy are concretely implemented and tested.

## Follow-up

Before production synchronization:

- define the cryptographic trust/key hierarchy,
- define recovery-material format and backup UX,
- define revocation and key-rotation behavior,
- threat-model server compromise and lost Devices,
- test recovery from zero remaining trusted Devices.
