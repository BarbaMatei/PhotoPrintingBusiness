---
name: loop-driver
description: >-
  Drive the review loop for a target: mechanically pick the next pass, announce its cost,
  stop at every owner gate, run it, and leave the records clean — one pass per invocation.
  Use this whenever the user says "continue the review loop for <target>", "drive the loop",
  "next pass for <target>", "what's next for <target>", "keep the loop going", or names a
  review target and asks to review/re-review/certify/verify it — even if they don't say the
  word "loop". Also use it when unsure where a target stands in the review process: the
  router readout is the answer. The mechanical router is the decision surface — hand-read
  the README router table only when the router itself abstains ("no row matched").
---

# Loop driver

The executable form of the README's standing instruction. One invocation = one pass, driven
end to end: **route → announce → (gate) → execute → record → report**. The README and the
runbooks own every rule; this skill only sequences them — when in doubt about a rule, the
rule file wins.

Why this exists: the router used to be a table a human executed from memory, and the
recorded failure modes of that (passes launched without cost warnings, records appended
inconsistently, gates drifted into "deviations") are what this skill closes. Each step below
exists because skipping it has already cost real money or trust at least once.

## 1 · Audit, then route — before anything else

```
node reviews/lib/records-auditor.mjs <target>     # must exit clean FIRST
node reviews/lib/route-next-pass.mjs <target>     # state → next pass → cost → gates
```

- **Auditor errors → repair the records before trusting any routing.** Broken records mean
  the router is deciding on wrong inputs. Repairs follow metrics-schema.md: past lines are
  never edited — a correction is its own appended line.
- Router exit **2** (owner gate) or **3** (judgment call): relay its output to the owner as
  a plain question and **stop this invocation**. Never pre-answer a gate. Certification
  always waits for an explicit go-ahead, even when it looks inevitable — a certification is
  the owner deciding to spend millions of tokens, not a step that happens to them.
  Stamp the wait: append `gate-open` (with the question as `reason`) to
  `reviews/<target>/worklog.jsonl` when relaying, and the invocation that consumes the
  answer appends `gate-closed` first thing — that span is the loop's measured
  blocked-on-owner time.
- If the router prints "no row matched", route by hand from
  [reviews/README.md](../../../reviews/README.md)'s router table using the facts it printed,
  and say in the report that this pass was hand-routed.
- The target's folder may not exist yet (first review). The router then says full discovery;
  the entry tier (README, "Entry tiers") decides full-loop vs ordinary treatment — that
  choice is the owner's if the change touches money, auth, data loss, concurrency,
  migrations, or new external input.

## 2 · Announce, then hold

Before anything runs, one line to the owner: current state → next pass → expected cost (the
router prints both; repeat them, don't re-derive). What "hold" means, precisely:

- **Router exit 0, cheap pass** (verification, fix-round handoff): announce, then proceed in
  this same invocation.
- **Router exit 0, discovery-scale** (full or delta discovery): the announce line comes
  first in the reply, then the launch — same invocation, per the README standing rule
  ("state the pass type and expected cost in one line" — a statement, not a question). If
  the owner has said they want launches held for an ack, that preference wins from then on.
- **Certification-grade launches and loop closures: never in the same breath.** Stop and
  wait for the explicit go-ahead, every time — these are the owner deciding to spend
  millions of tokens or to end a loop, and the recorded deviations cluster exactly here.

**Session-model guard:** discovery-scale fan-outs launched from a Fable session have died on
the session limit mid-run (recorded in runbook-discovery, Launch). If the current session is
Fable, say so in the announce line and let the owner choose: switch sessions, or proceed
resume-ready. On any mid-run death, resume — `Workflow({ scriptPath, resumeFromRunId })` —
completed agents return from cache; relaunching from scratch re-spends everything, and the
run journal (not guesswork) explains an empty result.

## 3 · Execute the pass

| Router says | Do |
|---|---|
| full discovery · delta discovery · certification | Follow `reviews/runbook-discovery.md` exactly — scoping, lens manifest, launch args, synthesis. The delta lens cap (5) and token budget (600k) are script-enforced; don't fight them. |
| verification | Follow `reviews/runbook-verification.md`. You must not be the fixer — sole exception: the test-only rule written in its step 1. Self-verification is the exact bias the loop exists to prevent. |
| fix round | Invoke the **/fix-review** skill and stop there — it is the sole owner of the fixer contract. Do not fix findings inline. |
| loop CLOSED | Report the closure line and stop. The target is under watch: a new serious finding in its files re-arms the loop and may be a `post-cert-escape` (reviews/track-record.md). |

## 4 · Records — the pass didn't happen until they exist

**Stamp the pass's runtime:** append `pass-launch` (`{pass, type}`) to
`reviews/<target>/worklog.jsonl` immediately before executing step 3, and
`pass-records-done` after the auditor exits clean below — the metrics line's
`runtime: {started, ended}` copies these two timestamps. Fix rounds stamp themselves
(the `/fix-review` skill owns its own worklog events).

In the runbooks' order: ledger update via **reconcile-findings** (discovery-type passes,
*before* the review file so it can reference D#s; verification updates statuses per its own
runbook) · metrics.jsonl line (schema v3 — discovery lines carry the per-finding `findings[]`
array and every pass line carries `runtime`) · index.md row · summary page via
**owner-summary** (decision passes only — a verification pass writes **no files**; its
outcome is the ledger flips, worklog, metrics and index row, reported at the owner gate in
chat, per `reviews/doc-contracts.md`). These are written at synthesis time because they are
unreconstructable later — that is the metrics schema's founding lesson. Then:

```
node reviews/lib/records-auditor.mjs <target>     # must exit clean before hand-back
```

**The doc gate — before anything reaches the owner.** After the records, run both halves:

```
node reviews/lib/doc-gate.mjs <target> <pass>     # structure lint — must exit clean
```

then spawn the **Sonnet judge** (Agent, `model: sonnet`): input = `reviews/doc-contracts.md`
plus the round's new/changed `reviews/` files; output = approve, or disapprove with per-file
reasons (language vs the vocabulary, evidence links supporting their claims, real reasons in
"Reasons to doubt"). Append a `doc-gate` worklog event with the verdict. On disapprove: fix
the files, re-run both halves. The summary is not handed to the owner and the review file is
not declared immutable until both halves pass. The gate judges, never edits — read its
reasons; don't game the lint.

## 5 · Close out

Re-run `route-next-pass.mjs`. Report to the owner: what ran, the headline result in one
plain sentence (the summary page carries the depth — don't restate it), the new state, and
what the router says comes next with its cost. Then **stop** — unless the owner said
**"until a gate"** this invocation, in which case loop back to step 1 and continue,
still stopping at every exit-2/3 gate and before every discovery-scale launch.

## 6 · Closing a loop — the owner said "close it"

Archiving is the last step of recording the close, in exactly this order (README, Files &
conventions):

1. `closed: <date> — <how>` into the ledger frontmatter; the index row records the story.
2. **Backlog rollup:** every ledger row still at `backlog` gets one line in
   `reviews/backlog.md` (D#, target, severity, what, area — template `templates/backlog.md`).
3. `archived: <date>` on the target's index row.
4. `git mv reviews/<target> reviews/archive/<target>` — contents unchanged, nothing rewritten.

Dormancy: when routing shows a target with no pass in 30+ days and nothing serious open,
*offer* archiving in the report — never move a folder the owner didn't ask about. A
`post-cert-escape` on an archived target moves its folder back out of `archive/` before the
re-armed pass runs.

## Never

- Launch a certification-grade pass without an explicit go-ahead given in this invocation.
- Close a loop yourself — `closed:` goes into the ledger frontmatter only on the owner's
  word, and the index row records how it closed.
- Mark anything `verified` while being the fixer, outside the written test-only exemption.
- Edit a `review-v*.md`, skip a record, or hand back with the auditor red.
- Create a `reviews/<target>/` folder except by executing a pass the owner requested for
  that target — findings noticed along the way go to `reviews/inbox.md`, and no owner
  decision is ever recorded that the owner did not state in so many words.
- Chain past a gate because the answer "seems obvious" — obvious-looking gates are where
  the recorded deviations clustered.
