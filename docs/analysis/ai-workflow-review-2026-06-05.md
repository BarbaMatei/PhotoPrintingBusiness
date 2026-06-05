# AI Workflow & Infrastructure Review — 2026-06-05

> What this document is: an honest look at how this project gets built today — the agents,
> the skills, the pipelines, the process — and a concrete proposal for what to add so the
> project can evolve faster and with less manual babysitting. Written after a full sweep of
> the repo: CI/CD, memory-bank, specsmd setup, the codebase, and the new parallel-wave
> workflow.

---

## 1. Where we stand today — and it's a strong position

Most projects that say they're "AI-driven" have a chatbot and a prayer. This one has an
actual system, built up over a month of disciplined work:

| Layer | What exists | Honest assessment |
|-------|-------------|-------------------|
| **Methodology** | specsmd AI-DLC: four agents (master, inception, construction, operations), ~20 skills, templates, integrity scripts | Mature and genuinely used, not shelf-ware |
| **Knowledge base** | memory-bank: 31 intents, 155 stories, 24 ADRs, 7 standards docs, SLOs and metrics docs | Rich — this is the project's institutional memory |
| **Copilot layer** | architect-analyst agent, 15 domain skills, 4 slash commands | Working; produced two reviews that became 23 bolts |
| **Claude layer** | bolt-parallel-planner + bolt-wave-orchestrator (built this week) | New, tested, ready for Wave 1 |
| **CI/CD** | CI on every PR (build + tests), secret scanning, Docker image to GHCR, one-command local dev stack | Solid skeleton |
| **The app itself** | 355+ backend files, 11 background jobs, ~100 backend test classes, 46 frontend specs | Healthy and well-tested for its age |

The bones are excellent. The weaknesses are all in the **connective tissue** — the places
where a human still has to remember something, repair something, or carry information from
one place to another by hand. That's exactly where things slip, and exactly what we can
automate next.

---

## 2. The five real weaknesses

These aren't theoretical. Each one has already cost something, or is about to.

### 2.1 New AI instances start blind — there is no CLAUDE.md

There's no instructions file at the repo root telling an AI session how this project works:
how to build it, how to run tests, which files are off-limits, where the standards live,
which agent to use for what. Every session rediscovers all of this from scratch.

This was always a small tax. The parallel-wave workflow turns it into a big one, because
now we launch *several* fresh instances per wave, and each pays the full price of
rediscovery — or worse, guesses wrong.

### 2.2 The story index keeps drifting from reality

`story-index.md` is maintained by hand, and history shows what happens: it once
undercounted by ~51 stories (repaired 2026-06-02), and drifted again for bolts 038, 039,
044 and 045 — all listed "not started" after they had shipped. The planner agent caught
that last one, which is good, but catching drift after the fact is firefighting.

The ironic part: a script that checks this (`status-integrity.cjs`) already exists inside
`.specsmd/`. It just only runs when someone remembers to run it.

### 2.3 Nothing actually guards the merge path

There is no branch protection, no required status checks, no PR template, no merge queue.
Today, a PR with failing tests can be merged with one click. Nothing bad has happened yet
because one careful person does all the merging — but "one careful person" is precisely
the bottleneck we're trying to relieve, and we can't safely hand more autonomy to agents
until the rails are mechanical instead of habitual.

### 2.4 "All tests green" means less than it sounds like it means

The integration tests run against an **in-memory database**, not real PostgreSQL. The
local dev database is SQLite. Production is Postgres. This gap has already produced one
real casualty: a migration (`AddOrderIdempotencyKey`) generated against SQLite that is
known-broken for Postgres — it's flagged in DEPLOYMENT.md waiting to bite.

There are also no end-to-end tests yet (coming in bolt 066). So an autonomous instance
can honestly report "everything passes" while having broken something prod-shaped. If we
want to trust instances to verify their own work — and we do — the verification itself
needs to get deeper.

### 2.5 The wave workflow still has two manual choke points

Planning and implementation are now automated. But after the instances finish, a human
still does all of this by hand: review every PR line by line, merge them in the right
order, update each remaining PR after every merge, run the suite between merges, update
the story index, clean up worktrees and branches. That's roughly ten mechanical steps per
wave, and from Wave 1 onward it's where most of the human time will actually go.

### A note on what's *not* on this list

Several gaps found during the sweep are **already scheduled as bolts** — dependency
updates via Renovate (054), e2e tests and bundle budgets (066), shared test
infrastructure (062), KNOWN_FAILURES and the audit checklist (057), background-job
liveness (056). The backlog already heals those. Everything proposed below is the
**meta-layer** — the things the backlog can't see because they're about how the backlog
itself gets executed.

---

## 3. What to build — a ladder, not a leap

Each rung makes the next one safe. The theme throughout: **automate the mechanics, keep
the judgment human.**

### Rung 0 — Foundations (hours of work, worth doing before Wave 1)

| # | What | Why it matters |
|---|------|----------------|
| 0.1 | **Write `CLAUDE.md`** at the repo root | Auto-loaded by every session *and every wave instance*: build/test commands, how to invoke specsmd, where the standards live, files never to touch (story-index!), the SQLite-vs-Postgres trap, and a map of which agent does what. The single highest leverage-per-hour item in this document. |
| 0.2 | **Branch protection + required checks + PR template** | Makes "no red branch can merge" a property of the repository instead of a property of your discipline. The precondition for every autonomy step that follows. |
| 0.3 | **Install the GitHub CLI** (`winget install GitHub.cli`, `gh auth login`) | Lets the orchestrator open PRs itself instead of handing you links; later enables automated PR review. |
| 0.4 | **Extend the pre-commit hook to guard the index** | The secrets hook already exists; add a rule that blocks `story-index.md` commits on feature branches. Turns the orchestrator's "please don't touch" rule into physics. |
| 0.5 | **Expand the Claude permissions allowlist** | Parallel instances currently stall waiting for permission prompts (only build/test/commit are pre-approved). Pre-approving the routine commands is what makes an unattended wave actually unattended. |

### Rung 1 — Complete the wave trilogy (the biggest workflow win available)

The planner thinks, the orchestrator acts — and then everything stops and waits for a
human to do mechanical work. Two new agents fix that:

**`pr-verifier` — the first reader of every PR.**
After a wave's PRs appear, this agent reads each one against the bolt's own success
criteria, its stories, the project standards, and the wave's conflict rules. It checks
the test evidence and posts a structured verdict: what's solid, what's questionable, what
to look at yourself. You stop being the first reader of 2,000 changed lines and become
the second reader of a verdict — judgment instead of grind.

**`wave-closer` — the integration hands.**
Once you approve the PRs, this agent does what §8 of the plan describes, mechanically:
merge in the planned order, update each remaining PR after every merge, run the full
suite between merges, make the single story-index update for the wave, clean up worktrees
and branches, and finish by dry-running the next wave so you can see what's coming.

**Plus one CI job: the index-keeper.**
Run the existing `status-integrity.cjs` on every PR and every push to main, and fail
loudly on drift. This ends the chronic-drift saga permanently. (Longer term, the deeper
fix is making the story index *generated* from the bolt files rather than hand-edited —
drift becomes structurally impossible rather than merely detected.)

> **With Rung 1 done, a full wave becomes:** you say "run the next wave" → PRs and
> verdicts appear → you approve → you say "close the wave". Two human touchpoints, both
> of them judgment, neither of them mechanics.

### Rung 2 — Make self-verification trustworthy

| # | What | Why it matters |
|---|------|----------------|
| 2.1 | **`migration-guard` skill + CI job** | Any branch that adds a database migration gets validated against *real* Postgres (the compose db), checked for snapshot consistency, and gated on the empty-migration drift check. We've already shipped one broken migration; bolts 047 and 068 both add real ones. |
| 2.2 | **`run-app` project skill** | One documented, reliable way for any instance to boot the full stack (compose + seeding + dev JWT key), hit the smoke endpoints, and look at the UI. Turns "verify your change in the running app" into a one-liner in kickoff prompts — crucial for the UI bolts (058, 067, 069). |
| 2.3 | **A real-Postgres test profile** | A test mode that points the integration test factories at compose Postgres instead of the in-memory provider. Pairs naturally with bolt 062's shared-factory work — worth folding in as a story there rather than a separate effort. |

### Rung 3 — The self-evolving flywheel (parked until the final roadmap phases)

> **Deliberately last.** Deployment is the *end* of the owner's roadmap (see §6), not the
> next step — these agents only become relevant when the dev-environment phase begins.
> They're documented here so the ladder is complete, not as a nudge to deploy sooner.

| # | What | Why it matters |
|---|------|----------------|
| 3.1 | **Schedule the architect loop** | The architect-analyst has already produced two reviews that became 23 bolts. Put it on a cadence (monthly or quarterly, matching bolt 057's audit checklist): review → you approve the proposals → inception agent generates intents and bolts → planner re-plans → orchestrator executes. At that point the project generates its own backlog, and you steer by approving direction. This one is independent of deployment and could start earlier. |
| 3.2 | **`release-agent`** | Wakes up specsmd's dormant operations phase when the dev-environment phase of the roadmap begins: deploy to the dev environment, smoke-test, watch /health and the SLOs, propose rollback on a breach. |
| 3.3 | **`incident-scout`** | Once an environment is live: reads Sentry and the metrics against `operations/slos.md`, and converts real failures into drafted intents and bolts. Closes the final loop — the running system itself feeds the backlog. |

---

## 4. The target operating picture

```text
        ┌────────────────────────────────────────────────────────────┐
        │  architect-analyst (scheduled)  →  inception agent          │
        │     "what should we build?"        intents/stories/bolts    │
        └──────────────────────────┬─────────────────────────────────┘
                          [you approve intents]
                                   ↓
   bolt-parallel-planner  →  bolt-wave-orchestrator  →  pr-verifier
        the plan              parallel instances        verdicts on PRs
                                   ↓
                          [you approve PRs]      ← your ONLY recurring gates
                                   ↓
                             wave-closer   →   (index-keeper CI keeps truth)
                       merges, syncs, cleans, re-plans
                                   ↓
                       release-agent → incident-scout → back to the top
```

You appear exactly twice per cycle, both times to exercise judgment: *is this the right
direction?* and *is this work good enough to ship?* Everything else is machinery.

---

## 5. Recommended build order

1. **This week, before Wave 1** — Rung 0: CLAUDE.md, branch protection + PR template,
   gh CLI, the pre-commit index guard, the permissions allowlist. Roughly half a day,
   mostly configuration.
2. **While Wave 1 runs** — build `wave-closer` and `pr-verifier`. Wave 1's four PRs will
   make the manual integration pain concrete, which is exactly the right moment to spec
   these two well.
3. **Before Wave 1's coupon PR merges** — `migration-guard`. Bolt 047 carries the first
   real migration; the guard should exist before it lands.
4. **Before Wave 2** — the `run-app` skill, so the boot-manifest UI work (058) can be
   verified visually by its instance.
5. **Any time** — the index-keeper CI job; it's independent of everything else.
6. **When the roadmap reaches its final phases (§6)** — Rung 3: the scheduled architect
   loop (this one can start earlier), release-agent, incident-scout.

---

## 6. The road ahead — the owner's sequence (deployment comes last, on purpose)

This is the deliberate order of the project's remaining life before anything goes live.
It exists in writing because agents — this author included — habitually treat "deploy" as
the default next milestone. Here, it is explicitly the **final** one.

**Phase 1 — Finish the backlog.** All remaining bolts, executed through the parallel wave
plan (waves 1–6).

**Phase 2 — Build the AI infrastructure.** Rungs 0–2 of this document: the foundations,
the wave trilogy, trustworthy self-verification.

**Phase 3 — Stabilize.** A full regression pass plus a *comprehensive* end-to-end testing
module covering the entire application — every major user journey, not just the three
smoke tests bolt 066 introduces. ➜ *Not yet in the backlog; needs a new intent.*

**Phase 4 — The environment triad.** Infrastructure able to run in three distinct states:
**local testing**, a **deployable dev environment** (the first thing that ever gets
deployed — a place to test and experiment freely), and **production**. The compose
dev/prod files are a starting point, but a formal dev tier with its own config, secrets,
and seed data does not exist yet. ➜ *Not yet in the backlog; needs a new intent.*

**Phase 5 — Multi-language & multi-country readiness.** The ambition is all EU markets,
not just Romania. This phase prepares the *architecture* — it does not implement
translations. The central open decision: one multi-locale site versus multiple per-country
sites on different URLs (deliberately undecided; deserves a proper architecture study and
an ADR before any code). The readiness work should map every seam the decision touches —
UI text extraction, backend message localization, per-country VAT and invoicing regimes
(ANAF e-Factura is Romania-specific; EU sales mean OSS VAT), currency, shipping carriers,
legal pages — and prepare the seams without building behind them.
➜ *Not yet in the backlog; needs a new intent and an architecture study.*

**Phase 6 — Only now: deployment.** Dev environment first, production after — at which
point Rung 3 (release-agent, incident-scout) finally earns its place.

---

*Produced with input from three repo sweeps (CI/infrastructure, memory-bank/process,
codebase/tests) on 2026-06-05. Companion documents:
`docs/planning/bolt-parallel-plan-2026-06-05.md` (the current wave plan) and
`docs/planning/agent-commands.md` (how to drive the planner and orchestrator).*
