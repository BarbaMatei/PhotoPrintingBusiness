# Future System — Test-Quality (QA-author) concept note

> **Status: PROPOSED / DEFERRED (captured 2026-06-15).** Idea record, not a spec. Aligns with the
> roadmap's **"full e2e/regression"** phase; wants real code + a real suite to act on. Part of the
> [future-systems map](README.md).

---

## The gap

Tests come from three places today: AI-DLC's per-bolt **TDD** (tests for the feature being built), the
bug-hunter's **`regression-harvest`** (keeps a proving test per confirmed bug), and the
`pr-test-analyzer` plugin (coverage check on a diff). But **nobody judges whether the suite as a whole
is actually good** — whether it covers the *right* things, whether it would *catch* a regression, where
the e2e/integration gaps are.

## The role (two faces)

Test-Quality owns **the test suite** — building it and judging it:

1. **Author & adversarially exercise** the suite — scan the app's test surface, write happy-path +
   auth-boundary + API-contract + **adversarial** e2e tests (injection, XSS, auth-bypass, IDOR, race,
   data-integrity, UI-error), and run them.
2. **Grade the net** — risk-weighted coverage + **mutation testing** ("if I corrupt this line, does a
   test fail?"). A high coverage % with a low mutation score is a paper safety net.

## Design direction

A clean **4-phase** shape: **scan → write → adversarialise → triage**, Playwright + Page Object Model,
TypeScript. The principles that make it good:

- **Philosophy:** a failing test *found* something — don't silence it; **blame the app first**; never
  change an assertion to go green without proof; run a test twice before calling it flaky. (This is the
  same honest-labeling / no-silent-failure ethos as the rest of the architecture.)
- **An adversarial catalog** — concrete reusable payload classes: SQLi, XSS, boundary, unicode/RTL,
  IDOR, double-submit / parallel writes, 500→UI, offline. Every flow's evil twin.
- **A triage protocol:** reproduce → investigate the app → verdict (app-bug / flaky / test-bug) →
  documented action; a `// WHY THIS EXISTS:` line on every test file for traceability.
- **Playwright craft:** POM, a selector-priority ladder (testid → role → label → text → CSS), assert
  the negative too, no `waitForTimeout`, per-test isolation, `beforeEach` cleanup.

## Design constraints for this architecture (the non-negotiables)

- **Read-only on app source — findings route through the loop, never an auto-fix.** "Blame the app
  first" is the right *instinct*, but the mechanism must be: a confirmed adversarial failure becomes a
  **fix-request** (`correlation_id`) → AI-DLC → Reviewer → merge. The failing test IS the proving test
  — the bug-hunter's `regression-harvest` pattern. Test-Quality must never patch app code directly.
- **Dual-DB parity (this app runs SQLite locally/test, PostgreSQL in prod).** A suite green on SQLite
  can give false confidence about production Postgres: the two diverge on concurrency (SQLite
  serialises writes; Postgres has real MVCC), constraint strictness, collation/case, type affinity,
  date/time, and migrations. So the **DB-sensitive and production-faithful e2e tests run against
  Postgres** (what ships), with SQLite kept for the fast local loop. The parity gap is itself a "hole
  in the net" Test-Quality should report. *(Also relevant to the bug-hunter's sandbox confirmation,
  which runs proving tests on the local DB.)*
- **Oracle-grounded scan.** Don't re-scan the app from scratch (that duplicates the bug-hunter's
  `app-mapping` + `code-index`). Consume those + the knowledge ledger's **risk-classed flows**, so the
  test surface is *risk-weighted* (auth/money/data-write first), not flat.
- **Owner-approved test writes** — new test files are the **one sanctioned write into the app tree**
  (per test, like `regression-harvest`), committed via the active CommitPolicy (Integration Contract
  §5.5). App code is never written.
- **Parameterised seeding** — seed via EF / test fixtures, never string-interpolated SQL.

## The disjointness boundary

The line that matters is **not "who may find a bug"** — multiple systems finding the same bug via
different modalities is *good* (defense in depth; the bug ledger dedups). It's **"who writes the fix"**
— nobody, directly; everything routes through the loop + Reviewer.

- **Inspector (bug-hunter):** finds bugs by **static analysis + sandbox confirmation** (reasons about
  code).
- **Test-Quality:** finds bugs by **black-box runtime e2e** (drives the running product) — a genuinely
  different modality — **and** owns the suite itself (authoring + grading), which the Inspector does
  not.

**Owner decision (2026-06-15): adversarial-e2e is owned by Test-Quality** (not folded into the
bug-hunter as a runtime hunter). The bug-hunter keeps its static + sandbox modality; Test-Quality owns
authoring/running e2e (incl. adversarial) plus suite grading. Both route findings through the loop, so
the duplication is harmless defense-in-depth, deduped at the bug ledger.

## Open questions (resolve when picked up)

- Mutation-testing tooling for the stack (Stryker.NET for C#, StrykerJS for TypeScript?) — cost/cadence.
- The Postgres-faithful tier for parity testing — where it runs (local Docker Postgres? the 3-env
  setup?) and which test subset is DB-sensitive enough to require it.
- Does it write into `bug-hunting/**`, or get its own store?
- How its findings (both bugs and *coverage/mutation gaps*) feed the [Conductor](conductor-system.md).

## Connections

Builds on: AI-DLC TDD, [bug-hunter](../bug-hunter-build-guide.md) (`regression-harvest`, the
Verifier's flake-guard), and the `pr-test-analyzer` plugin. Grounds its scan in the oracle
([knowledge-builder](../knowledge-builder-build-guide.md)) + `app-mapping`. Routes bug findings
through the fix loop ([contract §4](../integration-contract.md)); test writes follow the active
profile (§5.5). Feeds the [Conductor](conductor-system.md). Maps to the roadmap's e2e/regression phase.
