# Building bolts without paying for the same memory five hundred times

*A plan for running the construction groups cheaply — written 2026-09-04 by the Wave 1 coordinator,
after measuring what the first day of Wave 1 actually cost. Status: Phase 0 applied on branch
chore/cost-efficient-construction (PR pending); Phases 1–3 pending.*

---

## 1. What we learned, in plain words

An AI session has no memory of its own. Every time it takes a step — reads a file, runs a test,
edits a line — it is handed the **entire conversation so far** and asked "what next?". The provider
keeps a copy of that conversation in a short-term cache so the re-read is cheap (about a tenth of
the normal price), but it is never free, and the cache forgets after **five minutes** of silence.

So the bill for a working session is, roughly, **number of steps × size of the conversation at each
step**. A long session with many small steps pays for its own history over and over. That is what
happened on day one:

| Group | Steps | Conversation re-read (tokens) | Fresh reading | Wasted on cache misses |
|---|---|---|---|---|
| Coupons (047) | 465 | 169M | 2.1M | 44% of the fresh reading |
| UI scaling (066-067) | 669 | 209M | 2.0M | 53% |
| Dependencies (054) | 377 | 96M | 2.9M | 71% (six of nine misses were my kills) |
| Architecture docs (057) | 227 | 43M | 1.0M | 36% |
| Review verification (085-086) | 330 | 79M | 1.9M | 59% |

*A token is roughly three-quarters of a word. "Cache miss" = a moment where the five-minute cache
had expired, usually after a long test run or a paused subagent, and the whole conversation had to be
re-sent at full price.*

Three things stand out:

1. **The re-read column dwarfs everything else.** The UI group re-read 209 million tokens to write
   about two million. That is the quadratic law at work: the later the step, the bigger the history.
2. **A handful of moments cost a third to two-thirds of the fresh reading**: 2–9 cache misses per
   group. Long waits are expensive not because of the wait, but because of what comes after it.
3. **Reviewing was not the cost.** Each group launched only 2–7 review helpers. The implementation
   loop — read, edit, test, repeat — was.

Nobody did anything wrong. The tools default to this shape. The plan below changes the shape.

---

## 2. The five rules

### Rule 1 — The transcript is a cache; the files are the memory
Each stage of a bolt (plan, design, implement, test) becomes **its own session**. At the end of a
stage the session writes its state to the files the method already produces (`bolt.md`, the
design document with its caller table, the unit's `construction-log.md`), adds a short **"where I
stopped, what comes next"** block to the construction log, and **exits**. The next stage starts as a
**brand-new session** that reads those files and nothing else. We never resume a finished stage with
`claude --continue`; that drags the whole old conversation back in. `--continue` is only for a
session that was interrupted mid-stage.

*Why it works:* four sessions of 130 steps each starting from a 40k-token conversation cost a
fraction of one 669-step session that grows to 300k, because the expensive late steps never happen.

#### The stage-exit block

The session appends it to the end of the unit's `construction-log.md` right before it exits; the
launcher reads it back into the next stage's prompt, taking everything from the last such heading
to the end of the file or the next `## ` heading:

```
## Stage exit — <bolt id> — <stage> — <ISO timestamp>
- Done: <what this stage produced, with file paths>
- Decisions: <choices taken and why, one line each>
- Dead ends: <what was tried and dropped>
- Next: <the exact first step of the next stage, or "bolt complete">
```

### Rule 2 — Slow things last
Anything that takes more than a few minutes — the full test run, the review helper, a build — is
scheduled as the **last act of a stage**, right before the session exits. If the cache expires
during it, nothing is lost: nobody re-reads that conversation again. Inside a stage, steps stay
short so the cache stays warm.

*Coordinator corollary:* never kill a session mid-stage. Soft stops happen at stage boundaries. A
kill costs one full re-read on relaunch (about 300k tokens each on day one).

### Rule 3 — Many questions per step
The harness lets a session ask several things at once and pay one re-read for all the answers. Day
one averaged 0.7 tool calls per step. The rule: **every read or check that does not depend on
another's result goes in the same step**; several shell commands go in one call. A realistic
average of three per step cuts the number of steps — and the bill — by about three.

### Rule 4 — Small answers
What comes back from tools is the slope of the growing conversation. Rules: read line ranges, never
whole files; search with narrow context; tests through the summary wrapper that prints only counts
and failing test names; builds filtered to errors. For TypeScript questions like "who calls this",
use the language server (already installed for every worktree; nobody used it on day one) — one
precise answer instead of a search plus eight file reads. And cap the conversation itself: the CLI
has `--autocompact <tokens>`, which folds the history into a summary when it reaches a chosen size.
We set it around 120k so a stage that runs long cannot climb to 300k.

### Rule 5 — Measure every stage
A small script reads the session's transcript when it exits and appends one line to the
construction log: stage, steps, tool calls, fresh tokens, re-read tokens, output tokens, cache
misses. The coordinator rolls the lines up per wave. Construction gets the same kind of cost
record the review loop already keeps in `metrics.jsonl`, and next wave's rules are tuned from data.

---

## 3. What changes, where

| Where | Change | Rule | Who decides |
|---|---|---|---|
| `CLAUDE.md` (done) | A six-line "Working cheaply" block: batch independent calls; line ranges not whole files; summary test output; language server for TS symbol questions; exit at stage boundaries. Loaded by every session, so it is the cheapest place to enforce. | 3, 4, 1 | **Owner** (it is the project's instruction file) |
| Kickoff prompts (`bolt-parallel-plan-2026-09-03.md` §8 blocks + the coordinator addendum) | Launch is per stage, not per group: the prompt names the bolt **and** the stage, says "exit when the stage's artifacts are written", and carries the five rules. | 1, 2, 3, 4 | Coordinator |
| `.specsmd/aidlc/scripts/launch-stage.ps1` (done) | Builds the stage prompt from `bolt.md` + the construction log's last stage-exit block; runs `claude -p` with `--autocompact 120000`, `CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0`, `--exclude-dynamic-system-prompt-sections` (better cache reuse) and the rules appended as system prompt; when the session exits, calls the cost script. One command per stage instead of hand-written resume files. | 1, 2, 4, 5 | Coordinator writes; owner approves the location |
| `.specsmd/aidlc/scripts/working-rules.md` (done) | The five rules as direct instructions to a working session, plus the stage-exit block format; appended to every launched session's system prompt. | all | Coordinator |
| `.specsmd/aidlc/scripts/session-cost.mjs` (done) | Reads the newest transcript for a worktree, appends the cost line to the construction log. The measurement in §1 is its prototype and reproduces it exactly. | 5 | Coordinator |
| `reviews/lib/fix/run-scoped-tests.mjs` (done) | Add `--summary`: print only totals and failing test names. Groups call it with `--no-events` (no review records touched). Bonus: it already holds the one-test-process-at-a-time machine lock, so four groups can no longer overload the PC. | 4 | Coordinator (a `reviews/lib` change runs its fixture suite before hand-back) |
| `memory-bank/standards/bolt-process.md` | New section "Session boundaries": one session per stage, the stage-exit block, slow steps last, the cost line. Written **after** Wave 1 proves it — standards describe reality. | all | Coordinator drafts, owner merges |
| `.specsmd/aidlc/templates/construction/bolt-types/*.md` | Optional, later: a "stage exit" activity in each bolt type so the method itself asks for the block. Not needed for Wave 1; the kickoff carries it. | 1 | Owner, after Wave 1 |
| Coordinator practice (`wave-1-coordinator-log.md`) | No mid-stage kills; soft stop = "finish the stage, exit"; relaunch = fresh session, not `--continue`; per-wave cost roll-up. | 2, 5 | Coordinator |

Not changing: the model, the effort level (`xhigh` stays), the review loop, the number of parallel
groups, the bolt method's stages and gates.

---

## 4. How we execute

**Phase 0 — tooling, before the relaunch (coordinator, ~2–3 hours of session time)**
1. Write `session-cost.mjs` and run it over day one to lock the baseline table above.
2. Add `--summary` to the test wrapper; run its fixture suite.
3. Write `launch-stage.ps1`.
4. Draft the six `CLAUDE.md` lines for the owner's yes/no.
5. Rewrite the four resume prompts as stage prompts: each group finishes its current stage, writes the
   exit block, exits. From then on every stage is a fresh launch.

**Phase 1 — Wave 1 continues under the rules**
Relaunch the four groups with the launcher. Coupons has the most stages left (rest of 047's test
stage, then 048 plan/implement/test) and is the real test of the approach. The coordinator relaunches
each next stage when the exit block appears — automatic once the launcher watches for the exit.

**Phase 2 — measure at the wave boundary**
Compare the per-stage lines against the day-one baseline. Target: re-read tokens per bolt down by
at least 4× for the same stages; cache misses near zero except at stage ends. If a group's stage
still climbs past ~150 steps, split that stage in the kickoff for Wave 2.

**Phase 3 — make it the standard (Wave 2 onward)**
`bolt-process.md` gains the session-boundaries section; the plan's §8 kickoff blocks for Waves 2–11
are regenerated per stage; the bolt-type templates get the stage-exit activity if the owner wants the
method itself to carry it.

---

## 5. What to expect, honestly

- The **re-read** bill should fall roughly 4–6× for a group like UI scaling (209M → 30–50M). Rule 1
  does most of it, Rule 3 most of the rest.
- **Cache misses** should nearly vanish from inside stages (Rule 2), which alone is a fifth to a
  quarter of the day-one bill.
- **Fresh reading** goes up slightly: every new session re-reads the instructions and its bolt files
  (~40–60k tokens per launch). Against 300k-token re-reads that is noise, but it is real.
- **Quality risk:** a fresh session knows only what the files say. The stage-exit block has to be
  good — decisions taken, dead ends, exact next step. Day one already showed the method's files are
  enough to resume (every group resumed correctly from them after the pause). The design check and
  fresh-eyes gates stay exactly as they are.
- **Unknown:** whether the CLI can use the one-hour cache for these sessions (this coordinator's
  session has it; no flag for it is visible). If it can, Rule 2 matters less; if not, Rule 2 carries
  the weight. Checked in Phase 0.

---

## 6. Decisions for the owner

1. Approve the six-line `CLAUDE.md` block (text supplied in Phase 0).
2. Autocompact size: 120k proposed. Lower is cheaper, higher keeps more working memory in a long stage.
3. Script location: `.specsmd/aidlc/scripts/` proposed (next to `bolt-complete.cjs`); alternative `tools/`.
4. Whether to fold the stage-exit activity into the bolt-type templates now or after Wave 1.
