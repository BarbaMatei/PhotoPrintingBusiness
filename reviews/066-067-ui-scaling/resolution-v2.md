---
type: resolution
target: 066-067-ui-scaling
version: 2
answers: pass v2 (verification — index row)
status: resolved
fixed_commit: 52541d1
closed: 2026-09-08
---

# Resolution v2 — 066-067-ui-scaling

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-763 | fixed | `52541d1` | New `RealtimeE2eHandshakeTests` (dotnet, runs on this machine) pins the spec's dual-signal wait: both transports feed one counter, the poll is awaited with floor 0, and the counted request must carry `id=`. Live-stack proof stays CI-only. |

## Scope

| Cluster | Findings | Files | Protocol |
|---|---|---|---|
| A — a runnable guard for the realtime handshake wait | PPW-763 | `src/PhotoPrint.Tests/Unit/Configuration/RealtimeE2eHandshakeTests.cs` (new; the guarded artifact `src/PhotoPrint.UI/e2e/realtime-order.spec.ts` is unchanged) | — |

## Decisions

### The round adds a guard, not a second fix (PPW-763)

The verification reopened PPW-763 as `no-guard-ci-only`, not as a live defect: round 1's wait
(`realtime-order.spec.ts:45-70`) is sound by reading — it counts a WebSocket frame **or** a
long-poll transport request carrying `id=` into one counter — but nothing on this machine reddens
if that is reverted, because Playwright needs Docker. So the spec's behaviour is untouched and the
round's whole diff is one new dotnet test class. The invariant it pins is round 1's protocol bullet
verbatim: either transport counts, the `/negotiate` POST never does.

### Why a dotnet assertion over the spec text (PPW-763)

Three shapes were available. (1) Run the spec — impossible here, no Docker. (2) A Vitest spec —
the Angular unit-test builder includes `src/**` only, so nothing under `e2e/` is in scope; widening
it would pull Playwright specs into the Vitest runner. (3) A dotnet test asserting the artifact's
content — the repo's established pattern for exactly this situation: `ContainerRuntimeTests` over
`Dockerfile`, `E2eCredentialsTests` over `e2e/support/stack.ts`, and `RealtimeE2eSeedTests` over
this very spec's pinned order numbers. Shape 3 was taken, in `Unit/Configuration` beside its two
siblings. It is a proxy: it proves the spec still *encodes* a transport-agnostic wait, never that
the wait resolves against a live stack. That half is still only the CI e2e job on PR #21.

The assertions are structural, not substring-spotting: the increment's own guarding condition is
captured and asserted (so a dead `const hasId = …` beside a hub-only condition still reddens), the
frame hook must sit inside the socket-URL condition, both listeners must feed the *same* counter,
and the poll must be `await`ed with a floor of `0`.

### Parked — should the UI test scope ever reach `e2e/`? (PPW-763)

Fixer decision, parked for the owner (default taken: no infrastructure change). A behavioural
guard is possible if the URL predicate is extracted into a pure function and exercised with
representative URLs (`…/negotiate` → false, `/hubs/admin-orders?id=abc` → true) from a plain
Vitest spec. That needs either the Angular builder's `include` widened past `src/**` or e2e logic
moved into `src/`, both test-infrastructure changes outside this row. The text guard was taken
instead; the owner decides whether the UI suite should ever cover `e2e/` helpers.

### Two class siblings parked for the re-reviewer (PPW-763)

The class is "a protocol invariant whose only assertion lives in an artifact this machine cannot
execute". Swept: of round 1's three e2e invariants, PPW-764's is guarded by `E2eCredentialsTests`
and PPW-766's seeded-Paid-count half by `RealtimeE2eSeedTests`; PPW-763's was the unguarded
instance and is now closed. The round review found two further gaps, both on PPW-766's
already-verified half and outside this round's single row, so both are parked, not fixed and not
minted as backlog rows:

- `realtime-order.spec.ts:35-43` picks its target with `seeded.find(o => o.status === 'Paid')`.
  The protocol says an attempt "never falls back to an unpinned order", but a regression to
  `?? orders[0]` reddens nothing.
- `RealtimeE2eSeedTests.CiAttempts()` derives `retries + 1` from `playwright.config.ts`, which is
  the right count only while attempts run serially. No test reads `workers: 1` or
  `fullyParallel: false`, so raising either silently invalidates the seeded-order math.

### Revert proofs — the handshake wait (PPW-763)

Each lever was applied to `realtime-order.spec.ts`, run scoped, then reverted with
`git checkout --`. The smallest lever for the fix itself is the first one:

- **Delete the `page.on('request', …)` long-poll listener** (round 1's first, WebSocket-only
  attempt) → `RealtimeE2eHandshakeTests` red `passed 0, failed 2`.
- Drop `&& url.includes('id=')` from that listener's predicate → red `passed 1, failed 1`,
  `RealtimeSpec_DoesNotCountTheNegotiatePostAsAConnection`, with
  `Expected longPoll "…if (url.includes('/hubs/admin-orders')) hubSignals += 1;…" to contain "id="`.
- Hoist the `framereceived` hook out of the socket-URL condition **and** drop the `await` on the
  poll → red `passed 1, failed 1`,
  `RealtimeSpec_CountsEitherTransportIntoOneHandshakeSignal`.
- Spec restored → final scoped run over `PhotoPrint.Tests.Unit.Configuration`
  `passed 204, failed 0`.

### Round review and test audit — round 2

Round review (94k tokens): 3 items. One must-fix folded in — the first draft asserted
`/hubs/admin-orders` and `id=` as independent substrings of the listener body, which a hub-only
condition plus a dead `const hasId` would have satisfied; the guard now captures the condition that
guards the count. Its other two items are the class siblings parked above. Its "better guard shape"
suggestion is the parked Vitest option.

Test audit: `sound-with-notes`. One must-fix folded in — the poll gate had no `await` anchor, so a
fire-and-forget `expect.poll(…)` would have passed; it is anchored now. One optional taken: the
draft's blanket `NotContain("||")` over the listener body was dropped as overreach (it would block
a legitimate `id= || connectionId=` check) — the captured-condition assertion covers that risk. It
confirmed both tests fail against the pre-fix `page.waitForRequest` spec and against round 1's
WebSocket-only attempt, and rules (b)/(c) as not applicable (no persisted state, no fakes). Its
residual note stands: a shadowed inner `let hubSignals = 0` would still pass, and no text guard can
prove the wait resolves live.
