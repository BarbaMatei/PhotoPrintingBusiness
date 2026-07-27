---
type: review-metrics-schema
status: active
created: 2026-07-04
owner: Matei Barba
---

# Review metrics — what every pass records, and why

Every review pass (discovery, delta-discovery, *and* verification) appends **one line** to the target's
`reviews/<target>/metrics.jsonl` at synthesis time — after findings are settled, before the
review file is finalized. Append-only: never edit a past line; a correction is a note in the
next line. Unknown values are `null`, never guessed.

This data is what eventually answers the open questions in
[self-driving-loop-design.md](self-driving-loop-design.md): does new-serious-per-pass actually
decay (the stop rule)? what does a pass cost (the entry policy)? which lenses earn their keep?
It cannot be reconstructed later — bolt 035's `cost` fields are `null` forever because nobody
recorded them at the time. (035's rows were backfilled on 2026-07-04 from its review files;
unstated values are `null`, never guessed.)

## Fields (one JSON object per line)

| Field | Type | Meaning |
|---|---|---|
| `target` | string | the reviewed unit, e.g. `"035-payment-idempotency"` |
| `pass` | int | review version number (matches `review-v<n>.md`) |
| `type` | `"discovery"` \| `"delta-discovery"` \| `"verification"` | see the two-loops distinction + *The middle tier* in [README.md](README.md); the saturation/decay curve uses full `"discovery"` passes only |
| `date` | ISO date | when the pass ran |
| `commit` | string | the commit reviewed |
| `lenses` | array \| null | lenses/finders actually run |
| `verdict` | string \| null | the review's verdict |
| `new_findings` | `{high, medium, low, cleanup}` | **new** problems this pass named (info items count as `cleanup`, note it) |
| `refinds_identity` | int | findings that are the *same problem* as an earlier finding (reconciler / hand judgment) |
| `reraises_of_decided` | int | findings re-raising an accepted wont-fix / deferral / dismissal |
| `refuted` | int | candidate findings recorded as false positives this pass |
| `disputed` | int \| null | findings whose two skeptics contradicted each other (a guard found *and* a failing trace built); `null` when not tracked. Historical only — trace-first verification (2026-07-27) can no longer produce it |
| `verified` | int | findings flipped to `verified` this pass |
| `reopened` | int | findings reopened this pass |
| `tests` | `{passed, failed}` \| null | suite result at the reviewed commit |
| `cost` | `{agents, tokens, agents_by_stage?}` | fan-out size and rough token spend; `null` when not tracked. `agents_by_stage` = `{lenses, dedup, skeptics_guard, skeptics_trace}` — the discovery script reports these counts in its `_canonical` summary line; copy them in. They're what shows whether the skeptic tiering actually saves what it claims |
| `notes` | string | anything a future analysis will wish it knew |

## Example

```json
{"target":"036-example","pass":1,"type":"discovery","date":"2026-07-10","commit":"abc1234","lenses":["correctness","security","quality"],"verdict":"request-changes","new_findings":{"high":1,"medium":2,"low":4,"cleanup":3},"refinds_identity":0,"reraises_of_decided":0,"refuted":2,"verified":0,"reopened":0,"tests":{"passed":480,"failed":0},"cost":{"agents":9,"tokens":null},"notes":"first audit"}
```

## Rules

- The **synthesis step appends the line** — the fixer never writes here.
- `refinds_identity` / `reraises_of_decided` use the ledger's judgment
  (until the reconciler exists: the synthesizing agent's judgment, per the labeling rules in
  [archive/035-payment-idempotency/overlap-ground-truth.md](archive/035-payment-idempotency/overlap-ground-truth.md)).
- No global roll-up file: compute cross-feature summaries on demand from the per-feature files;
  a hand-maintained roll-up would drift.
