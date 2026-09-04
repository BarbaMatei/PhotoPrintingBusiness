---
id: 057-architecture-and-standards-docs
unit: 003-architecture-and-standards-docs
intent: 026-observability-boot-manifest
type: simple-construction-bolt
status: review-pending
stories:
  - 001-multi-replica-readiness-doc
  - 002-refresh-tech-stack-and-known-failures
  - 003-architecture-audit-checklist
created: 2026-06-05T09:30:00Z
started: 2026-09-04T00:50:00Z
completed: null
current_stage: review
stages_completed:
  - name: plan
    completed: 2026-09-04T01:05:00Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-09-04T01:55:00Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-09-04T02:15:00Z
    artifact: test-walkthrough.md

requires_bolts: []
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 1
---

# Bolt: 057-architecture-and-standards-docs

## Overview

Consolidate multi-replica readiness (P12) and refresh the standards docs with a known-failures register and a quarterly audit checklist (P19).

## Objective

Make the docs trustworthy and the scaling reasoning discoverable — independent of the code-bearing bolts.

## Stories Included

- **001-multi-replica-readiness-doc**: Consolidate ADRs 010/013/015/016/023 (Could)
- **002-refresh-tech-stack-and-known-failures**: Correct tech-stack + KNOWN_FAILURES (Must)
- **003-architecture-audit-checklist**: Quarterly audit checklist (Must)

## Bolt Type

**Type**: simple-construction-bolt
**Definition**: `.specsmd/aidlc/templates/construction/bolt-types/simple-construction-bolt.md`

## Stages

- [x] **1. plan**: ✅ Complete → implementation-plan.md (+ stage-2 adversarial design check)
- [x] **2. implement**: ✅ Complete → docs/architecture/multi-replica-readiness.md, tech-stack.md, docs/KNOWN_FAILURES.md, docs/ARCHITECTURE_AUDIT_CHECKLIST.md (+ stage-4 fresh-eyes micro-review)
- [x] **3. test**: ✅ Complete → test-walkthrough.md (every claim verified against the manifests; no suite run — docs-only)

## Dependencies

### Requires
- None (independent docs)

### Enables
- None

## Success Criteria

- [x] Multi-replica doc covers 5 concerns, cited + linked — `docs/architecture/multi-replica-readiness.md`
  carries the five decided concerns (promotion queue, vendor token caches, AWB duplicate-create,
  order status transitions, ANAF dispatch), each citing and linking its ADR (010, 013, 015, 016,
  023); inbound links from `README.md:22`, `memory-bank/standards/tech-stack.md:96`, `CLAUDE.md:107`
  and the audit checklist.
- [x] tech-stack.md matches reality; failures documented — every version, package and CI claim in
  `tech-stack.md` re-read against the manifests in this worktree (record in `test-walkthrough.md`).
  **Deviation on the number:** the criterion's "7 failures" is the inherited *"7 consistently-failing
  tests"* figure, which no run in this repo's history measured. `docs/KNOWN_FAILURES.md` documents the
  real classes instead — the MinIO-gated S3 suite (skips), seventeen PostgreSQL-backed classes (error,
  do not skip), and a section retiring the "7" figure and saying where it went — rather than restating
  an unverified count.
- [x] Audit checklist exists + referenced — `docs/ARCHITECTURE_AUDIT_CHECKLIST.md`, referenced from
  `CLAUDE.md:108`, `README.md:24`, `memory-bank/standards/tech-stack.md:103` and
  `docs/KNOWN_FAILURES.md:135`.

## Re-verify after 054 merges

Verified 2026-09-04 against `origin/main` = `f2e70ad`; `origin/feat/bolt-054-dependency-hardening`
is **not** an ancestor of it, so every line below states main as it is today and goes wrong once 054
lands. Nothing merges until the whole wave is done, so this list is the merge-time re-check. Today in
this worktree: `Directory.Packages.props` does not exist and `.github/renovate.json` does not exist.

| Where | What it says today | What 054 changes |
|---|---|---|
| `memory-bank/standards/tech-stack.md:33` | `Stripe.net` named with no version; the pin at `src/PhotoPrint.API/PhotoPrint.API.csproj:43` is `46.3.0` | pin moves to `47.0.0` — a major bump; re-read that nothing else in the doc implies the 46 line |
| `memory-bank/standards/tech-stack.md:39-42` | the OpenTelemetry package list, and "the Prometheus exporter and the EF Core instrumentation are pinned to `-beta.1` builds because no stable line has ever been published for either"; the csproj pins are `1.11.2`, `1.11.1`, `1.11.2-beta.1`, `1.11.0-beta.1` | the whole OpenTelemetry line moves to 1.15.x; re-read whether those two pre-release packages still carry a `-beta` suffix at the new version before trusting that sentence |
| `memory-bank/standards/tech-stack.md:56-58` | "Package versions are pinned inline in each `.csproj`; as of 2026-09-04 there is no `Directory.Packages.props` and no `Directory.Build.props`. Central package management arrives with bolt 054." | false after the merge — rewrite it to describe `Directory.Packages.props` as the single version source, and drop the forward reference to 054 |
| `memory-bank/standards/tech-stack.md:82-89` | the `ci.yml` description ("no lint step"; runs on pull requests and on pushes to every branch except `main`) and the `deploy.yml`/`ci.yml` trigger-mismatch paragraph | 054 edits `ci.yml`; re-read the trigger sentence, the "no lint step" claim, and whether the mismatch paragraph still holds |
| `docs/ARCHITECTURE_AUDIT_CHECKLIST.md:28` and the note at `:37` | step 1.3 opens "the Renovate dependency dashboard issue"; the note says the Renovate configuration arrives with 054 and that a missing dashboard issue is itself the finding | `.github/renovate.json` arrives — delete the note, and confirm the dashboard issue actually exists (Renovate opens it only after its first run against the repo) |
| `docs/ARCHITECTURE_AUDIT_CHECKLIST.md:55-56` | baseline "27 direct NuGet references in `PhotoPrint.API`, 13 in `PhotoPrint.Tests`" | central package management keeps the references but strips their versions; re-count and say where the versions now live |
| `docs/ARCHITECTURE_AUDIT_CHECKLIST.md:57-59` | baseline row: two OpenTelemetry packages on `-beta.1`, "so this is not drift" | re-measure at 1.15.x — if a stable line now exists for either, the row turns into a real step-2.3 finding |
| `docs/ARCHITECTURE_AUDIT_CHECKLIST.md:64-66` | step 2.4's known instance: a `Stripe.net` version pinned that had never been published, and "bolt 054 turns that substitution into a build error, so after it merges this step is enforced mechanically" | confirm 054 actually added the strictness (a version-pinning or audit setting), not only the version bump. If it only bumped, this sentence overclaims and must be cut back to the history |
| `docs/ARCHITECTURE_AUDIT_CHECKLIST.md:17-19` | the baseline is stamped "measured on 2026-09-04 at `main` = `182cd50`" | the stamp is history and stays true, but it already predates `f2e70ad` and will predate 054; section 1 and 2 numbers move — restamp at the first real audit run |

## For the coordinator

Three things this bolt found and could not fix inside its own writable surface.

1. **`docs/DEPLOYMENT.md` §12.6 (line 698) — two wanted edits**, in a file the dependency-hardening
   group owns this wave. (a) Add a cross-link to `docs/architecture/multi-replica-readiness.md`, now
   the consolidated answer that section paraphrases. (b) The section opens "The current design is
   correct under multi-replica" and credits "Sameday's `awbPayment` external-reference idempotency"
   for absorbing duplicate `CreateAwb` calls — ADR-015's own amendment retracts that idempotency
   claim, and duplicate-create is held off by a durable per-order lease instead. As written it tells
   an operator scaling out that something is safe which the amendment says is not.
2. **The deploy chain cannot fire.** `.github/workflows/deploy.yml` filters its `workflow_run`
   trigger to `branches: [main]` while `.github/workflows/ci.yml` carries
   `push: branches-ignore: [main]`, so no CI run on `main` ever exists to chain from and only
   `workflow_dispatch` reaches deploy — while `deploy.yml`'s own header comment describes the
   automatic chain. The docs describe the reality (`tech-stack.md:82-89`); the fix is a workflow
   change, off-limits to this bolt and overlapping 054's `ci.yml` edit.
3. **Nothing schedules the quarterly audit.** `docs/ARCHITECTURE_AUDIT_CHECKLIST.md` says so in its
   own opening paragraph. A recurring calendar entry or GitHub issue for end of March, June,
   September and December is an owner action; no file in this repo can carry it.

## Notes

Documentation only; aligns with [[project_bolt_046_deprioritized]]. P19 is pre-launch must-have.
