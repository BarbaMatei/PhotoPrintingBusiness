---
type: owner-summary
target: 054-dependency-hardening
pass: 1
pass-type: discovery
commit: e1febe5
date: 2026-09-04
decisions-needed: 6
---

# Owner summary — 054-dependency-hardening v1

Eight blinded lenses read this branch's whole diff against `origin/main` plus the bolt's own
notes; 23 skeptics then tried to disprove what they claimed. 36 problems came back and none
collapsed on checking: 2 🔴, 11 🟠, 15 🟡, 8 ⚪, verdict `request-changes`
([review-v1.md](review-v1.md)). The bolt's main safety feature ships switched off, and the log
line meant to prove it works never leaves the container.

## Needs your decision

1. **The trusted-proxy setting ships commented out, so the feature is inert** — 🔴
   [PPW-713](ledger.md). `.env.example:62` comments the value out, while the bolt's own record
   says it "ships switched on"
   (`memory-bank/bolts/054-dependency-and-boot-hardening/implementation-walkthrough.md:93`).
   Left empty, every visitor reads as the Caddy container: one shared rate-limit bucket for the
   whole site, and the refresh cookie loses `Secure`
   (`src/PhotoPrint.API/Services/AuthService.cs:354`). Fix now — one line plus a test that
   boots with the shipped file, ~30 min.
2. **No log this code writes reaches `docker compose logs`** — 🔴 [PPW-714](ledger.md).
   Production Serilog writes to a file only (`src/PhotoPrint.API/appsettings.json:183`) and
   `/app/logs` has no volume, so the verification greps in `docs/DEPLOYMENT.md` §16.6 read clean
   whatever is happening. Fix now — add the console sink, ~30 min.
3. **The rate limiter this bolt left alone on purpose** — 🟠 [PPW-711](ledger.md) and
   [PPW-712](ledger.md). `UseRateLimiter()` runs before `UseRouting()`
   (`src/PhotoPrint.API/Extensions/SecurityExtensions.cs:122`), so the named
   login/registration/password-reset limits never run; they also have no per-IP split (`:72`),
   so turning them on would give the whole site one hourly budget each. The deferral to intent
   029 / bolt 063 is disclosed in `docs/DEPLOYMENT.md` §16.7 item 3. Uphold it and record both
   as deferred, or fix them together (~2h) — never PPW-711 alone.
4. **Nothing checks dependencies for known vulnerabilities in CI** — 🟠 [PPW-718](ledger.md),
   against the intent's own "verified in CI" requirement
   (`memory-bank/intents/025-security-dependency-hygiene/requirements.md:74`);
   `.github/workflows/ci.yml:52` runs no audit. Fix now, ~1h.
5. **The boot guard accepts `0.0.0.0/0` as a trusted proxy** — 🟠 [PPW-715](ledger.md) — and the
   intent's open question still tells operators to trust the whole container subnet, 🟠
   [PPW-719](ledger.md). Fix both now, ~1h.
6. **Six 🟠 rows where the tests and the wiring make green mean less than it looks** —
   [PPW-716](ledger.md), [PPW-717](ledger.md), [PPW-720](ledger.md), [PPW-721](ledger.md),
   [PPW-722](ledger.md), [PPW-723](ledger.md). One fix round, ~3h.

## Reasons to doubt

- Three manifest lenses never ran: `db-parity`, `race`, `frontend-ux`. Only `race` matters here
  — the new middleware keeps process-wide state
  (`src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:35`) and only the
  correctness lens looked at it ([PPW-739](ledger.md)). The diff touches no query and no UI file.
- Eight of the ⚪ rows — PPW-739 to PPW-746 — are reported as read, not proven by a trace.
  [PPW-737](ledger.md) is `plausible` only — the
  OpenTelemetry 1.15 attribute name was never checked against the shipped package
  ([metrics.jsonl](metrics.jsonl)).
- First pass, so there is no trend to compare against: 2+11 serious rows on an empty ledger, no
  agent cut for budget, no finding hinted ([metrics.jsonl](metrics.jsonl)).
- Blinding is best-effort, and the bolt's own notes were in scope, so lens wording can echo the
  bolt's framing even where the code disagrees with it. Two sub-claims inside real findings were
  wrong and were corrected rather than carried forward ([review-v1.md](review-v1.md),
  "Refuted").

## Filed automatically

23 minor rows (15 🟡, 8 ⚪) went to the backlog on [ledger.md](ledger.md). One deserves your eye
anyway: [PPW-727](ledger.md), where a claim about the authentication audit log that this bolt
itself disproved still stands in the boot warning text and in ADR-018.

## State

All 36 rows are `open` on [ledger.md](ledger.md); nothing is fixed or verified yet. The router
proposes a fix round on the 13 serious rows next, at fix-round cost. Decisions 3 and 5 change
what that round does, so it starts from your answers.
