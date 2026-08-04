---
type: review-inbox
updated: 2026-08-03
---

# Inbox — findings without a target

Findings noticed **outside any open review pass** — by a fixer sweeping a defect class, a
driver reading code, anyone — land here with their evidence, untriaged. A new
`reviews/<target>/` folder is never created for them: only an owner-opened loop creates a
target folder, and when that happens the relevant rows below seed its ledger and are
struck from this file. Not to be confused with a ledger row's `backlog` status — that is a
triaged minor deferred *within* its target; this file is what nobody has triaged yet.

Severities are the recorder's first read, not a pass verdict.

## auth / rate limiting — no loop opened

Found incidentally during the 044-045-observability v1 fix round
([resolution](044-045-observability/resolution-v1.md)): that review's D1 was "the
`/metrics` allow-list trusts the TCP peer address, which behind the Caddy edge is the
proxy for every caller", and the fixer noticed the same defect class in the rate limiters
and the security-audit log. The loop driver verified the first two rows by reading the
code; the third is unverified. The owner declined to open a target for this (2026-08-03).

| Sev | Recorded | Title | File |
|---|---|---|---|
| 🔴 | 2026-08-03 | Global rate limiter partitions on `Connection.RemoteIpAddress`, which behind Caddy is one value for all traffic — the documented "100/min per IP" is 100/min for the whole internet, so one client at ~2 rps can 429 the entire site | `Extensions/SecurityExtensions.cs:60` |
| 🔴 | 2026-08-03 | Auth limiters are unpartitioned `AddFixedWindowLimiter` calls, not per-IP as their comments claim: registration 5/hour, resend-confirmation 3/hour and forgot-password 3/hour are **site-wide** budgets, so one actor can lock every user out of signup and password reset | `Extensions/AuthExtensions.cs:84-106` |
| 🟠 | 2026-08-03 | Security-audit log entries record Caddy's address as the client IP, so the audit trail cannot attribute an action to a caller | `Controllers/AuthController.cs:54, 72, 160` |

### Evidence held

- **Row 1** — read directly: `partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown"`,
  with the inline comment claiming "100/min per IP". No `UseForwardedHeaders` exists anywhere in
  `src/` (grepped), and `Caddyfile` reverse-proxies every path to `api:8080`, which is exposed
  internally only.
- **Row 2** — read directly: the three policies are plain `options.AddFixedWindowLimiter(...)` with
  no partition function, so the permit budget is process-global.
- **Row 3** — reported by the fixer, **not independently checked by the loop driver**. Treat as
  unverified until a pass looks at it.

### Note on urgency

The application is not deployed, so nothing is exploitable today. Rows 1 and 2 are
**pre-deployment blockers**: both are trivially triggerable denial-of-service against all
users, and neither has a test.

---

## From the 044-045-observability v1 fix round (2026-08-04)

Found by the cluster F fix-diff micro-review while fixing F15 (mapped 5xx bypassing Sentry).
Same defect class one layer out, but outside the review's finding set, so nothing was changed.

| Sev | Recorded | Title | File |
|---|---|---|---|
| 🟠 | 2026-08-04 | Background jobs are wholly outside Sentry's reach: all 13 `BackgroundJobs/` files catch their own exceptions and only log, never pass through `ExceptionHandlerMiddleware`, and none touch `IHub` — so a total AWB-retry or email-retry outage produces no Sentry issue at all | `BackgroundJobs/AwbRetryJob.cs:56,65`, `EmailRetryJob.cs:85`, `ArchiveRetentionJob.cs:63`, `OriginalPurgeRecoveryScanner.cs:81`, `PromotionRecoveryScanner.cs:85` |

### Evidence held

- Grepped `src/PhotoPrint.API/BackgroundJobs/` for `Sentry.|IHub|CaptureException|SentrySdk`:
  zero matches across 13 files. Each job's tick is wrapped in its own `catch` that calls
  `LogError` and swallows, so nothing propagates to the middleware that does capture.
- **Not strictly a doc contradiction:** `docs/DEPLOYMENT.md:821` scopes its Sentry claim to
  exceptions escaping "any controller or middleware", which these never do. The only net under
  them is the monthly manual log-vs-Sentry cross-check in §13.8.
- `Hubs/AdminOrderHub.cs` has no methods yet, so SignalR hub exceptions are not a live gap.

### Note on urgency

Not exploitable — this is a blind spot, not a vulnerability. It matters at the moment Sentry is
switched on and someone reads "no new Sentry issues" as "the retry jobs are healthy".
