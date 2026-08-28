---
type: runbook
for: verification passes (after a fix round)
updated: 2026-08-28
---

# Verification runbook

Anchored, per-fix, cheap — the opposite posture of discovery. No blinding, no manifest, no
codePack, no workflow script. The question is "did each specific fix hold?", never "is the
feature clean?".

1. The verification runs at the fixed round's tip, immediately after the fixer's hand-back,
   inside the same reviewed unit — commit any pending `reviews/` edits first (the script refuses
   a dirty tree). Read the latest `review-v<n>.md` + `resolution-v<n>.md`; check out the
   resolution's `fixed_commit`. You must not be the fixer — sole exception (calibration 2026-07-29): a
   **test-only** fix round (zero production-code changes) may be self-verified when every fix
   carries a revert-and-rerun proof whose failing-test set was predicted before the revert and
   matched exactly, recorded in the resolution. Any production-code change voids the exception.
2. **Revert-and-rerun — every round, every `fixed` row.** Revert the fix (source only); its
   regression test must go red with clean attribution and zero collateral; restore, green. A fix
   whose test cannot go red is not verified — reopen it. Run it mechanically over the whole
   round:

   ```
   node reviews/lib/verify-fixes.mjs <target>
   ```

   It prints one verdict line per row and stamps one `verify-result` event per row, which step 5
   renders the ledger flips from. `--only PPW-1,PPW-2` re-runs named rows; it is never the first
   pass over a round. The red leg counts only when the runner output names a failing test
   (recorded in `red_evidence`); a revert that breaks compilation — or reddens with nothing
   attributable — is `revert-broke-build`, never red. The tree must be clean or the run refuses.

   **Rows the script cannot verdict** — `revert-broke-build`, `env-missing`, `test-never-red`,
   `revert-failed`, `green-failed` — come back to you. Re-prove such a row by its smallest lever
   by hand, never by trusting a non-zero exit. A row you prove held by hand needs its own stamp,
   or step 5 leaves it `fixed`:

   ```
   node reviews/lib/wl.mjs <target> verify-result --id PPW-<n> --verdict held --commit <sha>
   ```

   The renderer keeps the last `verify-result` per id in the pass span, so this supersedes the
   script's verdict for that row. Reopen every row you cannot prove, and leave the script's
   verdict standing for it.

   **Evidence audit — when the round recorded its own proofs (2026-08-28).** A round whose
   resolution records a single-lever revert proof for **every** `fixed` row (the lever and the
   red evidence, per the `/fix-review` contract) also gets its claims checked: an agent who is
   not the fixer reads the recorded red/green evidence and **re-runs a random 2–3 rows** by their
   recorded lever. This audits the fixer's record; it does not replace the run above. Every
   sampled row reproduces → the record stands. **Any sampled row that fails to reproduce is a
   finding of its own: reopen it and record that this target's recorded proofs are unreliable,
   this round and every later one.** The `verified` flip always belongs to this re-review, never
   to the fixer's own record.
3. **Judgment items** (doc fixes; `wont-fix` / `deferred` / `disputed` rationales): first run
   `git diff <last-affirmed-commit>..HEAD -- <cited files>` yourself. Unchanged → record
   "unchanged since `<commit>`, stands" with **no agent**. Changed → one anchored Explore
   agent, given the finding + resolution note + fix delta, not the whole feature. Update each
   ledger row's last-affirmed commit.
4. **Read the round review before dispatching anything.** Rounds from 2026-08-28 carry one
   round-scope composition review (`round-review-returned` in the worklog, findings folded
   into the resolution) — read what it found and left open; do not re-run its questions
   with new agents. Only for an older round without one, ask the three questions per fix
   cluster yourself, by the owning lens:
   - *Class or instance* — do sibling sites still carry the defect?
   - *New surface at the bar* — does each added mechanism have sized defaults, a signal,
     failure-mode tests, docs?
   - *Regression* — did the fix change adjacent behavior?
   Asking only the regression question is the documented failure mode ([rationale](../notes/rationale.md)).
5. **Write no prose files** (artifact rules of 2026-08-10, [doc-contracts.md](../rules/doc-contracts.md)).
   Run

   ```
   node reviews/lib/render-records.mjs <target> --verification <pass> --outcome "<one line>"
   ```

   — it renders the [metrics.jsonl](../rules/metrics-schema.md) line, the
   [index.md](../state/index.md) row, and the ledger flips (`verified` at the proved-at commit
   for a held fix, back to `open` for a reopened one, one History line each) from the
   `verify-result` events of step 2 — the script's, plus any you stamped by hand. A `fixed` row
   with no `verify-result` in this pass's span is not flipped and stays `fixed`; the renderer
   says so when it finds no events at all. Add `--new-findings h,m,l,c`
   when the pass named new defects. Commit the worklog the run left behind, then **push** — the
   round's tip and the records commit both, because the auditor refuses a commit reachable from
   no pushed ref and these records carry shas written a moment ago. Then run
   `node reviews/lib/records-auditor.mjs <target>` — it must exit clean — and the unit's single
   doc gate covers this pass's records together with the fix round's. A verification that
   names **new** defects reconciles them like any pass and adds a `findings[]` entry per new
   defect — `{d, new, sev, fix_generated, seed_round, area}` — so fix lineage is counted
   where it surfaces; `seed_round` names the fix round whose commits caused it and `area`
   its component word, both the reconciler's judgment, `null` when unattributable (never
   guessed — the router reads a missing value as "not yet measured"). The verdict — at most
   `approve-with-followups`; a quiet verification means "the fixes held", never "the code is
   clean" — goes in the index row. Report the outcome at the owner gate in chat; any owner
   decision made there is recorded on the ledger row it concerns.

Cost: scale agent count to the fix size — a 50-line fix doesn't need 8 finders. Give agents
the saved fix diff, not the repo.
