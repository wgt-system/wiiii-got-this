# Wiiii Got This – iOS Build and Tooling Strategy

## Status

Working implementation/tooling baseline.

The product stack is already accepted as C# / .NET 10 + Avalonia 12 with Windows and iPhone as required clients.

This document does not choose a specific Mac hardware purchase, hosting vendor, Apple Developer membership, or CI provider.

The official .NET iOS workload is installed on the Windows development machine, and the `net10.0-ios` target compiles successfully there. Actual simulator/device runtime validation has not yet been performed. Signing, provisioning, and runtime execution still require Mac/Xcode infrastructure.

## 1. Development Workstation

The primary development workstation remains Windows.

Normal work should include:

- WGT domain/application development,
- Windows client development,
- shared Avalonia UI development,
- unit/application tests,
- most integration tests,
- contract tests,
- local fake/reference providers.

A Mac is not intended to become the primary development workstation.

## 2. iOS Build Constraint

Native iOS compilation/signing requires Apple/Xcode infrastructure.

Avalonia supports publishing iOS applications from Windows by using a network-accessible Mac build host.

Therefore the accepted WGT architecture must include access to macOS/Xcode before the first real iPhone deployment milestone.

## 3. Target Workflow

```text
Windows development machine
        │
        │ source / dotnet publish / remote build
        ▼
Mac build host
├── Xcode
├── required .NET/iOS workloads
├── signing identities
└── provisioning profiles
        │
        ▼
signed iPhone build
        │
        ▼
physical iPhone / distribution
```

## 4. Mac Build Host

The build host may eventually be:

- a dedicated Mac mini,
- another network-accessible Mac,
- suitable hosted macOS build infrastructure,
- CI infrastructure with the required Apple tooling.

The repository must not depend on one vendor-specific path.

Local development scripts/configuration should keep build-host settings external to source-controlled secrets.

## 5. Signing and Provisioning

iPhone deployment requires the Apple signing/provisioning path appropriate to the chosen distribution method.

Sensitive signing material must not be committed to the repository.

The exact distribution path—development/ad-hoc/TestFlight/App Store or another permitted path—belongs to a later release/distribution decision.

## 6. CI Direction

Early WGT Core CI does not need to block all development on iOS packaging.

Suggested progression:

1. shared .NET/domain/application tests on ordinary CI,
2. Windows build/test,
3. iOS project compile validation once a Mac runner/build host exists,
4. signed device/distribution builds only when needed.

The project must nevertheless create and exercise the iOS target early enough that cross-platform assumptions are not left until the end.

## 7. Early iPhone Smoke Slice

Before accepting the first real Vocation or Illumination provider integration on iPhone, perform the real iOS smoke validation covering at least:

1. WGT launches through the real iOS host;
2. shared Avalonia `MobileShellView` renders;
3. the WGT SQLite database opens successfully;
4. migrations execute;
5. current Device configuration persists;
6. the Reference Integration is resolved;
7. the available Reference Capability can be opened;
8. integration enablement and Device override persist across application restart.

This is a mandatory gate and remains outstanding until it is executed on appropriate Apple tooling. It must not be treated as completed based on shared-code tests or successful compilation alone.

Successful Windows `net10.0-ios` compilation is not runtime validation. Windows/Desktop may implement, validate and release a real provider integration before this smoke, but such a Windows-first milestone must not wire or claim the real provider as accepted for iPhone. Until the smoke succeeds, iPhone composition must remain limited to already validated/reference behavior rather than claiming production provider readiness. This gate does not waive signing, provisioning or device-runtime requirements. Illumination's separate local-runtime/persistence proof remains required before local Illumination execution is accepted on iPhone.

Before substantial WGT UI/integration work accumulates, create a small iPhone smoke slice that verifies:

- Avalonia application launches,
- shared WGT application/domain assembly loads,
- local SQLite adapter works,
- one fake/reference Capability can be resolved,
- one WGT-native view can be displayed,
- local integration configuration survives restart.

This prevents the project from becoming a Windows application that is only theoretically portable.

## 8. Secrets

Do not commit:

- Apple private signing keys,
- provisioning secrets,
- Mac credentials,
- SSH private credentials,
- Apple account credentials.

Use local secret storage / CI secret facilities appropriate to the selected tooling.

## 9. Decision Deferred

The following are deliberately deferred until the iPhone smoke milestone approaches:

- buy versus rent Mac hardware,
- specific Mac model/provider,
- Apple Developer distribution setup,
- TestFlight/App Store publication,
- CI provider for signed iOS builds.

These are operational choices rather than WGT domain decisions.
