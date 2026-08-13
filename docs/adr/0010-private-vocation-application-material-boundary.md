# ADR-0010: Private Vocation Application Material Boundary

- Status: Accepted
- Date: 2026-08-13

## Context

Vocation may own private application material such as CVs, cover letters, and other personal application documents. WGT may later present or transport that material, but must not absorb Vocation's business semantics or weaken its privacy boundary.

## Decision

CVs, cover letters, and personal application documents belong to **Vocation semantics**, not to the WGT domain.

WGT may present, cache, or transport this material only through explicit integration boundaries. Such handling must not make WGT the owner of the material's business meaning, lifecycle, or authoritative state.

Private application material must never appear in:

- public Published Contracts;
- public fixtures or examples;
- logs;
- repository artifacts; or
- other publicly exposed surfaces.

Future cross-device transfer must preserve an end-to-end-protected private trust boundary. Conveyance may relay only opaque protected payloads and must not receive plaintext application material.

Any future document-generation or rendering path must remain local or inside the same trusted private boundary. A public rendering service must not receive the documents or their private source content.

This ADR defines the ownership and exposure boundary only. It does not authorize document storage, uploads, synchronization, encryption implementation, Conveyance implementation, LaTeX/rendering, or Vocation source changes.

## Consequences

- Vocation remains authoritative for document meaning and application-material semantics.
- WGT integrations must use narrowly scoped, explicit contracts and must avoid public publication of private material.
- Future transport and rendering designs must be evaluated against the private trust boundary before implementation.
