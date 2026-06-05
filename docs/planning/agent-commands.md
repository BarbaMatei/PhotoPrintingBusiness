# Parallel Bolt Workflow — Agent Commands

Two Claude Code agents drive the parallel bolt workflow. Type these phrases as a normal
prompt in any Claude Code session opened in this repo (agents register at session start —
if a freshly created/edited agent isn't recognized, start a new session).

| Agent | File | Role |
|-------|------|------|
| `bolt-parallel-planner` | `.claude/agents/bolt-parallel-planner.md` | **Thinks** — analyzes remaining bolts, groups them into branches, schedules waves, writes the plan |
| `bolt-wave-orchestrator` | `.claude/agents/bolt-wave-orchestrator.md` | **Acts** — executes one wave of the plan: worktrees, parallel instances, verification, PRs |

The human gate stays with you: **reviewing and merging PRs**. Neither agent ever merges,
pushes to main, or fast-forwards anything.

---

## Planner commands

| Say this | What happens |
|----------|--------------|
| `plan the remaining bolts` | Full analysis → new plan written to `docs/planning/bolt-parallel-plan-<date>.md` |
| `re-plan the remaining bolts` | Same, re-run after reality diverged (bolts shipped, new bolts added, a wave went sideways). Drift-aware: trusts git evidence over the story index |
| `plan the remaining bolts, max N parallel branches` | Override the per-wave width preference |
| `plan only bolts <NNN>–<NNN>` | Restrict the scope of the plan |

Run the planner whenever the current plan stops matching reality — it is cheap and
re-runnable. The orchestrator always works from the **most recent** plan file unless told
otherwise.

## Orchestrator commands

### The two you'll use most

| Say this | What happens |
|----------|--------------|
| `dry-run the next wave` | Rehearsal: runs every pre-flight check for real (base branch up to date? bolt defs committed? worktrees free? `gh` installed?), then shows exactly what a real run would execute — worktree commands, instances + prompts, PR titles. **Changes nothing.** |
| `run the next wave` | The real thing: pre-flight → create worktrees/branches → launch one clean-context specsmd instance per group **in parallel** → wait → verify each (tests green, forbidden files untouched, everything pushed) → open one PR per branch → report with merge order. Then stops. |

### Variations

| Say this | What happens |
|----------|--------------|
| `dry-run wave 3` / `run wave 2` | Target a specific wave instead of auto-detecting the next incomplete one (pre-flight still refuses if its dependencies aren't merged) |
| `run the next wave using docs/planning/<file>.md` | Use a specific plan file instead of the latest |
| `what's the state of the current wave?` | Inspection only — which branches/PRs exist, what's merged, what's pending |
| `complete the current wave` | For a partially finished wave (e.g. one instance failed earlier): reports exact state and finishes only what's missing instead of relaunching everything |
| `relaunch the <group> instance` | After a failure: spawns a fresh clean-context instance for just that group, fed the failure context from the report |

### What the orchestrator refuses to do (by design)

- Merge PRs, push to `main`, fast-forward anything
- Start a wave when pre-flight fails (stale base, bolt defs not committed, dirty worktrees) — it stops and prints the exact remediation commands instead
- Run more than one wave per invocation — the next wave needs your merged PRs as its base
- Open a PR for a branch whose instance failed or reported red tests

---

## The wave lifecycle (the loop you'll repeat)

```text
1. you:          "dry-run the next wave"        → green? proceed (optional after wave 1)
2. you:          "run the next wave"            → instances run in parallel, PRs appear
3. you (GitHub): review + merge PRs in the reported order
                 (after each merge: "Update branch" on the remaining PRs, let CI re-run)
4. you:          clean up worktrees (commands are in the orchestrator's report)
5. repeat from 1 — the orchestrator auto-detects the next wave
```

When anything unusual happens between waves (manual git surgery, abandoned branch, new
bolts planned), insert: `re-plan the remaining bolts`, then continue the loop with the new
plan.

## One-time prerequisites (before the first wave)

1. **Base branch current** — the plan's §0: commit the inception artifacts, open the
   `analysis/architect-review` → `main` PR, review + merge it. The orchestrator's
   pre-flight verifies this and refuses until it's true.
2. **GitHub CLI (recommended)** — `winget install GitHub.cli`, then `gh auth login`.
   Without it the orchestrator can't open PRs itself and will hand you compare-URLs +
   ready-made titles/bodies to paste instead.

## Reading a pre-flight refusal

A refusal is the system working, not breaking. The report names the failed gate and the
fix. The common ones:

| Gate failure | Meaning | Fix |
|--------------|---------|-----|
| Bolt definitions not on base | `memory-bank/bolts/<id>/` isn't committed on `origin/main` | Run the plan's §0 (commit + PR to main) |
| Previous wave not merged | A prior wave's PR is still open | Merge it (in the reported order), then retry |
| Worktree path exists | Leftover from an earlier wave | `git worktree remove "<path>"` then `git worktree prune` |
| `gh` unavailable | GitHub CLI missing/unauthenticated | Install + `gh auth login`, or accept compare-URL fallback |
