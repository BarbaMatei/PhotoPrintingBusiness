Verification pass v2 ran on 097-mockpass, and the fix it was meant to check does not exist. resolution-v1 names commit `406f0d9` as the fix, but that is the same commit review-v1 reviewed and rejected, and it only adds review paperwork (`reviews/095-postcert/*`, `reviews/track-record.md` — 6 files, 32 lines, no program code, no test). No file outside `reviews/` has ever been tracked in that repo on any branch or tag, so the claimed "red pre-fix, green post-fix" regression test cannot exist. F1 is reopened, not verified.

Honest limit: this was a paper pass. With no code and no test suite I could not do the runbook's revert-and-rerun, and its step-4 fix-diff questions (do sibling sites still have the defect, is the new mechanism up to the bar, did the fix break anything nearby) are unanswerable with no diff. Both limits are written into the records as unanswered, not glossed as clean.

Records produced, all inside the fixture's `reviews/097-mockpass/`:
- `review-v2.md` — pass-type verification, verdict `request-changes`, F1 reopened. It is also the status of record, since this target has no `ledger.md`.
- `metrics.jsonl` — pass-2 line (`verified: 0`, `reopened: 1`, `tests: null`, `cost.agents: 0`), plus a separate appended correction line flagging pass 1's "9 tests passed" as having no possible source. No past line was edited.
- `summary-v2.md` — owner summary, 2 decisions needed, every claim linked.
- Skipped per your instruction: ledger update and index row (the fixture has neither file).

Auditor exits clean (0 errors; the one warning is the missing `reviews/index.md`). I committed nothing — the fixture had an unrelated modified file, and both commits my records cite are already tag-reachable, so the auditor's evidence check passes without a commit.

Router now says the next pass is a fix round, unmetered, via `/fix-review`. I stopped there rather than starting it: one pass per invocation.

One judgment call for you: I appended the metrics correction myself instead of leaving a number I know is unsupported. That is append-only and schema-legal, but say if you would rather corrections wait for your sign-off.
