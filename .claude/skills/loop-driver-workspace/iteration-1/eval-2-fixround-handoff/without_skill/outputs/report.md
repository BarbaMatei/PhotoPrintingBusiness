The router puts 091-fixround at a fix round, and that is where I stopped — running it means
launching the /fix-review skill, which this exercise forbids. No owner decision is pending: the
fix-round row needs no go-ahead, so the loop is blocked only on executing that pass.

Router readout — `node reviews/lib/route-next-pass.mjs --root "C:/Users/mbarba/AppData/Local/Temp/claude/d--photo-printing-website/6994f33b-0b77-431d-9dc3-262f6c68ae9c/scratchpad/router-fixture" 091-fixround` (exit 0):

- Latest review v1, discovery pass, verdict `request-changes`, 3 new serious findings (1 high,
  2 medium), 0 reopened.
- No resolution file answers review-v1, so README router row 2 applies: fix round.
- Cost: unmetered, scales with finding count. Nothing goes to the ledger backlog — the low and
  cleanup counts are both 0.

Records audit — same `--root`, `node reviews/lib/records-auditor.mjs ... 091-fixround` (exit 0):
0 errors, 1 warning, and the warning is only that the fixture root has no `reviews/index.md`. The
cited commit 75dd301 resolves and is reachable from tag `fixture-base`, so that evidence link holds.

One caveat worth knowing: the fixture's `review-v1.md` is a stub whose whole body is "fixture
review", with no findings table, and there is no `findings-v1.md` or `summary-v1.md`. The routing
is correct, but a real fix round would have nothing to read. I changed no file in the repo or the
fixture.
