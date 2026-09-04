---
stage: implement
bolt: 057-architecture-and-standards-docs
created: 2026-09-04T01:40:00Z
---

## Implementation Walkthrough: architecture-and-standards-docs

### Summary

Four documents, no code. One consolidates the multi-replica reasoning that was spread across
five ADRs and adds what the ADRs never covered; one refreshes the tech-stack standard against
the manifests; one registers the tests that do not pass on a plain developer machine; one turns
the whole lot into a quarterly ritual with a measured baseline. Three existing files gained
links so the new documents are reachable from where a reader already is.

### Structure Overview

The four documents sit at two different altitudes on purpose. `tech-stack.md` stays in
`memory-bank/standards/` with the other descriptive standards, because it is one of them and is
loaded as agent context. The three new files sit under `docs/`, because they are read by a
person with a question — "why is this test red", "can we run two of these", "what do I check
this quarter" — rather than loaded as background.

Cross-linking runs in one direction wherever possible, so a fact has one home:
`KNOWN_FAILURES.md` points at `data-stack.md` for the test-fixture design rather than restating
it, the audit checklist points at the deployment guide's invoice-gap query rather than copying
the SQL, and the readiness doc points at each ADR rather than summarising its alternatives
table. Discovery goes the other way: `README.md` and `CLAUDE.md`'s map table point in.

### Completed Work

- [x] `docs/architecture/multi-replica-readiness.md` — the consolidated readiness picture. Two
      blockers that stop a second instance before any of the five decided concerns matter, then
      one section per named concern with its ADR and a today/if-046 split, then the
      instance-local state that has no decision record, then the negatives, then an ordered
      list of what a second replica would actually take.
- [x] `docs/KNOWN_FAILURES.md` — the two environment-gated surfaces with their exact mechanism,
      what a developer sees for each (one skips, one errors), the disposal of the inherited
      "7 consistently-failing tests" figure, a checked-and-clean section, and an explicit
      statement of what the register cannot tell you without a suite run.
- [x] `docs/ARCHITECTURE_AUDIT_CHECKLIST.md` — one page, six sections, each step with its
      command and what a bad answer looks like, a measured baseline per section, and a run log
      whose first row records what this pass found and left owed.
- [x] `memory-bank/standards/tech-stack.md` — refreshed against `package.json`, `angular.json`,
      both `.csproj` files and the workflow files. The email provider is now described as the
      required configuration it is; the observability, invoicing and rate-limiting libraries
      that four bolts added are named; the globalization gotcha is corrected; the deploy-trigger
      claim now matches what the workflows actually do.
- [x] `memory-bank/standards/system-architecture.md` — the in-process-queueing sentence now
      says every job runs in every instance and only three claim their rows, and links to the
      readiness doc.
- [x] `README.md` — an "Architecture & maintenance" group with the three new documents.
- [x] `CLAUDE.md` — three rows in the read-when map table. This is the repo's only real
      standards index; `memory-bank/standards/` has no index file.
- [x] `memory-bank/bolts/057-.../implementation-plan.md` — stage-1 artifact, with the backlog
      sweep, the reference-impact sweep, the verification table and the design-check outcome.
- [x] `memory-bank/intents/026-.../units/003-.../construction-log.md` — created for this unit,
      recording the three process deviations this wave's parallel execution required.

### Key Decisions

- **The readiness doc leads with what the ADRs never decided.** The five ADR-backed concerns
  are each individually safe or knowingly-accepted at one instance. The things that actually
  stop a second instance — the unlocked boot migration and ten sweeping background services
  that select rows without claiming them — had no decision record at all. Putting the five
  concerns first would have produced a document that reads as thorough and is wrong about
  severity, so the two blockers come first and the five follow.
- **Every concern's "today" half is written from the code, not from its ADR.** Three of the five
  had drifted: ADR-010's summary predates the recovery scanner being the real duplication
  vector, ADR-015's original stance was superseded by its own amendment, and ADR-023 still
  credits compare-and-swap for a property that a claim column now provides. The ADRs are cited
  for the *decision*; the shipped mechanism is described from the source.
- **No fabricated tracking ids.** The register's "tracking issue" column resolves to the review
  backlog, which this bolt may not write to. Gated suites need no ticket because the gate is
  the explanation, and nothing was invented to fill a column.
- **Counts are stated as what a command can count.** Test *classes* and *gates*, with the grep
  printed beside them; never a pass/fail tally, because no suite was run. The inherited
  "7 failing" number is exactly what happens when an unmeasurable number sits in a document.
- **Version granularity in `tech-stack.md` stays at the major line.** Naming every patch pin
  would guarantee the doc is wrong after the next dependency bump. Exact versions appear only
  where they are load-bearing.
- **`CLAUDE.md` was edited rather than deferred.** It is not on this wave's do-not-touch list,
  and the alternative left story 003's criterion satisfied only by proxy.

### Deviations from Plan

- **`CLAUDE.md` moved from "unaffected" to "updated"**, on the design check's finding that
  deferring it implied a constraint that does not exist. The plan's reference-impact sweep was
  corrected in place.
- **The instance-local list grew from six entries to nine**, and one of the plan's six was
  split in two (inbound and outbound rate limiting are different failures). The plan's list was
  assembled by pattern-matching for caches and channels; the design check found the two
  categories it missed.
- Everything else was built as planned.

### Dependencies Added

None. No package, no tool, no configuration.

### Developer Notes

- The three new documents under `docs/` and the two link edits must land in **one commit**.
  `tech-stack.md`, `README.md` and `CLAUDE.md` all link to files that do not exist on `main`,
  so a split commit leaves a broken link in history — and the audit checklist's own step 5.5
  would fail on its own baseline commit.
- `feat/bolt-054-dependency-hardening` will invalidate a small, known set of lines here. They
  are listed in `bolt.md` under "Re-verify after 054 merges" rather than hedged in the prose,
  so the coordinator has one place to check at merge time.
- Two findings surfaced that this bolt cannot fix inside its own surface — the deploy-trigger
  mismatch in the workflows, and the audit reminder that nothing yet schedules. Both are on
  `bolt.md`'s list for the coordinator rather than left in the documents as complaints.

### Self-validation (specsmd stage-2 checkpoint)

**Validated 2026-09-04.** Every deliverable in the plan exists; every claim traces to a command
recorded in `test-walkthrough.md`; the comments rule is respected (no bolt, review or finding
ids narrated inside the documents — ADR citations are content the stories require, and the
process history lives here and in `bolt.md`); nothing under the wave's do-not-touch list was
modified. The stage-4 fresh-eyes micro-review runs next, as a fresh subagent, before the test
report.
