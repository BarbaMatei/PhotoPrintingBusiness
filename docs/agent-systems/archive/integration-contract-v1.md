# Integration Contract — AI-DLC × Bug-Hunter × Knowledge-Builder (v1)

*The normative interface between the three systems. Both build guides reference this file; neither
restates it. **If a brief and this contract disagree, this contract wins.** Changes here are
versioned (v1, v2, …) and require checking every consumer listed in §8.*

> Referenced by: `docs/agent-systems/knowledge-builder-build-guide-v3.md` (Appendix A) and
> `docs/agent-systems/bug-hunter-build-guide-v3.1.md` (mirror edits applied 2026-06-11).

---

## §1 — Storage layout and the sole-writer map

| Store | Writer (sole) | Readers |
|---|---|---|
| `memory-bank/**` (intents, bolts, standards, operations, story-index, maintenance-log) | AI-DLC | knowledge builder, bug-hunter, humans |
| `bug-hunting/**` (bug ledger, reports, eval, `fix-requests/`) | bug-hunter | knowledge builder, AI-DLC, humans |
| `knowledge/**` (knowledge ledger, views, index, eval fixtures/runs) | knowledge builder | bug-hunter, AI-DLC, humans |
| application source + test projects | humans + AI-DLC bolts (and the bug-hunter's **approved** regression tests only) | everyone |

`knowledge/` layout: `knowledge-ledger.json` · `ledger-views/` (sharded human views) · `index/`
(query index) · `eval-fixtures/` · `eval-runs/` (throwaway).

Cross-store reads are always allowed; cross-store **writes never are**. Each ledger records a content
hash per publish; an out-of-band edit is detected on load and surfaced, never silently absorbed.

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

**Envelope-level:** `{ as_of_commit, ledger_version, index_version, staleness_warning? }`.

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
scopes as in §2. If the map is absent or stale, flow queries return empty with
`staleness_warning: flow-map-unavailable` — never a silent guess.

## §4 — Loop-signal mailboxes

- **`correlation_id` origin:** assigned by the bug-hunter (`fix-request-emit`); recorded on the bug,
  the fix-request record, and — by AI-DLC convention — in the bug-bolt's `bolt.md` frontmatter.
- **"Fix done" (AI-DLC):** the bug-bolt's `bolt.md` reaches `status: complete` in `memory-bank/bolts/`.
- **"Verified fixed" (bug-hunter):** the fix-request record in `bug-hunting/fix-requests/` carries
  `fix_status: open | fix-reported | verified-fixed | fix-failed` (+ `verified_at`,
  `proof_test_ref`), written by the bug-hunter's `fix-verification`.
- **Re-distillation (knowledge builder):** requires BOTH signals for the same `correlation_id`;
  idempotent — one re-distillation per verified fix. Never on AI-DLC's word alone.

## §5 — Freshness, integrity, cadence

- Every knowledge-ledger publish: atomic swap, `ledger_version` bump, `as_of_commit` stamp, content
  hash recorded. The query **index carries its own `index_version`**; the envelope reports both, and
  a mismatch is served with `staleness_warning: index-behind` rather than refused.
- Consumers record the `as_of_commit` they used (the bug-hunter records it per run, and warns at run
  open if the oracle trails HEAD beyond a threshold).
- **Trigger wiring:** the incremental distillation run is a step in the bolt-completion workflow
  (alongside `.specsmd/aidlc/scripts/bolt-complete.cjs`) or a daily batch — pick one and wire it;
  a cadence policy with no mechanism is how the ledger goes permanently stale.

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
| BH `orchestrator` | §5 staleness warning at run open |
| AI-DLC bug-bolt convention | §4 `correlation_id` in `bolt.md` frontmatter |
