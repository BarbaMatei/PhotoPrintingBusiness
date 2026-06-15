# Operating Profiles — operator & deployment guide

> **This is an operator-facing guide, not a rulebook.** The **normative** definition of profiles —
> the invariant, the two policy axes, the validity test, the named profiles — lives in the
> **integration contract, §5.5** ([integration-contract-v1.5.md](integration-contract-v1.5.md)). If
> anything here and §5.5 disagree, **§5.5 wins.** This doc just helps you *choose, switch, and wire* a
> profile, and is the home for the actual deployment artifacts (hook script, CI template) once they're
> built.

---

## What a profile is (30-second recap)

The bug-hunter and knowledge-builder are a **portable product**. Their *core* — what they do, the
ledgers, the loop, the safety rules — never changes between deployments. Only the **operating
context** changes, and it's two independent, mix-and-match policies:

- **`TriggerPolicy`** — *when* a run fires and *how* concurrent runs are kept from colliding.
- **`CommitPolicy`** — *how* a run's findings land on the one shared history.

A **profile** is one `(TriggerPolicy, CommitPolicy)` pair. The orchestrator skills are
**profile-agnostic** — they run when invoked and commit however the active policy says — so switching
profiles never touches the skills, only the thin deployment adapters described below.

## The options at a glance

*(Summary — see §5.5 for the authoritative wording.)*

| `TriggerPolicy` | Fires on | Serializes via |
|---|---|---|
| **`local-hook`** | a `post-merge` git hook on `main`, on your machine | the `.run-lock` (single-flight) |
| `ci-pipeline` | an on-merge-to-`main` CI job | the CI runner's concurrency group |
| `manual` | an operator "refresh" command | the `.run-lock` |

| `CommitPolicy` | How findings land | Requires |
|---|---|---|
| **`direct-to-main`** | each system commits its own store straight to `main` (two small chore commits) | push rights to `main` |
| `pr-auto-merge` | each system commits to a throwaway branch → PR → auto-merge | a protected `main` |

## Named profiles

| Profile | = | For | Status |
|---|---|---|---|
| **`solo-local`** | `local-hook` + `direct-to-main` | one operator with push rights to `main` | **ACTIVE on this repo** |
| `team-ci` | `ci-pipeline` + `pr-auto-merge` | multiple operators / protected `main` | captured, **not built** (YAGNI) |

## How to choose your profile

- **Solo, and you can push to `main`?** → **`solo-local`**. (This project.)
- **A team, or `main` is protected (PRs required)?** → **`team-ci`** — so runs serialize centrally and
  findings land via PR rather than direct push.
- **Mixed / something else?** Compose your own pair. It's **valid** only if the trigger guarantees
  one-writer-at-a-time and the commit lands writes on the single history (the §5.5 validity test).

## How to switch profiles

You change the **deployment adapters**, never the skills:

1. Pick the `(TriggerPolicy, CommitPolicy)` pair.
2. Wire that trigger (install the git hook, *or* add the CI job, *or* document the manual command).
3. Point the commit step at that commit path (direct push vs. branch+PR+auto-merge).
4. Record the active profile (here: in the integration contract §5.5's active-profile line).

The librarian-before-inspector order, the bookmark catch-up, and async approvals are invariant — they
come for free with the core regardless of profile.

---

## Deployment artifacts

> **Stubs — filled in at build time** (the skills must exist first). Tracked here so there's one home
> for them.

### `solo-local` (active)

- **Trigger — `post-merge` hook on `main`:** _TODO (wire when the orchestrators are built)._ A
  `post-merge` hook that, when `HEAD` is `main`, launches the run in the background: **librarian
  first, then inspector**, each resuming from its bookmark. A hook that fires mid-pass is ignored
  (the running/next pass reaches current `main` anyway).
- **Manual fallback — "refresh" command:** _TODO._ Same sequence, invoked by hand.
- **Commit — `direct-to-main`:** each orchestrator's Close commits its own store
  (`git add -- knowledge/` / `git add -- bug-hunting/`) straight to `main`. The gitignored code index
  is never committed.

### `team-ci` (not built — placeholders)

- **Trigger — CI job on merge to `main`:** _not built._ On-merge job, serialized by a concurrency
  group; runs librarian then inspector.
- **Commit — `pr-auto-merge`:** _not built._ Commit each store to a throwaway branch → open a PR →
  auto-merge. The review that matters is still the **inbox**, not the PR diff (ledger files are
  sole-writer and cannot conflict).

---

*Maintenance: this doc references the contract by section (§5.5) so its meaning survives version
bumps; the file link is updated by the README version-bump checklist.*
