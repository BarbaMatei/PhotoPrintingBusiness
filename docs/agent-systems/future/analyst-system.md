# Future System — Analyst (architectural review) concept note

> **Status: PARTIAL — a proto-version already exists** as the `architect-analyst` agent
> (`../../../.github/agents/architect-analyst.agent.md`). This note is the plan to *evolve* it into a
> first-class system in the coordinate layer. Part of the [future-systems map](README.md).

---

## The role

A **proactive architectural reviewer**: scans the whole system, infers the business workflows, detects
gaps (security / scalability / observability / missing capabilities), and **proposes ranked
improvements**. Crucially, it *originates* candidate work from **first-principles analysis** — "what
should exist that doesn't, and what's structurally wrong" — as opposed to work that comes from a bug, a
drift, or a feature request. It's the system equivalent of a senior architect doing a standing review.

## Where it sits — an input to the Conductor

The Analyst lives in the **coordinate layer** as one of the [Conductor](conductor-system.md)'s input
sources:

```
Analyst (architectural gaps) ─┐
Inspector (bugs) ─────────────┤
Librarian (intent drift) ─────┼─→ Conductor (aggregate + prioritise) → human → AI-DLC inception
fix-request loop ─────────────┤
feature backlog ──────────────┘
```

It produces *candidate work*; the Conductor *ranks and de-dups* it against everything else; the human
ratifies. The Analyst never decides what gets built — same proposes-not-decides rule as the Conductor.

## Disjointness (vs the other systems)

- vs **Inspector (bug-hunter):** the Inspector finds *defects in code that exists*; the Analyst finds
  *capabilities/structure that should exist but don't*. Different verbs.
- vs **Reviewer:** the Reviewer judges *one diff* pre-merge; the Analyst judges *the whole system*
  periodically.
- vs **Observability:** Observability finds *runtime* gaps from the live product; the Analyst reasons
  *statically* about architecture.

## What exists today, and how to evolve it

The `architect-analyst` agent already runs the right protocol — scan-architecture → infer-workflows →
detect-gaps → propose-improvements — and emits a ranked proposals report (+ a Now/Next/Later roadmap).
To make it a *system* in this architecture rather than a one-off report generator:

1. **Ground it in the oracle (knowledge ledger).** Today it reads only code. A gap detected as "the
   *intent* says X but no capability implements X" is far stronger than one guessed from code-reading
   alone — and it stops the Analyst from "proposing" things that were deliberately descoped (the oracle
   knows what's `rejected`/`parked`).
2. **Emit structured candidate-work into the Conductor's input**, not a standalone markdown dump — so
   proposals flow into the prioritised queue instead of needing a human to transcribe them.
3. **De-dup against known work** (open bugs, the backlog, parked decisions) so it doesn't re-propose
   things already tracked or deliberately declined.
4. **Stay read-only and proposes-not-decides** (already true).

## Open questions (resolve when picked up)

- Cadence: on-demand, or a periodic standing review (e.g. each release / N bolts)?
- Scoring: align its `(impact × 3) + ((6 − complexity) × 2)` ranking with the Conductor's priority
  function so the two don't fight.
- Minor existing inconsistency to fix on evolution: the agent's description says "10 ranked
  improvements" while its body and output format say "20" — reconcile.

## Connections

Feeds the [Conductor](conductor-system.md). Reads the oracle
([knowledge-builder](../knowledge-builder-build-guide.md)). Disjoint from the
[Inspector](../bug-hunter-build-guide.md), the [Reviewer](code-review-system.md), and
[Observability](observability-system.md). Existing implementation:
`.github/agents/architect-analyst.agent.md`.
