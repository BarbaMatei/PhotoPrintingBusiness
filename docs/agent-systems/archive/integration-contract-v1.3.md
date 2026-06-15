# Integration Contract — AI-DLC × Bug-Hunter × Knowledge-Builder (v1.3)

*The normative interface between the three systems. Both build guides reference this file; neither
restates it. **If a brief and this contract disagree, this contract wins.** Changes here are
versioned (v1, v1.1, v2, …) and require checking every consumer listed in §8.*

> **v1.3 (2026-06-12) — runtime co-residence + fix-loop fixes from cross-system review v3 (I1, I2,
> I3, I7, I8, I12, I13 + the code-index line of I6), one owner decision:** §1 — the integration
> worktree admits **at most one active run across both systems** (each Open checks the sibling's
> `.run-lock`); write audits are **scoped to the run's own store**; the code-index refresh is
> **atomic via pointer swap** with a `built_at_commit` stamp. §2 — envelope gains
> **`oracle_coverage`** so a mid-backfill oracle is never mistaken for "no contracts exist". §4 —
> the scan predicate includes **`fix-failed`** (re-fixes are re-checked); **owner decision (I3,
> option B): `closed-unverified` fixes produce NO oracle entry of any kind** — the blind spot is
> accepted and documented, not accidental. §5 — publish-commits are **path-scoped and serialized**
> under the §1 mutex; `pre-merge` runs are read-only advisory; rollback bumps a recovery note for
> consumers.

> **v1.2 (2026-06-12) — mechanisms and identity rules from cross-system review v2 (H2, H4, H15,
> H16, H19, H20 + shares of H1/H7/H12/H17), two owner decisions included:** §1 — the three stores
> are **single-history** (runs only in the integration worktree on **`main`**; ledger merge
> conflicts never resolved textually), the `code-index/` store joins the map, each orchestrator
> **audits its run's writes** at close; §2 — `tamper_warning?` joins the envelope; §4 —
> `correlation_id` allocation rule, terminal **`closed-unverified`**, the scan implementer is BH
> Prompt 31b; §5 — **every publish is followed by a git commit by the publishing orchestrator**
> (owner decision A), corrupt-load → restore-from-git, hunt cadence wired like distillation.

> **v1.1 (2026-06-11) — errata from the cross-system review (G3, G6, G9, G10, §5 default), no
> interface change:** §1 gains the `bug-hunting/` eval-runs layout line and an honest scope for the
> content hash (accident-detection + `tamper_warning`); §3 defines "stale" for the flow map (build
> stamp + the §5 threshold); §4 names the `fix-reported` writer and both mailbox **checking
> mechanisms**; §5 gets a default staleness threshold.

> Referenced by: `docs/agent-systems/knowledge-builder-build-guide-v3.3.md` (Appendix A) and
> `docs/agent-systems/bug-hunter-build-guide-v3.4.md` (mirror edits 2026-06-11; G hardening
> 2026-06-11; H mechanisms 2026-06-12; I co-residence/loop fixes 2026-06-12).

---

## §1 — Storage layout and the sole-writer map

| Store | Writer (sole) | Readers |
|---|---|---|
| `memory-bank/**` (intents, bolts, standards, operations, story-index, maintenance-log) | AI-DLC | knowledge builder, bug-hunter, humans |
| `bug-hunting/**` (bug ledger, reports, eval, `fix-requests/`) | bug-hunter | knowledge builder, AI-DLC, humans |
| `knowledge/**` (knowledge ledger, views, index, eval fixtures/runs) | knowledge builder | bug-hunter, AI-DLC, humans |
| application source + test projects | humans + AI-DLC bolts (and the bug-hunter's **approved** regression tests only) | everyone |
| `bug-hunting/code-index/` (symbol/reference index) | the shared `code-index` tool — derived, regenerable; either system's run may refresh it (the one sanctioned dual-writer; conflicts resolved by regeneration, never merge) (v1.2). **Refresh is atomic — published via pointer swap, never in-place; readers always see a complete index; stamped `built_at_commit`** (v1.3) | both systems |

`knowledge/` layout: `knowledge-ledger.json` · `ledger-views/` (sharded human views) · `index/`
(query index) · `eval-fixtures/` · `eval-runs/` (throwaway).

`bug-hunting/` layout note (v1.1): `bug-ledger.json` + `bug-ledger.md` · per-run reports ·
`fix-requests/` · `eval-runs/` (throwaway — eval-mode runs never write the real ledger).

**Single-history stores (v1.2):** `bug-hunting/**` and `knowledge/**` live on one git history. Hunts
and distillations run **only in the designated integration worktree on `main`** (owner decision);
all other worktrees/branches treat these stores as **read-only**. A git merge conflict in a ledger
JSON is **never resolved textually** (interleaved monotonic IDs, hashes, and version stamps cannot be
hand-merged) — keep the integration branch's copy and re-run; runs are idempotent via signatures and
coverage hashes. Optionally add a `.gitattributes` merge guard so a conflicting merge fails loudly.
**Cross-system mutex (v1.3):** the integration worktree admits **at most one active run across both
systems** — at Open each orchestrator checks for the *other* system's `.run-lock` (same stale-lock
rule) and refuses/queues while it is present and fresh; the two locks together act as one mutex.

Cross-store reads are always allowed; cross-store **writes never are** — and **each orchestrator
audits its own run's writes against this map at close**, failing the run loudly on any write outside
its allowed set (v1.2). **The audit diffs only the run's own store paths** (`-- bug-hunting/` /
`-- knowledge/` + the shared code index), so a sibling system's in-flight files can never trip it —
each system's own close-audit covers its own store (v1.3). Each ledger records a content
hash per publish; an out-of-band edit is detected on load and surfaced, never silently absorbed.
**Scope of that check (v1.1):** the hash lives beside the file, so this detects *accidents*, not
adversaries — git history is the actual audit trail. On a mismatch the ledger still serves queries,
with `tamper_warning` in the envelope (oracle consumers treat results as degraded), until the
operator reconciles against git history.

## §2 — The `ledger-query` interface

**Targets:** `file_path` (± `line`) | `symbol` | `flow_id` (see §3) | `intent_id` | `bolt_id`.

**Filters — two independent axes** (decision currency and implementation status are orthogonal):

| Axis | Values | Default |
|---|---|---|
| `decision` | `current` \| `all` | `all` (everything returned, tagged) |
| `status_filter` | `any` \| `done` \| `done-or-partial` | `any` |

Recommended slices: the **bug-hunter** uses the defaults (it wants superseded/retracted/planned
entries *returned tagged* so its Verifier can weigh them); **AI-DLC** writing or implementing specs
uses `decision: current, status_filter: any` (planned contracts are exactly its context); a
"what is enforceable right now" consumer uses `decision: current, status_filter: done-or-partial`.

**Per-hit envelope** (every field, every hit):

```
{ id, statement, contract_kind, scope,
  decision, superseded_by, retraction?, active,        // decision-currency axis (active ≡ decision==current)
  status, status_evidence,                             // implementation axis
  confidence, confidence_rationale, verification, auto_activated, ratification_depth,
  contested, unanchored, security_flag,
  source_ref, code_refs, intent_id, bolt_id }
```

**Envelope-level:** `{ as_of_commit, ledger_version, index_version, staleness_warning?,
tamper_warning?, oracle_coverage }` — `tamper_warning` (v1.2) is set when the ledger loaded with a
content-hash mismatch (§1); consumers treat such results as degraded (corroborating like
`verification: not-checked`, never confidence-raising) until the operator reconciles.
**`oracle_coverage` (v1.3): `{ intents_distilled, intents_total, backfill_complete }`**, populated
by `ledger-query` from the KB's `coverage` section and `runs.mode` — so a mid-backfill oracle
(`as_of_commit == HEAD` but only N of M intents distilled) is distinguishable from "genuinely no
governing contract"; the bug-hunter warns at run open while `backfill_complete` is false.

Rules:
- **Tagged, never hidden.** Superseded, retracted, parked, and not-`done` contracts are returned with
  their tags under the default filters; exclusion happens only when a filter asks for it.
- **`contested` contracts must not raise any consumer's confidence** until a human resolves them.
- **Scope inclusion.** `contracts_for(file|symbol)` returns anchored matches (`code_refs`) **plus**
  every contract whose `scope` applies (`global`, matching `layer`, or matching `path_glob`) — this
  is how standards and SLO contracts reach the lookup path despite having no bolt.
- **Trust is visible.** `verification` (`entailed | partially-entailed | not-checked`),
  `auto_activated`, and `ratification_depth` travel with every hit so a consumer can distinguish a
  human-ratified, entailment-verified contract from policy-auto-activated prose.

## §3 — Flow identity

Flows are owned by the **bug-hunter's `app-mapping`**, which publishes the flow list (IDs, risk
classes, ordered components) in its ledger's `application_map`. The knowledge builder does not define
flows. `contracts_for(flow_id)` resolves **flow → files** through the published map, then anchors and
scopes as in §2. **Freshness (v1.1):** `app-mapping` stamps the published map with the commit it was
built at (`built_at_commit`); "stale" means that stamp trails HEAD beyond the same threshold as §5.
If the map is absent or stale, flow queries return empty with
`staleness_warning: flow-map-unavailable` — never a silent guess.

## §4 — Loop-signal mailboxes

- **`correlation_id` origin:** assigned by the bug-hunter (`fix-request-emit`); recorded on the bug,
  the fix-request record, and — by AI-DLC convention — in the bug-bolt's `bolt.md` frontmatter.
  **Allocation rule (v1.2):** the value is the bug's ledger id (already atomic and never reused) or
  bug-id + a run-scoped suffix, allocated through `ledger-io`; **never reused** — a re-emission after
  a `Reopened` regression gets a fresh id so two fix cycles can't alias.
- **"Fix done" (AI-DLC):** the bug-bolt's `bolt.md` reaches `status: complete` in `memory-bank/bolts/`.
- **"Verified fixed" (bug-hunter):** the fix-request record in `bug-hunting/fix-requests/` carries
  `fix_status: open | fix-reported | verified-fixed | fix-failed | closed-unverified` (+
  `verified_at`, `proof_test_ref`) — `closed-unverified` (v1.2) is the terminal state for the
  no-proving-test fallback closure (so statically-confirmed fixes
  don't park at `fix-reported` forever); the knowledge builder does **not** re-distil on it (no
  proof). **Owner decision (v1.3, review I3, option B): a `closed-unverified` fix produces no
  oracle entry of any kind — not even a queue-only candidate.** The resulting blind spot
  (statically-confirmed bug classes, e.g. concurrency, never mint a negative-invariant contract) is
  **accepted and documented**, not accidental; revisit only by explicit owner decision.
  All transitions after `open` are written by the bug-hunter's
  `fix-verification`: it writes **`fix-reported` the moment it consumes the "fix done" signal**
  (fix exists, not yet verified — v1.1), then `verified-fixed` or `fix-failed` after running the
  proving test.
- **Checking mechanisms (v1.1)** — a mailbox nobody polls starves silently, so both directions are
  wired: the **bug-hunter's orchestrator**, at run open, scans `bug-hunting/fix-requests/` for
  records with `fix_status: open | fix-reported | fix-failed` (v1.3 — `fix-failed` included so a
  re-fix is actually re-checked; terminal = `verified-fixed | closed-unverified` only) and checks
  each correlated bug-bolt's `bolt.md`
  status — that is how "fix done" is noticed. **Implemented by BH Prompt 31b** (the Phase 5
  orchestrator extension — v1.2; previously this mechanism had no builder). A proving-test failure
  writes `fix-failed` only when the bolt's completion commit is reachable from the run's commit;
  otherwise the record stays `fix-reported` ("merge pending") (v1.3). **AI-DLC discovers new fix-requests** through the
  owner-driven inception flow: the owner (or an inception session) reads `fix-requests/` with
  `fix_status: open` as candidate bug-bolts — convention, stated here so the mailbox has a reader.
- **Re-distillation (knowledge builder):** requires BOTH signals for the same `correlation_id`;
  idempotent — one re-distillation per verified fix. Never on AI-DLC's word alone.

## §5 — Freshness, integrity, cadence

- Every knowledge-ledger publish: atomic swap, `ledger_version` bump, `as_of_commit` stamp, content
  hash recorded. The query **index carries its own `index_version`**; the envelope reports both, and
  a mismatch is served with `staleness_warning: index-behind` rather than refused.
- Consumers record the `as_of_commit` they used (the bug-hunter records it per run, and warns at run
  open if the oracle trails HEAD beyond a threshold). **Default threshold (v1.1):** stale = more than
  **20 commits or 14 days** behind HEAD, whichever trips first — configurable, but this is the value
  in effect when nobody configures anything. The same default governs §3's flow-map staleness.
- **Trigger wiring:** the incremental distillation run is a step in the bolt-completion workflow
  (alongside `.specsmd/aidlc/scripts/bolt-complete.cjs`) or a daily batch — pick one and wire it;
  a cadence policy with no mechanism is how the ledger goes permanently stale. **The same applies to
  bug-hunting runs (v1.2):** wire one trigger (bolt-completion step, weekly batch, or pre-merge) —
  the §4 mailbox is only checked at run open, so no trigger means the fix loop starves one level up.
  **A `pre-merge` run is read-only advisory (v1.3):** it fires on a feature-branch worktree, where
  §1 makes the stores read-only — so it writes no ledger, coverage, or mailbox state; findings go
  to the PR comment only.
- **Publish = commit (v1.2, owner decision):** every publish of `bug-hunting/**` or `knowledge/**`
  is followed by a **git commit of the published store by the publishing orchestrator's close step**
  — the commit IS the restore point. **Path-scoped and serialized (v1.3):** the commit stages only
  the publishing system's own store (`git add -- bug-hunting/` or `git add -- knowledge/`, never
  `git add -A`), and the commit window runs under the §1 cross-system mutex — so a restore to
  `ledger_version` N−1 always lands on one system's clean publish; the write audit remains the
  detection backstop, not the serialization. A rollback bumps a recovery note in the run summary so
  consumers can compare their recorded `as_of_commit` and flag findings grounded in the withdrawn
  version (v1.3). On a corrupt or hash-mismatched load, the ledger-io `load`
  refuses/serves-degraded and the recovery action is restore-from-git (`ledger_version` N−1); a
  distillation publish that `distillation-eval` flags as regressed is rolled back the same way.

## §6 — Twin-name discipline

The systems share one skill namespace. Every skill description names its system and disowns its
sibling:

| Knowledge builder | Bug-hunter | Disclaimer to embed |
|---|---|---|
| `knowledge-ledger-io` | `ledger-io` | "the KNOWLEDGE ledger — NOT for bugs; bugs use `ledger-io`" |
| `artifact-ingest` | `tool-ingest` | "AI-DLC artifacts — NOT tool/linter output; that's `tool-ingest`" |
| `knowledge-orchestrator` | `orchestrator` | "distillation runs — NOT bug-hunting runs" |
| `ledger-query` | `intent-lookup` | "serves the ledger — `intent-lookup` is the bug-hunter-side consumer" |
| `current-state-description` | `app-mapping` | "behavioral observations for drift/firewall — NOT the code map; the map is `app-mapping`" |
| `eval-fixtures` / `distillation-eval` | `eval-corpus` / `eval-metrics` | "grades DISTILLATION accuracy — NOT bug-detection recall" |

## §7 — Cross-system build interleave

Bug-hunter steps are named by bolt (its inception has run: bolts 085–094); knowledge-builder steps
are named by phase **until its own inception assigns bolt numbers** — update this section when it
does.

```
1. Bug-hunter bolts 085–088      (foundation; produces the SHARED TOOLS: git-revision-tracking, code-index)
2. KB Phases 1–2                 (may run parallel with bug-hunter bolts 089/090)
3. Bug-hunter bolt 091           (oracle tier / intent-lookup — needs KB ledger-query; recommended after KB Phase 2)
4. Bug-hunter bolts 092–093      (parallel with KB Phases 3 and 5)
5. KB Phase 4                    (after bug-hunter bolt 093 — needs the fix-request store + fix_status)
```

`code-index` and `git-revision-tracking` are **shared deterministic tools** owned by neither system's
judgment layer. Judgment agents (hunters, Verifier, Curator, distiller, reconciler, both
orchestrators) are never shared — the separation of powers depends on it.

## §8 — Consumers of this contract

| Consumer | What it implements from here |
|---|---|
| KB `ledger-query` (Prompt 6) | §2 interface, §3 flow resolution, §5 versions/staleness |
| KB `contract-anchoring` / `intent-extraction` | §2 scope semantics |
| KB `correlation-tracking` (Prompt 18) | §4 mailboxes |
| BH `intent-lookup` (Prompt 24) | §2 envelope (its five required fields are a subset) |
| BH `fix-verification` / `fix-request-emit` (Prompts 31/33) | §4 `fix_status` lifecycle |
| BH `orchestrator` (incl. Prompt 31b) | §5 staleness warning at run open; §4 fix-request mailbox scan at run open incl. `fix-failed` (v1.3); §1 cross-system mutex + run lock + store-scoped write audit; §5 path-scoped publish-commit (v1.3); §2 `oracle_coverage` warning (v1.3) |
| KB `knowledge-orchestrator` | §1 cross-system mutex + run lock + store-scoped write audit; §5 path-scoped publish-commit + rollback (v1.3) |
| KB `ledger-query` (envelope) | §2 `oracle_coverage` population from coverage + runs.mode (v1.3) |
| shared `code-index` | §1 atomic pointer-swap refresh + `built_at_commit` stamp (v1.3) |
| BH `app-mapping` (Prompt 12) | §3 flow identity + `built_at_commit` freshness stamp (v1.1) |
| AI-DLC bug-bolt convention | §4 `correlation_id` in `bolt.md` frontmatter |
