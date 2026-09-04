---
stage: plan
bolt: 057-architecture-and-standards-docs
created: 2026-09-04T00:55:00Z
---

## Implementation Plan: architecture-and-standards-docs

### Objective

Make the standards and architecture docs trustworthy: one place that explains what blocks
multi-replica deployment, a `tech-stack.md` whose every claim is checkable against the
manifests in this worktree, a register of the tests that do not pass on a plain developer
machine, and a quarterly checklist so all of it is re-checked on a cadence instead of by
accident.

No code changes. No test runs — this bolt changes nothing that could be tested, and a full
suite run would saturate a machine shared with three other wave-1 sessions.

### Deliverables

| # | Path | Story | New / edit |
|---|---|---|---|
| 1 | `docs/architecture/multi-replica-readiness.md` | 001 | new |
| 2 | `memory-bank/standards/tech-stack.md` | 002 | edit (refresh) |
| 3 | `docs/KNOWN_FAILURES.md` | 002 | new |
| 4 | `docs/ARCHITECTURE_AUDIT_CHECKLIST.md` | 003 | new |
| 5 | `memory-bank/standards/system-architecture.md` | 001 | edit — link to #1 from the scaling sentence it already carries |
| 6 | `README.md` | 002, 003 | edit — three links, next to the existing Operations list |
| 7 | `memory-bank/bolts/057-.../bolt.md` | all | "Re-verify after 054 merges" + pointers wanted in files this bolt may not touch |

Stage artifacts: this plan, `implementation-walkthrough.md`, `test-walkthrough.md` (a
verification report — see "Verification" below), and the unit `construction-log.md`.

### Dependencies

- **None blocking.** The bolt is independent by design (`requires_bolts: []`).
- **`feat/bolt-054-dependency-hardening` is in flight and NOT merged** — checked at
  `origin/main` = `182cd50`; 054's tip `0ab31eb` is pushed but unmerged. Per the kickoff,
  claims are verified against `main` as it stands, and every line 054 will change is listed in
  `bolt.md` for the coordinator to re-check at merge time. No 054 content is described as
  present.

### Technical approach

**Story 001 — `docs/architecture/multi-replica-readiness.md`.**
One section per concern, in the story's order, each citing its ADR and each split into
`today` / `if bolt 046 lands`. The five named concerns: promotion queue (ADR-010), Sameday
token cache (ADR-013), AWB duplicate-create (ADR-015 **as amended 2026-07-27** — the
`Orders.AwbClaimedAt` lease, not the original "vendor idempotency" stance), status
compare-and-swap (ADR-016), ANAF dispatch (ADR-023).

Then a second, clearly separate section: **instance-local state the five sections do not
cover**, found by reading the code rather than the ADRs — local-disk tier 1 (ADR-008/011),
SignalR with no backplane, the in-memory rate limiter, the `IMemoryCache` admin-stats cache,
the `MemoryCacheOnceRegistry` log-once registries, and the second in-process channel
(`AwbJobQueue`). A readiness doc that listed only the five ADR-backed items would leave an
operator thinking the local-disk tier is fine, which is the opposite of true; each entry
states the observable symptom on a second replica. This section is an addition beyond the
story's acceptance criteria, made because the criteria's stated purpose — understand what
blocks multi-replica — cannot be met without it.

The doc must not read as a commitment to build Redis: bolt 046 is named as deprioritized
everywhere it appears.

**Story 002 — `tech-stack.md` refresh.** The doc was rewritten from the code on 2026-07-14 and
the three errors the story names (Angular 17+, Jasmine/Karma, phantom `heic2any`/`ng2-charts`)
are already gone; the email-provider line is still framed as "MailKit (dev) + SendGrid (prod)"
rather than config-driven, and the drift since is what bolts 038/039/044/045 added:
OpenTelemetry, the Prometheus exporter, Sentry, QuestPDF, `Polly.RateLimiting` — all
installed, none mentioned. Approach: keep the existing shape and the existing
version-granularity (major lines, not patch pins, so the doc does not rot on every bump), fix
the email line, add the missing library families, add the `InvariantGlobalization=false`
gotcha the API csproj carries, and add pointers to #3 and #4.

**Story 002 — `docs/KNOWN_FAILURES.md`.** The "7 consistently-failing tests" figure comes from
one line of the 2026-06-03 architect review reading a stale `941/948`. It cannot be reproduced
without a suite run, which this bolt is forbidden to do, and three months of bolts have landed
since. What is exactly knowable from the source, and what the register will therefore hold:

- the MinIO suite — `[SkippableFact]` + `Skip.IfNot(_fx.Available, …)`, which **skips** without
  `STORAGE_TEST_*`;
- the PostgreSQL-backed classes — `PostgresTestDatabase`'s constructor **throws** (it does not
  skip) when no server is reachable, so on a machine without PostgreSQL these are errors, not
  skips;

with counts stated as what they are — gates and test *classes*, counted by a grep the doc
prints — never as a pass/fail tally. The register says plainly that "7 failing" conflated
skips with failures and that an actual failure count needs a run this bolt did not do. Per the
story's edge-case rule, nothing is labelled an expected failure on inherited hearsay: an
unexplained failure would be escalated, not documented as normal.

"Tracking issue" maps to this repo's real tracker, `reviews/state/backlog.md`, which this bolt
may not edit and may not mint ids in. Gated suites need no ticket — the gate *is* the
explanation; anything without one is listed for the coordinator instead of being given a
fabricated id.

**Story 003 — `docs/ARCHITECTURE_AUDIT_CHECKLIST.md`.** One page, five required areas
(vulnerabilities, outdated packages, LOC growth, ADR additions, doc rot), each as a runnable
step with the command and what a bad answer looks like. Anchored with a baseline taken in this
bolt so the first real run has something to compare against. Two care points: the Renovate
dashboard the story wants it to tie into arrives with 054 (`.github/renovate.json` does not
exist on `main` today), so it is written as the place to look with its arrival noted for
re-verification; and the invoice-number gap audit, which ADR-020 already calls a quarterly
ritual and which otherwise has no home, is referenced by pointer without copying its SQL.

### Required reading (bolt-process routing table)

Docs-only, so the code-touching rows do not apply. Read and used: `decision-index.md` "Read
when" lines, ADR-008/010/011/013/015/016/023, `system-architecture.md`, `data-stack.md`,
`coding-standards.md`, `definition-of-done.md`, `bolt-process.md`.

### Backlog sweep (`reviews/state/backlog.md`, areas `records` and `data`)

| Row | Decision |
|---|---|
| PPW-548 — ADR-023 and `decision-index.md` still credit CAS for multi-replica AWB safety, superseded by the `AwbClaimedAt` lease | **re-deferred**: the fix edits an ADR owned by another target and `decision-index.md`, which two other wave-1 groups already edit. Coordinator ruling 2026-09-04. Mitigated, not closed: deliverable #1 states the lease as the current mechanism, so the consolidated doc is right where the ADR is stale. |
| PPW-601 — `system-architecture.md` never updated for the invoicing feature | **re-deferred**: closing it means describing a feature this bolt did not build, and the row's terminal state must be written back to its home ledger under `reviews/state/`, which is not this bolt's surface. Coordinator ruling 2026-09-04; on its merge-time list. |
| PPW-573 — data-stack standard and the deployment guide left stale by the migration squash and the provider removal | **re-deferred**: the data-stack half reads as already done (rewritten from the code 2026-08-20, one `InitialPostgres` baseline, no SQLite), and the deployment-guide half is `docs/DEPLOYMENT.md`, which bolt 054 owns this wave. |
| PPW-630 — the quarterly gap-audit query uses session-timezone `EXTRACT` while the unique index uses `AT TIME ZONE 'UTC'` | **re-deferred**: fixing the query is invoicing work. Deliverable #4 avoids propagating it — it points at the ritual instead of copying the SQL. |
| PPW-12, PPW-131, PPW-335, PPW-371, PPW-390, PPW-393, PPW-402, PPW-421, PPW-422, PPW-433, PPW-436, PPW-437, PPW-497, PPW-539, PPW-572, PPW-577, PPW-619, PPW-623, PPW-627, PPW-631, PPW-640, PPW-641, PPW-643, PPW-644, PPW-650, PPW-656 | **re-deferred**: filed under `records` but each is a defect in another target's bolt artifacts, source files or frontend code — none is in this bolt's four documents. |

`reviews/state/backlog.md` is not edited; the coordinator writes the row notes at merge time.

### Reference-impact sweep

Every doc that cites what this bolt changes, and what happens to it:

| Referrer | Effect |
|---|---|
| `memory-bank/standards/system-architecture.md` | **updated** — its "blocks multi-VM scale-out … bolt 046" sentence gains the link to #1 |
| `README.md` | **updated** — three new links |
| `memory-bank/standards/data-stack.md` | **unaffected** — #3 points at its relational-test section rather than restating the mechanism |
| `memory-bank/standards/decision-index.md` | **unaffected by ruling** — off-limits this wave; #1 does not depend on it being fixed |
| `docs/DEPLOYMENT.md` | **unaffected** — 054 owns it; the wanted pointer is listed in `bolt.md` |
| `CLAUDE.md` — its map table is the read-when index in practice | **updated** — three read-when rows added. It is not on the do-not-touch list, and it is the only real standards index (`memory-bank/standards/` has no index file), so deferring it would have left story 003's criterion satisfied only by proxy |
| `.specsmd/**` templates and guides citing `tech-stack.md` | **unaffected** — they reference the path, not its content |
| `docs/analysis/architect-review-2026-06-03.md`, `docs/planning/bolt-parallel-plan-*.md` | **unaffected** — historical records; #3 answers the review's recommendation without editing it |
| `memory-bank/intents/026-.../requirements.md`, its stories, `units.md`, `story-index.md` | **unaffected** — story and index roll-up is the coordinator's at merge time |

### Verification (this bolt's substitute for a test suite)

There is no test to write, so the failure-mode table's "which test proves it" column becomes
"which command proves it". Every claim in every deliverable must trace to one of these, and
`test-walkthrough.md` records the claim-by-claim result.

| What can be wrong | What should happen instead | What proves it | Recorded as |
|---|---|---|---|
| A frontend version or library claim is stale or phantom | Only what `package.json` lists, at the major line it lists | `cat src/PhotoPrint.UI/package.json` | per-claim row |
| A backend library claim is stale or phantom | Only what the two `.csproj` files list | `cat src/PhotoPrint.API/PhotoPrint.API.csproj src/PhotoPrint.Tests/PhotoPrint.Tests.csproj` | per-claim row |
| A test-runner or lint claim is wrong | Builder read from `angular.json`; "no ESLint" from the absence of both config and script | `grep -n '"test"' -A2 src/PhotoPrint.UI/angular.json`, `ls src/PhotoPrint.UI` | per-claim row |
| A CI claim is wrong | Only what the workflow files do | `cat .github/workflows/ci.yml` (read-only) | per-claim row |
| A multi-replica claim describes an ADR's intent rather than the shipped code | Each concern's `today` half cites the file and symbol that implements it | targeted `grep` per concern, listed in the walkthrough | per-concern row |
| A known-failure entry is really a bug | Escalate, do not normalise | the gate mechanism is read in the source; no gate found means escalate | explicit in #3 |
| A claim 054 is about to change is stated as durable | It is listed for re-verification | `git diff main...feat/bolt-054-dependency-hardening` (read-only) | "Re-verify after 054 merges" in `bolt.md` |
| A count is asserted without a run | State only what a grep can count, and print the grep | the command appears in the doc | explicit in #3 |

### Acceptance criteria

- [ ] #1 has one section per the five named concerns, each citing its ADR and each stating
      today vs bolt 046; AWB safety is described as the `AwbClaimedAt` lease
- [ ] #1 also covers the instance-local state the five do not, symptom by symptom
- [ ] #1 never reads as a commitment to build Redis
- [ ] `system-architecture.md` links to #1
- [ ] #2's every claim traces to a manifest in this worktree; the email provider reads as
      config-driven; OpenTelemetry, the Prometheus exporter, Sentry, QuestPDF and
      `Polly.RateLimiting` appear
- [ ] #3 lists every environment-gated test surface with its mechanism, says which skip and
      which throw, and states honestly what it could not measure
- [ ] #3 gives no test a fabricated tracking id
- [ ] #4 covers vulnerabilities, outdated packages, LOC growth, ADR additions and doc rot, with
      a baseline and a runnable command per area
- [ ] #4 is referenced from `README.md` and `tech-stack.md`
- [ ] `bolt.md` carries the "Re-verify after 054 merges" list and the pointers wanted in files
      this bolt may not touch
- [ ] `git diff origin/main...HEAD --name-only` matches nothing on the do-not-touch list

### Self-validation (specsmd stage-1 checkpoint)

**Validated 2026-09-04.** Stories reviewed against the unit brief and the originating review
recommendation; deliverables and their owners named; the one real dependency (054, unmerged)
resolved into a verification rule rather than a wait; acceptance criteria are each checkable by
reading a named file. Two deliberate departures from the acceptance criteria as written, both
recorded above with reasons: the instance-local-state section is an addition, and the
"7 failing tests" figure is reframed rather than reproduced.

### The stage-2 gate: adversarial design check

Run 2026-09-04 as a fresh subagent against this plan and the two documents already written, with
the brief "attack this — false claims, missed multi-replica hazards, ADR-intent versus shipped
code, the known-failures census, acceptance-criteria dodges". Two blockers, ten high and four
medium findings. Every one is folded in; nothing was recorded as declined. What changed:

| Finding | Disposition |
|---|---|
| `Database.Migrate()` at boot has no lock — a second instance crash-loops. In neither list | folded in as the readiness doc's first blocker, ahead of the five concerns |
| Thirteen hosted services run in every instance; only three claim their rows. The plan's list covered two | folded in as a table of all thirteen with trigger, claim and symptom. `EmailRetryJob` (duplicate customer email) and `AccountDeletionJob` (erasure run fails on the loser's `SaveChanges`) are called out by name |
| The `InvariantGlobalization` paragraph, copied from the csproj comment, misdescribes the `Dockerfile` — nothing sets the invariant switch to `true`, and `icu-data-full` is the load-bearing part | rewritten as the three things that must agree; the false override claim is gone |
| `deploy.yml`'s `workflow_run` is filtered to `main` while `ci.yml` ignores pushes to `main`, so only `workflow_dispatch` reaches it | `tech-stack.md` now says so; the workflow itself is off-limits, so it is on `bolt.md`'s escalation list |
| `AnafTokenProvider` is a second in-process token cache with no ADR, and SPV refresh tokens rotate | folded into concern 2, which is now "token caches" plural |
| Outbound vendor limits are per process too (`SamedayPolicies` limiter, two `SemaphoreSlim` caps) | the single rate-limiter bullet is split into inbound and outbound rows |
| `ImageDecodeLimiter` is a per-process memory-bomb guard, not a cache | added with the co-located-replica OOM symptom |
| An ADR-010-derived summary would miss that `PromotionRecoveryScanner`, not the channel, is the duplication vector | concern 1 written from the code; the two per-process ceilings named |
| ANAF has its own lease column (`Invoices.ClaimedAt`), covering the `Pending` path only | concern 5 corrected; the `Submitted` and `Rejected` slices are described as unclaimed |
| The checklist's doc-rot table invented a header date for `api-conventions.md` and pre-dated `tech-stack.md` | table rebuilt from `git log` and the actual headers; three docs have no header at all and `decision-index.md`'s `last_updated` is three months stale — all now recorded as findings |
| The checklist's largest-file command is broken (`xargs` batching) and the measurement it declined to record breaches its own threshold twice | command fixed with `grep -v ' total$'`; `home-page.ts` 951 and `InvoiceUploadJob.cs` 615 recorded in the baseline and the run log |
| Routing story 003's "standards index" to `README.md` was a dodge, because `CLAUDE.md` is not off-limits | three read-when rows added to `CLAUDE.md`'s map table |
| Exact census: 10 gated S3 tests in 1 class (skip), 17 PostgreSQL-backed classes (error), 13 of them under `Unit/`, 0 skipped frontend specs, nothing normalising a bug | all of it in `KNOWN_FAILURES.md`, with the trap that `CLAUDE.md`'s own `Unit.*` filters hit database-requiring tests |
| The Postgres precondition is a reachable role that may `CREATE DATABASE`, not "PostgreSQL installed" | corrected in both `KNOWN_FAILURES.md` and `tech-stack.md` |
| `MetricsEndpointIpAllowListMiddleware` keeps capped per-process denial state, unlike the log-once registries | listed separately, with the replica-count-dependent alert threshold |
| Story 003's "checklist never run" edge case had no answer | due dates in the header, a line telling the reader to set a reminder, and a `bolt.md` note that the reminder itself is still owed |
| No Data Protection or antiforgery usage, so no shared key ring; the log-once registries really are log-only | added to the readiness doc's "what is *not* a problem" section so nobody re-investigates |

The check also re-ran every count in the audit checklist's §2, §3 and §4 baselines and every
frontend claim in `tech-stack.md`, and confirmed them exactly.
