# Working rules for a stage session

You are one session for one stage of one bolt. Work that stage, write its artifacts, and end. Every step you take re-reads this whole conversation, so keep it short and small.

1. **The files are the memory; this conversation is a cache.** Start from `bolt.md`, the bolt type definition, the unit brief and the last stage-exit block in the unit's `construction-log.md`. When the stage's artifacts are written, update the `bolt.md` frontmatter (`current_stage`, `stages_completed`), append the stage-exit block below to the construction log, commit, and end the turn. Never start the next stage, and never ask to continue.
2. **Slow things last.** Anything that takes more than a few minutes (a scoped test run, a review subagent, a build) runs as the last act of the stage, right before the stage-exit block. Keep every other step short.
3. **Many questions per step.** Put every read, search or check that does not depend on another's result into the same turn; several shell commands go in one call.
4. **Small answers.** Read line ranges, never whole files; search with narrow context. Run tests only through the wrapper, `node reviews/lib/run-scoped-tests.mjs <bolt-id> --kind <red|green|revert-and-rerun> --filter "<FQN fragment>" --summary` (UI: `--ui --include "<name>"` instead of `--filter`): with `--log memory-bank/bolts/<bolt-id>/test-stamps.jsonl` for every test named in `failure-modes.jsonl`, with `--no-events` for any other run. For TypeScript symbol questions (who calls X, where is X defined) use the LSP tool, not grep. Filter build output to errors.
5. **Your cost is measured when you exit.** Do not re-read what you just wrote, do not recap, do not linger after the stage-exit block.
6. **Failure modes first, and proven.** The design/plan stage writes `memory-bank/bolts/<bolt-id>/failure-modes.jsonl` (one JSON row per failure mode, its test named as the exact `--filter`/`--include` fragment; the adversarial check's rows appended with a disposition). The implement stage runs each named test red before the code, green after, red again under `--kind revert-and-rerun --mutate <production file>:<line>` (the wrapper breaks the line and restores it), then green. `node .specsmd/aidlc/scripts/check-stage-exit.mjs <bolt-id> <stage>` tells you what is still missing; the pre-commit hook runs it on the commit that marks the stage complete and refuses the commit until it passes. Never bypass the hook.

## The stage-exit block

Append it to the end of the unit's `construction-log.md`, exactly in this shape (the launcher parses the heading and reads to the end of the file or the next `## ` heading):

```
## Stage exit — <bolt id> — <stage> — <ISO timestamp>
- Done: <what this stage produced, with file paths>
- Decisions: <choices taken and why, one line each>
- Dead ends: <what was tried and dropped, so nobody retries it>
- Next: <the exact first step of the next stage, or "bolt complete">
```

Timestamps are `YYYY-MM-DDTHH:MM:SSZ`.
