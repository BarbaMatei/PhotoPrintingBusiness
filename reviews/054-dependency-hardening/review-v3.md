---
type: review
target: 054-dependency-hardening
version: 3
supersedes: null
commit: a33b39d
branch: feat/bolt-054-dependency-hardening
pass-type: delta-discovery
date: 2026-09-05
lenses: [security, correctness, observability, completeness-critic]
lenses-not-run: [race, db-parity, frontend-ux]
verdict: approve-with-followups
blockers: []
findings: { high: 0, medium: 2, low: 9, cleanup: 0, refuted: 1 }
tests: { dotnet: "39/39", frontend: "not run — no frontend change" }
---

# Review v3 — 054-dependency-hardening

Reviewed range `e1febe5..a33b39d` — the cumulative fix diff since the v1 pass, 23 changed
files, code tip `8ae0953`. Four lenses of the delta cap of five; skeptics ran on the six
serious candidates only. No fixed row was reopened: the two 🟠 below are gaps the round left,
not fixes that failed.

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-747 | 🟠 | No test proves the rate limiter partitions per forwarded client, so a one-line reorder silently restores one bucket for the whole internet | `src/PhotoPrint.API/Program.cs:375` | yes |
| PPW-748 | 🟠 | The metrics-scrape exclusion's excluded branch is untested in the changed test file; only an out-of-scope file guards it | `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:122` | yes |
| PPW-712 | 🟠 | Named auth rate-limit policies have no per-IP partition (re-raise of a deferred row; deferral stands) | `src/PhotoPrint.API/Extensions/SecurityExtensions.cs:72` | owner decides |
| PPW-749 | 🟡 | The 512-peer log budget never resets, so cheap in-network noise can permanently silence the proxy-drift warning | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:112` | no |
| PPW-750 | 🟡 | DEPLOYMENT.md §16.3 names three changed behaviours and omits the HSTS header the trusted-proxy switch now makes reachable | `docs/DEPLOYMENT.md:1702` | no |
| PPW-751 | 🟡 | Serilog WriteTo merges by array index, so the Development overlay collides with the base Console sink's formatter at WriteTo:0 | `src/PhotoPrint.API/appsettings.Development.json:51` | no |
| PPW-752 | 🟡 | TrustedProxyList re-parses the trusted-proxy list and discards parse errors, so the validator's caps do not guard the type that decides trust | `src/PhotoPrint.API/Configuration/TrustedProxyList.cs:12` | no |
| PPW-753 | 🟡 | Log assertions capture around Serilog, so no test executes the production logging configuration this round rewrote | `src/PhotoPrint.Tests/Integration/ForwardedHeadersIntegrationTests.cs:244` | no |
| PPW-754 | 🟡 | The new production rolling file sink writes into the container's ephemeral layer — no volume backs /app/logs | `src/PhotoPrint.API/appsettings.Production.json:13` | no |
| PPW-755 | 🟡 | The new production File sink can be dropped or fail to open with no diagnostic, because SelfLog is enabled nowhere and the package is transitive | `src/PhotoPrint.API/appsettings.Production.json:11` | no |
| PPW-756 | 🟡 | A null RemoteIpAddress returns before judging, on the one transport where ASP.NET honours X-Forwarded-For with no peer check | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:36` | no |
| PPW-757 | 🟡 | The NuGet audit gate is asserted as a command string and never executed, and the shipping image restores without it | `src/PhotoPrint.Tests/Unit/Configuration/DeploymentDefaultsTests.cs:121` | no |
| PPW-731 | 🟡 | Once-per-process dedupe with no counter makes ongoing proxy drift look like a one-off (re-find, still open) | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:49` | no |
| PPW-732 | 🟡 | Singleton lifetime — the basis of "warned once" — is unverified in the real pipeline (re-find, still open) | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:26` | no |
| PPW-739 | ⚪ | Check-then-act on the 512-entry log cap lets _loggedPeers exceed the cap (re-find, one sub-claim added) | `src/PhotoPrint.API/Middleware/UntrustedForwardedPeerMiddleware.cs:35` | no |
| PPW-741 | ⚪ | The metrics path is re-derived in three places with divergent empty-value handling (re-find, still open) | `src/PhotoPrint.API/Extensions/ForwardedHeadersExtensions.cs:108` | no |

Eight of the eleven new rows are fix-caused and carry `residual-of` lineage on the ledger:
PPW-748 (of PPW-722), PPW-750 (of PPW-713), PPW-751, PPW-753, PPW-754, PPW-755 (of PPW-714),
PPW-752 (of PPW-715), PPW-757 (of PPW-718). All are seed round 1.

## Refuted

One candidate did not survive and got no ledger id: a completeness claim that the delta diff
artifact contained only `.cs` files, leaving 12 of the 23 changed files unreviewed. The sibling
config-and-docs artifact holds exactly those 12 files with 24 hunks; the two artifacts are
disjoint and union to 23 files, so no file reached zero coverage. The same finding also
miscounted the backend hunks (29, not 11).

The re-raise of PPW-712 lost its escalation argument in synthesis rather than in a skeptic. The
lenses argued high severity because the round ships proxy trust on and raised
`RateLimit__Public__PermitLimit` to 600, so a single attacker at 10 logins a minute would lock
every user out. `UseRateLimiter()` (`src/PhotoPrint.API/Extensions/SecurityExtensions.cs:122`,
installed at `src/PhotoPrint.API/Program.cs:380`) still runs before `UseRouting()` (`:393`), so
the named auth policies remain inert — the exact reason v1 recorded for the deferral. Severity
stays 🟠, the deferral stands, and it still may not land without PPW-711.

Two severities were cut below their lens maximum on evidence the traces themselves produced:
PPW-749 (filling the 512-slot budget needs 512 distinct in-network source addresses, because
the budget keys on the TCP peer and not on header contents) and PPW-750 (`Caddyfile:36`
overwrites `Strict-Transport-Security` before any browser sees the API's own value). PPW-751 is
`plausible`, not confirmed: the index collision is real, but the pinned Serilog 10.0.0 picks the
`outputTemplate` overload, so the claimed compact-JSON dev console does not occur.

## Notes for the fixer

- **Order.** PPW-747 and PPW-748 first — both are tests, one each, and they pin the two claims
  this bolt's own runbook makes. Then the record row PPW-750, then the logging rows.
- **PPW-747 must not touch `SecurityExtensions.cs` ordering.** That change is PPW-711, still
  deferred; this row buys a test that reddens when the Program.cs order is reversed.
- **PPW-748 goes in `ForwardedHeadersWithObservabilityTests`**, the second class in
  `ForwardedHeadersIntegrationTests.cs` — the class the round's recorded test filters omitted,
  which is why the verification's first hand proofs read green against a real defect.
- **The four logging rows are one sitting.** PPW-751, PPW-753, PPW-754 and PPW-755 all live in
  the sink configuration the round rewrote; PPW-754 needs a compose volume and PPW-755 a direct
  package reference, so the fix is not test-only.
- **PPW-749, PPW-731 and PPW-739 are three mechanisms in one budget** — never resets, warns
  once per peer, and overshoots the cap under concurrency. A replacement that keeps an uncapped
  counter settles all three; fixing them separately will churn the same twenty lines.
- **Ten of these rows are `unverified-low`** — read by a lens, never handed to a skeptic
  (delta budget). Confirm the mechanism before writing an assertion against it. The sub-claims
  checked by hand during synthesis are marked as verified on their ledger rows: PPW-755's two
  package facts, PPW-757's Dockerfile and deploy-trigger claims.
- **Coverage debt is unchanged.** `race`, `db-parity` and `frontend-ux` have still never run on
  this target; `race` is the one that matters, since PPW-749/PPW-739 concern concurrent state
  in a singleton. `Dockerfile` and `.github/workflows/deploy.yml` reached no lens in either
  pass — PPW-757 names that gap.
