# Wiiii Got This – iOS Build and Tooling Strategy

## Status

Working implementation/tooling baseline.

The product stack is already accepted as C# / .NET 10 + Avalonia 12 with Windows and iPhone as required clients.

The primary development workstation remains Windows. The repository now also runs a real `net10.0-ios` simulator-target restore/link/AOT compile on GitHub-hosted macOS/Xcode infrastructure. This closes the former compile-only Apple-toolchain gap, but it does **not** constitute simulator UI validation, signing/provisioning validation, or physical-iPhone runtime acceptance.

A permanent Apple bundle identifier, signing identity, provisioning profile and distribution path remain deliberately undecided. CI uses a simulator-only application identifier and contains no signing secrets.

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

Native iOS compilation/linking and real iPhone signing/provisioning require Apple/Xcode infrastructure.

The repository's GitHub Actions workflow now provides an Apple-toolchain compile gate on macOS for an `iossimulator-arm64` target. Real device deployment still requires the appropriate signing/provisioning path and physical-device access.

Therefore successful Windows or macOS compilation is necessary regression evidence but not sufficient runtime evidence.

## 3. Current CI Workflow

The checked-in CI baseline contains two complementary gates:

```text
Windows CI
├── restore
├── solution build
├── full tests
└── vulnerable-package audit

macOS CI
├── .NET 10 / Xcode toolchain verification
├── .NET iOS workload
├── net10.0-ios simulator restore
├── iOS linker/AOT compile
└── explicit linker-warning policy
```

The iOS compile currently uses a CI-only simulator application identifier. It must not be treated as the future production bundle identity.

Repository-wide compiler warnings remain errors. Because Avalonia 12.0.4 currently emits an external `Avalonia.DesignerSupport` trim-summary warning, the iOS linker warnings are inspected separately: CI may tolerate only that known external warning and must fail on new/unexpected linker warnings.

## 4. Real Device Workflow

The later real-device path remains conceptually:

```text
WGT source
   ↓
Mac / Xcode / .NET iOS workload
   ↓
signing + provisioning
   ↓
signed iPhone build
   ↓
physical iPhone runtime validation
```

The Mac may be local, remote or hosted. The repository must not depend on committed machine credentials or signing secrets.

## 5. Signing and Provisioning

iPhone deployment requires the Apple signing/provisioning path appropriate to the chosen distribution method.

Sensitive signing material must not be committed to the repository.

The exact distribution path—development/ad-hoc/TestFlight/App Store or another permitted path—remains a later release/distribution decision.

## 6. iPhone Runtime Gate

Physical-device validation remains mandatory before claiming the iPhone host as accepted.

The baseline WGT smoke covers at least:

1. WGT launches through the real iOS host;
2. shared Avalonia `MobileShellView` renders;
3. the WGT SQLite database opens successfully;
4. migrations execute;
5. current Device configuration persists;
6. the Reference Integration is resolved;
7. the available Reference Capability can be opened;
8. integration enablement and Device override persist across application restart.

For Orientation-host readiness, the physical gate additionally covers:

1. the packaged Orientation `embed.html` surface and relative assets load through WKWebView;
2. `orientation.host-bridge` 1.0 reaches ready/status lifecycle correctly;
3. touch pan/zoom and feature selection work on the actual device;
4. foreground/background/reload lifecycle remains coherent;
5. WGT-owned When-In-Use location permission is requested only when needed;
6. CoreLocation fixes are converted to generic `current-position.set` messages;
7. denied/restricted/unusable position states produce `current-position.clear`;
8. no platform-specific second map renderer is introduced.

Orientation renderer readiness and Vocation mobile-data readiness are separate gates. The current iOS composition remains Reference-only unless an actual Vocation provider/read seam is explicitly accepted and composed. A working Orientation WKWebView must not create a dead Jobs/Map product destination by itself.

## 7. What CI Proves — and Does Not Prove

The macOS iOS compile gate can prove substantially more than the former Windows structural build:

- Apple-target API compatibility;
- iOS workload/Xcode compatibility;
- bundle-resource build integration;
- linker/AOT compatibility;
- trim-analysis regressions within the enforced warning policy.

It does not prove:

- signed-device installation;
- real WKWebView rendering behavior;
- touch behavior on physical hardware;
- CoreLocation permission UX or GPS behavior;
- application lifecycle behavior on a physical iPhone;
- Vocation provider/data availability on iPhone.

Do not collapse those distinctions in release notes or readiness claims.

## 8. Secrets

Do not commit:

- Apple private signing keys,
- provisioning secrets,
- Mac credentials,
- SSH private credentials,
- Apple account credentials.

Use local secret storage / CI secret facilities appropriate to the selected tooling.

## 9. Deferred Operational Decisions

The following remain deliberately deferred until signed-device/distribution work is needed:

- permanent Apple bundle identifier;
- buy versus rent Mac hardware;
- specific persistent Mac model/provider;
- Apple Developer distribution setup;
- TestFlight/App Store publication;
- signed-build credential topology.

These are operational choices rather than WGT domain decisions.
