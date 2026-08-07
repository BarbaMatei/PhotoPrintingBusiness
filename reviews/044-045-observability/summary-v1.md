---
type: owner-summary
target: 044-045-observability
pass: 1
pass-type: discovery
commit: 5cac465
date: 2026-07-31
decisions-needed: 6
---

# Summary v1 — observability (bolts 044 + 045)

First review of the tracing/metrics/error-tracking work. **39 findings, 9 of them serious
enough to block** ([review](review-v1.md) · [detail](findings-v1.md) · [ledger](ledger.md)).
Verdict `request-changes`. **Nothing here is on fire today** — both features sit behind flags
that are `false` in the shipped config, so every one of these bites on the flag flip, not now.

## Needs your decision

Grouped by decision, not by finding — 23 serious findings collapse into 6 choices. Counts and
`D#`s are exact; nothing is hidden.

1. **🔴 The `/metrics` page would be readable by anyone on the internet** — `D1`, `D10`, `D12`,
   `D23` (4 findings). The gate checks the caller's network address, but the site runs behind
   Caddy ([Caddyfile:13](../../Caddyfile) proxies every path), so every request looks like it
   comes from Caddy. Allow-listing Caddy — the only way to make scraping work — lets everyone
   in. *Suggested: block `/metrics` at Caddy or move it to a separate internal port, ~1–2h,
   plus a test that the gate rejects a proxied request. Not a code-only fix.*
2. **🔴 Three separate paths send customer emails and live guest session tokens to Sentry** —
   `D2`, `D3`, `D4`, `D22` (4 findings), each reproduced against the real Sentry SDK by a
   verifying agent. Performance traces skip the cleaner entirely; the cleaner never looks at
   the URL query (where admin order-search puts customer emails and account confirmation puts
   its token); and header matching is case-sensitive, so on HTTP/2 the guest token survives.
   *Suggested: fix as one cluster and invert the approach — strip everything, add back what
   triage needs, ~3–4h. Fixing them one at a time invites a fourth leak.*
3. **🔴 Both headline features do not work, and the tests can't see it** — `D5`, `D6`, `D9`,
   `D8` (4 findings). Per-route sampling can never match a route, so every route traces at
   100%; "errors are always sampled" is dead code. A verifying agent deleted the whole Sentry
   PII cleaner from [Program.cs:56-57](../../src/PhotoPrint.API/Program.cs#L56) and the Sentry
   tests stayed **32/32 green**; deleting a metric call site left all **1001** green.
   *Suggested: fix `D6` before or with `D5` — correcting the sampler makes `D6` lose more error
   traces, not fewer. ~1 day including the tests that would actually catch this.*
4. **🔴 A charged customer whose order never becomes Paid is invisible** — `D7`. Several
   payment-webhook branches record no metric and no log above Debug, so the payment SLO still
   reads 100%. *Suggested: fix now, ~1h; it is a terminal `else` with a warning log and a
   counter.*
5. **🟠 Nine more mediums with real operational bite** — `D11` `D13` `D14` `D15` `D16` `D17`
   `D18` `D20` `D21`. The worst three: enabling tracing without an OTLP endpoint silently
   prints **full SQL to stdout in production** (`D13`); five of eight Grafana panels query
   metric names the code never emits, so the dashboard is permanently blank (`D14`); and
   Sentry's own failures are silent, so an exhausted quota reads as "no errors" (`D18`).
   *Suggested: one fix round with the blockers, ~1 day.*
6. **🟠 The 1461-green test result is not reproducible** — `D19`. The new test factories set
   process-wide environment variables and never restore them, while xUnit runs test classes in
   parallel, so which hosts boot the real Sentry SDK changes run to run. *Suggested: fix early
   in the round — it undermines every other test result you'll be shown.*

## Reasons to doubt this pass

- **2 of 11 manifest lenses did not run** — `db-parity` and `frontend-ux`. Justified (no
  migration, no UI file in scope) but **owed, not waived**; a certification must fold them in.
- **No trend yet.** This is the first line in [metrics.jsonl](metrics.jsonl) for this target,
  so the "do findings decay across passes" signal that gates the stop rule has no data here.
- **7 serious findings skipped the adversarial check** (`D1` `D5` `D6` `D7` `D10` `D11` `D19`)
  — accepted because 3–6 lenses found them independently. I rechecked all seven by hand
  against the source and they held, but that is the synthesizer checking, not an independent
  agent. Convergence is a precision signal, not proof.
- **2 findings are `plausible`, not confirmed** (`D32`, `D33`) and **6 cleanups were never
  verified at all** (`D34`–`D39`) — cleanups never get skeptics by design.
- **The repo's own citation scanner under-reports.** It says 0 comment-citation leaks, but its
  pattern matches `ADR-0NN` and `BUG-1` style only — not `bolt 044` / `story 003` / `intent
  020`. There are **77** of those in `src/` comments, two in bolt-045 files. That is a gap in
  the `system` target's tooling, and it is why `D34`/`D35` disagree with the clean scan.
- **Blinding is best-effort** — enforced by prompt only; no tool verifies the lenses stayed out
  of `reviews/`.
- **A discovery pass cannot certify.** `request-changes` is the strongest statement available
  here; "the feature is clean" needs a later full pass after the fixes.

## Filed automatically

**16 findings** (9 🟡 + 7 ⚪) went to the [ledger](ledger.md) backlog and are **not** part of
the fix round. One deserves your eye anyway: **`D25`** — tracing attaches full SQL text and
exception messages to spans sent to the OTLP collector, with no cleaner on that path at all.
It is a *second* data-egress route that this branch's privacy work never looked at.

## State

Router says **fix round** next (open 🔴/🟠 with no resolution). This is a **full-loop** target,
so it ends at certification — a separate, explicitly-gated spend, and not before the blockers
are fixed and independently verified.
