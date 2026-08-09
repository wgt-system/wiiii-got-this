# Wiiii Got This – Acceptance Tests

## 1. Purpose

This document defines technology-independent acceptance behavior for the initial Wiiii Got This domain and application baseline.

Tests become executable after architecture and implementation technology are selected.

## 2. Service Integration Configuration

### AT-01 Global enablement is inherited

Given:

- a known Service Integration,
- global enablement is `enabled`,
- Device A has no override,

Then:

- effective enablement on Device A is `enabled`.

### AT-02 Device disable override wins

Given:

- global enablement is `enabled`,
- Device A override is `disabled`,

Then:

- effective enablement on Device A is `disabled`,
- other Devices without overrides remain enabled.

### AT-03 Clearing override restores inheritance

Given:

- Device A has an explicit override,
- the override is cleared,

Then:

- Device A inherits global enablement.

### AT-04 Disabling integration does not delete foreign state

When:

- a Service Integration is disabled,

Then:

- no foreign service/domain deletion operation is performed,
- WGT-owned integration configuration remains recoverable as specified.

## 3. Registration and Discovery

### AT-05 Registered does not mean enabled

Given:

- a Service becomes known through registration/discovery,

Then:

- it is not automatically enabled unless an explicit future policy says otherwise.

### AT-06 Endpoint change preserves Service Identity

Given:

- a known Service changes its network/process location,
- the published stable Service Identity remains the same,

Then:

- Wiiii Got This updates location/registration metadata,
- it does not create a duplicate Service Integration solely because of the endpoint change.

### AT-07 Discovery does not imply trust

Given:

- an unknown Service is discovered,

Then:

- no privileged operation or foreign-data access occurs merely because discovery succeeded.

## 4. Availability

### AT-08 Disabled is distinct from unreachable

Given two Capabilities:

- Capability A belongs to a disabled integration,
- Capability B belongs to an enabled but unreachable provider,

Then:

- their Availability reasons are distinguishable.

### AT-09 Reachable but incompatible is unavailable

Given:

- provider is reachable,
- no supported contract version exists,

Then:

- Capability is not invoked,
- Availability reports incompatibility rather than reachability success.

### AT-10 Service can be reachable while one Capability is unsupported

Given:

- Service is reachable and compatible,
- Capability A supports current Device/Platform,
- Capability B does not,

Then:

- A is available if other requirements pass,
- B is unavailable with an unsupported-context reason.

### AT-11 Missing prerequisite is explicit

Given:

- integration is enabled,
- provider is compatible,
- a required local/remote prerequisite is missing,

Then:

- the Capability is unavailable,
- the reason is not collapsed into generic provider failure.

### AT-12 Unrelated services remain usable

Given:

- Service A fails,
- Service B remains healthy,

Then:

- B's Capabilities remain resolvable and usable,
- WGT does not enter whole-product failure merely because A failed.

## 5. Contract Boundaries

### AT-13 Foreign domain classes are not required

For a Vocation or Illumination integration:

- WGT compiles/runs/tests without importing the provider's internal domain assemblies/modules/classes.

### AT-14 No foreign database access

For each provider integration:

- all business data is obtained through explicit published contracts,
- no WGT persistence adapter reads provider-owned tables.

### AT-15 Unsupported contract is rejected deterministically

Given:

- provider publishes only an unsupported contract version,

Then:

- invocation is blocked before foreign business operations are attempted.

### AT-16 Contract DTO is boundary data

Given:

- WGT receives a foreign DTO,

Then:

- WGT may map it into read/presentation state,
- it does not persist the DTO as an authoritative WGT domain aggregate merely for convenience.

## 6. Presentation Integration

### AT-17 Integrated presentation does not change ownership

Given:

- an Illumination workflow is presented inside WGT,

Then:

- review/scheduling decisions are still made through Illumination's published behavior,
- WGT does not calculate its own replacement scheduling transition.

### AT-18 Different clients may present same Capability differently

Given:

- two WGT presentation environments support the same Capability,

Then:

- each may render different layout/interaction appropriate to its Platform Context,
- both invoke the same service-owned semantics.

### AT-19 Unsupported presentation is explicit

Given:

- Capability business contract is compatible,
- no presentation/invocation contribution can be used by the current client,

Then:

- Capability is not silently treated as available.

### AT-20 External delegation is explicit

Given:

- a Capability uses external application/resource delegation,

Then:

- delegation occurs only through its accepted invocation path,
- WGT does not treat external launch as the universal default integration mechanism.

## 7. Vocation Boundary

### AT-21 Vocation overview is read through contract

Given:

- a future Vocation read Capability is available,

When:

- WGT shows opportunity overview,

Then:

- data came from the Vocation published read contract,
- no WGT JobOpportunity aggregate is required.

## 8. Illumination Boundary

### AT-22 Illumination interaction remains service-owned

Given:

- a future Illumination study Capability is used in WGT,

When:

- the user submits a review-related action,

Then:

- WGT sends an explicit Illumination command/action,
- Illumination determines the authoritative resulting learning state.

## 9. Data Locality

### AT-23 Server presence does not authorize upload

Given:

- WGT has access to a personal server,
- a foreign service marks or defines state as local-only/not remotely persistable,

Then:

- WGT does not upload or remotely persist that state merely because infrastructure exists.

### AT-24 Cache does not become authority

Given:

- WGT caches a foreign read result,

Then:

- the cache remains distinguishable from authoritative provider state,
- stale cache is not presented as current availability/business truth where freshness matters.

## 10. Runtime and Deployment Independence

### AT-25 Docker is optional architecture, not domain requirement

The domain/application tests must run without depending on Docker-specific domain concepts.

A deployment may still use Docker where selected.

### AT-26 Service process topology is opaque to domain

Capability resolution does not require a rule that every Service maps to one process/container.

## 11. Future Synchronization Guard Tests

These are specification guards, not initial executable behavior.

### AT-27 WGT does not invent foreign merge rules

Any future replicated Vocation or Illumination state must use service-owned conflict/merge semantics.

### AT-28 Offline capability requires explicit support

WGT must not assume a Capability works offline merely because cached data exists.

## 12. Architecture Acceptance Gate

Before the first production-code milestone:

- target client/platform scope is explicit,
- language/runtime is explicitly accepted,
- UI/client strategy is explicit,
- persistence approach is explicit,
- initial publication/transport approach is explicit,
- implementation plan names a narrow first vertical slice,
- tests for that slice are identifiable from this document.
