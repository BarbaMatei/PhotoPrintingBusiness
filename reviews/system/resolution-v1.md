---
type: resolution
target: review-system
review: review-v1.md
status: in-progress
fixed_commit: 1daedb9
date: 2026-07-29
findings:
  SF1:  { status: fixed,          commit: 08e5a3e, note: "R1 calibration — first cert attempt = pair; re-certification after small verified fix round = one fresh full pass (standard, not deviation); full-loop tier never closes without a fresh full pass after the last fix round. README note ² + runbook-discovery + rationale entry" }
  SF2:  { status: fixed,          commit: 08e5a3e, note: "R2 — README front page now states what certified means (no 🔴, every 🟠 decided, not zero defects); cert index rows record Mediums open at close" }
  SF3:  { status: fixed,          commit: 08e5a3e, note: "HINTS updated to two-tier IStorageRouter reality; dedup hinted-topic list updated in the same edit" }
  SF4:  { status: fixed,          commit: none,    note: "3 tags pushed to origin (review-evidence/015, /042, /043) at the local branch tips — every cited commit now reachable from a pushed ref; ongoing check assigned to the records auditor (SF5)" }
  SF5:  { status: fixed,          commit: 1daedb9, note: "schema v2 (per-finding lens/verdict/fix-lineage/sev_delta) + records auditor; seeded fixture 12/12 caught, real records 0 errors, runbook-gated after every append" }
  SF6:  { status: fixed,          commit: 1daedb9, note: "fix_generated field in schema v2 findings[]; first data lands on the next discovery pass" }
  SF7:  { status: fixed,          commit: 1daedb9, note: "pass-cost-only scope note written into the schema; per-round metering declined for now" }
  SF8:  { status: fixed,          commit: 08e5a3e, note: "stale '3 of 5' corrected in wf.js, reconcile-findings SKILL, README; blind-first re-judge probe left optional" }
  SF9:  { status: fixed,          commit: 09173c4, note: "sweep complete: 371 tracked + all bare-ID and short-token leftovers -> 0 across three nets; auditor enforces 0 on every run; backend 1001 green, UI flakes green isolated. Residual: discovery-workspace reachability check = design-doc tool #3, tracked there" }
  SF10: { status: false-positive, commit: null,    note: "refuted by the defense checker (priced-in: design-doc assumption 2 + planned tool #4); residue (severity-delta logging) folded into schema v2" }
  SF11: { status: false-positive, commit: null,    note: "refuted by the defense checker (launching labor is the main agent's; human acts are the designed gates); residue = the loop-driver, already the build plan" }
  SF12: { status: fixed,          commit: 1daedb9, note: "records auditor cross-checks tallies vs findings[], review↔metrics pairing, and cited commits on every append" }
  SF13: { status: open,           commit: null,    note: "model check + auto-resume fold into the loop-driver — build item 5" }
  SF14: { status: deferred,       commit: null,    note: "owner 2026-07-29: seeded run 2 'not now'; R3 re-scope NOT approved — the design-doc gate stays as written, deliberately visible, as the standing reminder the test is owed" }
  SF15: { status: fixed,          commit: 08e5a3e, note: "R4 — test-only self-verify exemption written into runbook-verification step 1; the 015 ledger's 'expiry: next calibration' marked resolved" }
  SF16: { status: fixed,          commit: 878beb6, note: "track-record.md seeded with both certifications; escape rule in README + reconcile-findings skill (unsure → count it, doubt stated); auditor errors when a certified target is missing from the file" }
---

# Resolution v1 — review-system

Owner decisions this round (2026-07-29): **R1, R2, R4 approved and applied. R3 not approved** —
the seeded-run-2 trust gate in [self-driving-loop-design.md](../self-driving-loop-design.md)
stays as written even though practice has run ahead of it; the owner prefers the violated rule
visible over a re-scoped one. Seeded run 2 itself: deferred ("not now"). Evidence preservation:
tags chosen over branch re-push (remote branches were deleted deliberately post-merge).

| SF# | Sev | Status | How |
|-----|-----|--------|-----|
| SF1 | 🟠 | fixed | R1 in README note ², runbook-discovery step 1, rationale calibration entry |
| SF2 | 🟠 | fixed | R2 in README front page + note ² (🟠-open-at-close index convention, forward-looking) |
| SF14 | 🟠 | deferred | owner decision recorded above; SF16's scorecard (now live) is the interim measure |
| SF16 | 🟠 | fixed | track-record.md + README/reconciler rule + auditor enforcement |
| SF4 | 🟠→🟡 | fixed | `review-evidence/{015,042,043}` tags pushed to origin (verified via ls-remote) |
| SF3 | 🟡 | fixed | wf.js HINTS + dedup topic list |
| SF5 | 🟡 | fixed | schema v2 + records auditor @1daedb9 (fixture 12/12, real records 0 errors) |
| SF6 | 🟡 | fixed | `fix_generated` in schema v2 findings[]; first data next discovery pass |
| SF9 | 🟡 | fixed | sweep @09173c4 (371→0, three nets, suites green); workspace check = tool #3 |
| SF15 | 🟡 | fixed | R4 in runbook-verification step 1; 015 ledger expiry resolved |
| SF7 | ⚪ | fixed | pass-cost-only scope note in schema v2 |
| SF8 | ⚪ | fixed | three stale quotes corrected |
| SF12 | ⚪ | fixed | auditor cross-checks on every append, runbook-gated |
| SF13 | ⚪ | open | folds into loop-driver |
| SF10 | — | false-positive | defense checker verdict, recorded in review-v1 |
| SF11 | — | false-positive | defense checker verdict, recorded in review-v1 |

Remaining build order (owner-sequenced, from review-v1 recommendations): comment-citation
sweep → loop-driver skill. Parked: seeded run 2 (SF14). Verification of this round's fixes = the future re-grade
(`review-v2` of this target), which re-scores the locked baseline scorecard.
