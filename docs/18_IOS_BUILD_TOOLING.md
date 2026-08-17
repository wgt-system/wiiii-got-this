# Wiiii Got This – iOS Build and Tooling Strategy

## Status

Deferred platform track.

WGT retains an iOS project and long-term iPhone product intent, but active development is currently Windows/Desktop-first. There is no current real Mac/Xcode/physical-iPhone validation environment, so Apple runtime/provider acceptance is intentionally not part of the active v0.6.0 milestone.

The current real iOS composition remains limited to the Reference Integration. Vocation Jobs, Vocation Map and other foreign-provider product claims are not accepted on iPhone merely because shared code or project targets exist.

This document preserves the requirements for resuming real iOS work later; it is not a current Windows release blocker.

## 1. Development Workstation

The primary development workstation is Windows.

Current normal work includes:

- WGT Domain/Application development,
- Windows/Desktop product development,
- shared Avalonia presentation where platform-neutral,
- unit/application/integration/contract tests,
- Desktop provider integration,
- Windows release validation.

Do not distort the Desktop product or architecture merely to simulate iPhone readiness without the required Apple runtime environment.

## 2. Real iOS Constraint

Real iPhone acceptance requires appropriate Apple infrastructure:

- macOS,
- Xcode,
- the matching .NET/iOS workload,
- signing/provisioning where required,
- simulator and/or physical-device execution appropriate to the claim being made.

Compilation alone is not runtime evidence.

A future Apple build environment may be a dedicated Mac, another network-accessible Mac, or suitable hosted macOS/CI infrastructure. The repository must not depend on one vendor-specific topology.

## 3. Target Workflow When Resumed

```text
Windows development machine
        │
        │ source / remote build or CI
        ▼
Mac/Xcode environment
├── matching .NET/iOS workload
├── simulator where useful
├── signing identities / provisioning when required
└── runtime diagnostics
        │
        ▼
physical iPhone validation
        │
        ▼
accepted Apple runtime claim
```

The exact hardware/provider/distribution choice remains operationally deferred.

## 4. Current Composition Rule

Until real iOS work resumes and passes the required runtime gates:

- the current iOS composition remains Reference-only;
- do not expose Vocation Jobs or Map merely because Desktop seams exist;
- do not infer provider availability transitively from repository dependencies;
- do not create iOS-specific copies of Vocation or Orientation semantics;
- do not claim Apple runtime readiness from shared tests, Windows builds or hypothetical host code.

If a future product area depends on multiple services, each concrete seam needed on the device must actually be composed and validated.

## 5. Required First Real iPhone Smoke

Before accepting foreign-provider integration on iPhone, validate at minimum:

1. WGT launches through the real iOS host;
2. shared Avalonia presentation renders correctly;
3. WGT SQLite persistence opens and migrations execute;
4. current Device configuration persists;
5. the Reference Integration resolves and can be used;
6. integration enablement/device override survives restart;
7. foreground/background/reopen lifecycle is stable;
8. touch/focus/navigation behavior is usable on the actual device.

Only after that baseline should provider-specific iPhone capabilities be accepted.

## 6. Provider-specific Gates

### Vocation

Vocation owns the provider/data topology for any future iPhone read path. WGT must not invent a Vocation synchronization/publication mechanism merely to make Desktop data appear on iPhone.

### Orientation

Orientation owns generic geospatial rendering/interaction. A future WGT iPhone Map requires a real validated Orientation host plus the actual provider/read seams supplying spatial data. Renderer readiness and provider-data readiness are separate gates.

### Illumination

Local Illumination execution on iPhone additionally requires Illumination to prove its own runtime/persistence viability behind an Illumination-owned application/published boundary.

### Conveyance

Conveyance may later satisfy accepted generic durable-delivery requirements, but it does not create provider semantics or waive provider/runtime validation.

## 7. CI Direction

The active WGT CI is intentionally Windows/Desktop-focused for the current milestone:

1. restore;
2. solution build;
3. tests;
4. vulnerability audit;
5. repository diff hygiene;
6. Desktop startup smoke.

When iOS work is deliberately resumed, add an Apple runner/build-host gate appropriate to that milestone. Do not keep an expensive or misleading Apple compile gate active merely to imply progress while no Apple runtime validation path exists.

## 8. Signing and Secrets

Never commit:

- Apple private signing keys,
- provisioning profiles/secrets,
- Mac credentials,
- SSH private credentials,
- Apple account credentials.

The eventual distribution model—development, ad-hoc, TestFlight, App Store or another permitted path—is deferred until there is an actual Apple release requirement.

## 9. Windows Release Independence

Apple runtime validation is a separate platform claim.

A Windows/Desktop milestone may be validated and released while Apple runtime work is deferred, provided the release notes and repository documentation do not claim unsupported iPhone provider/runtime behavior.
