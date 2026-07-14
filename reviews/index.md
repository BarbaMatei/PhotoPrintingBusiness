---
type: review-index
updated: 2026-07-04
---

<!-- 2026-06-19: v8 is an independent fresh clean-room audit of the SAME commit (50fc692),
     run without reading v1–v7. It re-surfaced the two prior accepted-deferrals (Postgres
     test infra = DB-1, codebase-wide dup = QUAL-3) and added several lower-severity items. -->

# Review Index

Running log of multi-lens reviews. See [README.md](README.md) for the process.

Findings column is H / M / L / Cleanup. Status tracks the resolution loop: `open → in-progress → resolved` (and `verified` once a re-review confirms).

| Date | Target | Branch | Verdict | Findings | Status | Latest review | Resolution |
|------|--------|--------|---------|----------|--------|---------------|------------|
| 2026-07-14 | Bolt 042 — thumbnail cache (v4 fresh discovery) | `feat/bolt-042-thumbnail-cache` | Approve-with-followups (v4 discovery — no blockers) | 0 / 11 / 14 / 7 | **Open** — first blinded discovery pass since v1; **all three v1 blockers stayed closed** (fixes held). 32 findings, none High: the notable mediums are **fix-generated** (deterministic-key created a concurrent-write 500 + a write-vs-cleanup orphan) plus a decode-concurrency OOM (re-raises v3 deploy note), batch bomb-event gap, HEIC over-accept, and security-path test gaps. 515/515 .NET · 403/403 FE. Not saturated → closure wants one more quiet discovery pass. First real run of [discovery-review.wf.js](lib/discovery-review.wf.js) (11 lenses/51 agents/2.0M tok; efficiency measures + hinted-flag worked; args-string bug found+fixed mid-run). | [v4](042-thumbnail-cache/review-v4.md) | [v4](042-thumbnail-cache/resolution-v4.md) |
| 2026-07-14 | Bolt 042 — thumbnail cache (v3 superseded) | `feat/bolt-042-thumbnail-cache` | Approve-with-followups (v3 verification — all findings resolved) | 3 / 8 / 14 / 3 | **v1: 26 verified (review-v2). v2 follow-ups: NEW-1/NEW-2/NEW-4 verified (review-v3, @`f8b1325`), NEW-3 deferral accepted → bolt-043 orphan sweep; CLOUD-1 deferred.** 0 reopened across v2+v3; each fix proven revert→red + independent verifier. v3 caught+fixed 2 comment drifts in-pass. Suites green: **.NET 515/515**, **frontend 403/403**. Deploy-time note: decode concurrency limit (NEW-1 raised per-preview decode to ~400 MB). Feature-closure still wants a saturated discovery pass. | [v3](042-thumbnail-cache/review-v3.md) | [v2](042-thumbnail-cache/resolution-v2.md) |
| 2026-07-04 | Bolt 035 — payment idempotency | `feat/bolt-035-payment-idempotency` | Approve-with-followups (v10 verification — resolution loop complete) | 0 / 2 / 9 / 7 | **14 verified · 4 accepted-deferred · 0 open.** v9 (4 anchored lenses) verified 13 fixes + re-affirmed the migration/deploy deferrals (DB-1, DB-2, SEC-1, BUG-2) sound + reopened OBS-3 for incomplete doc alignment; v10 (1 lens) verified the OBS-3 completion (@065a516). 0 regressions, tenant isolation intact, **487/487 green**. Fix loop complete; feature-closure still wants a saturated discovery pass. | [v10](035-payment-idempotency/review-v10.md) | [v8](035-payment-idempotency/resolution-v8.md) |
| 2026-06-19 | Bolt 035 — payment idempotency (superseded) | `feat/bolt-035-payment-idempotency` | Approved (v7 re-review, 0 blockers) | 0 / 3 / 6 / 6 | **Loop complete** — 13 verified, 2 accepted-deferred (DB-1: migration-deploy infra; QUAL-3: codebase-wide pattern), 0 open. v7 verified QUAL-4 @fbb4c7c. 474/474 green. | [v7](035-payment-idempotency/review-v7.md) | [v5](035-payment-idempotency/resolution-v5.md) |

## Backlog / improvements to the system

- ~~Encode the lens fan-out as a `Workflow` script~~ **done** → [lib/discovery-review.wf.js](lib/discovery-review.wf.js) (generic lenses + args-driven scope + severity-tiered skeptics; main agent still does scoping + synthesis).
- Add a reusable DB/migration-parity lens (dual SQLite/Postgres is a recurring risk here).
- Auto-append findings as inline PR comments once `gh` is available in the environment.
- **Redesign codePack (#4).** Authoring a ~25k-token pack into the workflow `args` is impractical (the main agent would emit it verbatim) and a path-based pack forces every agent to over-read. Options: a prep sub-agent that builds+caches the pack, or drop #4 and rely on targeted per-lens reading (what v4 did). (Harness quirk found in v4: `args` arrives as a JSON *string* — the script now `JSON.parse`s it defensively.)
