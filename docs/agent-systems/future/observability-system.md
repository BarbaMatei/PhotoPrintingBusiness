# Future System — Observability / SRE concept note

> **Status: PROPOSED / ROADMAP-GATED (captured 2026-06-15).** Idea record, not a spec. **Strictly
> post-deployment** — the pre-deployment roadmap puts deploy last, and this system watches a *running*
> product, so it has nothing to do until there is one. Part of the [future-systems map](README.md).

---

## The gap

Every system you have is **build-time**: it reasons about code and specs at rest. Once the product
runs in production, **nobody watches it** — errors, latency, failed jobs, payment/e-invoicing
failures, anomalies. This is the biggest lever for "self-healing with minimal human intervention," and
it's the one role entirely absent today.

## The role

An **observability / SRE** system that:
- watches the running product (logs, metrics, traces, error rates, SLO burn),
- detects anomalies and incidents (not just thresholds — regressions vs. the normal baseline),
- and **turns a confirmed incident into a fix-request** that re-enters the existing loop — exactly the
  way the Inspector feeds it, but sourced from production reality instead of static analysis.

That closes the outer loop: production breaks → incident → fix-request (`correlation_id`) → AI-DLC
bug-bolt → fix → verify → re-distil — with the human approving, not paging themselves at 3am.

## Where it sits

The **Operate** layer (see the [future-systems map](README.md)) — below the doing-systems,
fed by the live product, feeding back into the loop. It needs its own store for incident records and a
connection to whatever runtime telemetry the deployment exposes.

## Relationship to what exists

- Feeds the **fix loop** ([contract §4](../integration-contract-v1.5.md)) the same mailbox the
  Inspector's confirmed bugs use — an incident is just a bug discovered at runtime.
- Distinct from the Inspector: the Inspector reasons about code *at rest*; Observability observes
  behaviour *in flight*. (Same disjointness discipline as the rest of the org.)
- Likely composes existing telemetry tooling rather than reinventing it (the plugin-as-worker pattern).

## Open questions (resolve post-deployment)

- What telemetry stack does the deployment expose (logs/metrics/traces)? That dictates the adapters.
- Incident → fix-request triage: which incidents auto-file vs. queue for the owner?
- SLO definitions (the repo already has `operations/slos.md`) — this system would consume them.
- Interaction with the eventual 3-env setup and EU/multi-region readiness.

## Why it's gated, not built

Building it before there's a running product would be speculative — the adapters depend entirely on
the deployment that doesn't exist yet. Captured here so that when deployment arrives, the
self-healing-loop design is already on paper.
