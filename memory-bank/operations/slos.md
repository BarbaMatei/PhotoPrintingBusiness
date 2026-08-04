# Service Level Objectives (SLOs)

> **Status:** authored 2026-06-02 alongside bolt 045; queries corrected against the
> emitted names once bolt 044 landed. SLO 5's instrument exists but nothing increments
> it yet, so that panel reads "No Data" until intent 016 ships. SLOs 1–4 are measured.
> A test holds every query below against a real `/metrics` exposition
> (`DashboardMetricNamesTests`), so a rename that breaks a panel fails the build.

This document records the operational commitments FotoTipar makes to itself.
Each SLO is a measurable target the team is expected to keep over a defined
rolling window. Missing an SLO is a signal to **stop shipping non-critical work**
and fix the underlying cause.

The dashboard rendering these SLOs lives at
[`ops/dashboards/fototipar-overview.json`](../../ops/dashboards/fototipar-overview.json).

---

## 1. Availability — `≥ 99.5% rolling 30 days`

**What it measures:** the share of HTTP requests to `*.fototipar.ro` that
return a non-5xx response.

**Allowed downtime:** ≈ 3 hours 36 minutes per month.

**Source metric** — the ASP.NET Core instrumentation's request histogram; there is
no separate request counter, so its `_count` series is the request tally:
```
sum(rate(http_server_request_duration_seconds_count{http_response_status_code!~"5.."}[30d]))
  / sum(rate(http_server_request_duration_seconds_count[30d]))
```

**Why this target:** consumer e-commerce industry baseline; below this the
brand starts seeing review damage from abandoned checkouts.

**Action on breach:** create an incident, root-cause analysis posted to
ops channel within 24 hours.

---

## 2. Checkout latency — `p95 ≤ 1.5s on POST /api/payments/stripe/intent`

**What it measures:** the 95th percentile of the time the server spends
producing a Stripe PaymentIntent on the live checkout path.

**Why this target:** anything over 2 seconds noticeably reduces conversion.
1.5s leaves headroom for transient network noise.

**Source metric** — the route label is `http_route`, and it carries the ASP.NET route
template without a leading slash or the HTTP method:
```
histogram_quantile(0.95, sum by (le) (rate(
  http_server_request_duration_seconds_bucket{http_route="api/payments/stripe/intent",http_request_method="POST"}[5m])))
```

**Excluded:** time the user spends inside the Stripe Elements iframe — that's
on Stripe, not us. We measure only our server's handler.

**Action on breach:** trace a slow request, check Postgres lock contention,
check Stripe SDK call duration.

---

## 3. Payment-webhook success — `≥ 99.9% rolling 7 days`

**What it measures:** the share of `POST /api/webhooks/stripe` and
`POST /api/webhooks/euplatesc` requests that result in the order being
successfully marked Paid (or correctly rejected with a `200` for known
duplicate/idempotency cases).

**Why this target is so high:** a missed payment webhook means a customer
paid but their order is stuck in Pending. The cost of a single miss is
disproportionate — customer service work, refund handling, lost trust.

**Source metric:**
```
payment_webhook_total{result="ok"} / payment_webhook_total
```

**Action on breach:** any single failed webhook that didn't recover via the
provider's automatic retry should produce an alert. We do not wait for the
SLO to breach — webhook failures are immediate red flags.

---

## 4. AWB auto-creation success — `≥ 98% rolling 7 days`

**What it measures:** for orders that reached the Paid status, the share
where the Sameday AWB (shipping label) was successfully created within
24 hours.

**Why 98% (and not 99%):** Sameday's API occasionally returns transient
validation errors (e.g., locker temporarily full) that legitimately can't
be auto-resolved. 2% gives operations room for manual intervention without
flagging the bolt 037 retry loop as broken.

**Source metric:**
```
awb_creation_total{result="ok"} / awb_creation_total
```

**Action on breach:** check Sameday's status page, check the
`AwbCreationGiveUp` structured-log markers — the cluster of give-up reasons
usually points at one shared cause (e.g., expired credentials, locker
catalog drift).

---

## 5. ANAF e-Factura submission success — `≥ 99% rolling 30 days`

**What it measures:** the share of generated invoices that were accepted
by ANAF SPV on first or retried submission within 5 business days
(regulatory deadline).

**Why 99% (and not higher):** ANAF SPV has known periods of unavailability.
This SLO is set against the deadline window we have legal obligation to
meet, not the SDK's immediate response.

**Source metric** — the instrument exists but nothing increments it yet; the
increments ship with intent 016:
```
invoice_anaf_status_total{status="accepted"} / invoice_anaf_status_total
```

**Action on breach:** this is a **regulatory compliance issue**, not a
quality-of-service issue. Escalate immediately.

---

## SLO ownership

| SLO | Primary owner | Notification channel |
|---|---|---|
| Availability | Platform / oncall | Sentry + ops Slack |
| Checkout latency | Backend lead | Ops Slack (latency is metric-only — Sentry sees no event) |
| Payment-webhook success | Backend lead | **Sentry pages immediately** (do not wait for SLO) |
| AWB auto-creation | Backend lead | Ops Slack |
| ANAF submission | Backend lead + Finance | **Email + Slack** (regulatory) |

**What Sentry actually sees.** Only *exceptions* surfacing as a 5xx reach Sentry —
unhandled ones and mapped ones whose status is ≥ 500. A breach that produces no
exception (slow checkout, a webhook branch that returns a non-`ok` result, an AWB
outcome recorded as `retry_later`) reaches the metric pipeline and the Grafana
dashboard, never Sentry. Alerting for those SLOs must be built on the Prometheus
metrics, not on Sentry issues. Standalone `LogError` lines go to the Serilog file
sink only — see `docs/DEPLOYMENT.md` §13.1.

## Review cadence

- **Weekly**: glance at dashboard, note any SLO trending down.
- **Monthly**: formal review — was anything missed, are targets still right.
- **Quarterly**: revisit the targets themselves — are we over-investing in
  reliability nobody perceives, or under-investing where it hurts?

---

## What this document is NOT

- It is not an SLA (a customer-facing commitment with contractual remedies).
  It's an internal target.
- It does not include burn-rate alerts. Those are planned for a follow-up
  intent if/when needed.
- It does not include frontend metrics (page-load, JS-error rate). Those
  belong to a separate intent if/when we add a frontend observability layer.
