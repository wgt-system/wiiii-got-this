# Architecture Decision Records

Accepted decisions:

- `0001-wgt-client-technology-stack.md`
  - C# / .NET 10
  - Avalonia 12
  - Windows + iPhone
  - SQLite for WGT-owned local state
  - no mandatory WGT server
  - web optional

- `0002-synchronization-relay-context-boundary.md`
  - generic Synchronization / Relay is a separate bounded context/service
  - Services retain sync payload/merge/conflict ownership
  - local-only operation remains supported

- `0003-v1-integration-adapter-presentation-model.md`
  - executable Integration Adapters ship with WGT
  - WGT-native Avalonia presentation for V1
  - Services publish data/commands/metadata, not arbitrary executable UI
  - new integration families may initially require a WGT application update

- `0004-v1-personal-device-trust.md`
  - explicit personal Device pairing/trust
  - no mandatory user account for V1
  - trusted Devices have revocable credentials
  - future public/multi-user evolution may add account + Device trust

- `0005-hybrid-device-trust-recovery.md`
  - trusted Device approval is the normal enrollment/recovery path
  - separately stored recovery material is the emergency path
  - server control alone cannot recover/take over the trust domain


- `0006-wgt-local-persistence-stack.md`
  - SQLite via Microsoft.Data.Sqlite.Core
  - SQLitePCLRaw.bundle_green
  - explicit SQL adapters and WGT-owned migrations
  - no EF Core in WGT V1

- `0007-v1-client-host-composition-topology.md`
  - one primary WGT app host per client
  - statically shipped Integration Adapters
  - foreign contexts may be in-process/local out-of-process/remote
  - no mandatory WGT server


- `0008-v1-service-registration-publication-discovery.md`
  - explicit shipped Integration Adapters are the V1 registration/discovery baseline
  - provider-specific Published Contracts/transports are allowed
  - no mandatory universal publication wire protocol or automatic LAN discovery
  - generic Service Registry deferred until independently justified

Current intentionally deferred decisions are tracked in `docs/22_DEFERRED_DECISIONS.md`.

The most important are:

- production synchronization cryptography/key hierarchy,
- concrete first Illumination sync/interaction contracts,
- concrete first Vocation WGT contract,
- Mac build-host operational choice,
- future account/auth architecture if the product becomes multi-user.

Do not create placeholder ADRs that pretend unresolved options have been accepted.
