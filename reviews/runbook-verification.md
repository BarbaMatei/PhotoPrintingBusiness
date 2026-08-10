---
type: runbook
for: verification passes (after a fix round)
updated: 2026-07-24
---

# Verification runbook

Anchored, per-fix, cheap — the opposite posture of discovery. No blinding, no manifest, no
codePack, no workflow script. The question is "did each specific fix hold?", never "is the
feature clean?".

1. Read the latest `review-v<n>.md` + `resolution-v<n>.md`; check out the resolution's
   `fixed_commit`. You must not be the fixer — sole exception (calibration 2026-07-29): a
   **test-only** fix round (zero production-code changes) may be self-verified when every fix
   carries a revert-and-rerun proof whose failing-test set was predicted before the revert and
   matched exactly, recorded in the resolution. Any production-code change voids the exception.
2. **Revert-and-rerun every `fixed` finding:** revert the fix (source only), its regression
   test must go red with clean attribution and zero collateral; restore, green. A fix whose
   test cannot go red is not verified — reopen it.
3. **Judgment items** (doc fixes; `wont-fix` / `deferred` / `disputed` rationales): first run
   `git diff <last-affirmed-commit>..HEAD -- <cited files>` yourself. Unchanged → record
   "unchanged since `<commit>`, stands" with **no agent**. Changed → one anchored Explore
   agent, given the finding + resolution note + fix delta, not the whole feature. Update each
   ledger row's last-affirmed commit.
4. **Review the fix diffs — three questions per fix cluster**, asked by the owning lens:
   - *Class or instance* — do sibling sites still carry the defect?
   - *New surface at the bar* — does each added mechanism have sized defaults, a signal,
     failure-mode tests, docs?
   - *Regression* — did the fix change adjacent behavior?
   Asking only the regression question is the documented failure mode ([rationale](rationale.md)).
5. **Write no files** (artifact rules of 2026-08-10, [doc-contracts.md](doc-contracts.md)).
   The pass's record is: ledger status flips (`verified` for held fixes, reopen failures) with
   one History line per row touched, worklog events, the [metrics.jsonl](metrics-schema.md)
   line and the [index.md](index.md) row (then run
   `node reviews/lib/records-auditor.mjs <target>` — must exit clean). The verdict — at most
   `approve-with-followups`; a quiet verification means "the fixes held", never "the code is
   clean" — goes in the index row. Report the outcome at the owner gate in chat; any owner
   decision made there is recorded on the ledger row it concerns.

Cost: scale agent count to the fix size — a 50-line fix doesn't need 8 finders. Give agents
the saved fix diff, not the repo.
