# Metrics Reference

Authoritative reference for every metric the PhotoPrint API emits.
Names + label values defined in
[`MetricNames`](../../src/PhotoPrint.API/Observability/MetricNames.cs);
instruments defined in
[`FotoMetrics`](../../src/PhotoPrint.API/Observability/FotoMetrics.cs).
The Grafana dashboard in
[`ops/dashboards/fototipar-overview.json`](../../ops/dashboards/fototipar-overview.json)
queries these names verbatim — renames need both files.

The Prometheus exporter binds at `GET /metrics` (configurable via
`Observability:Metrics:PrometheusEndpoint`), gated per ADR-018 by the scrape
listener (`Observability:Metrics:ScrapePort`, other listeners get 404) and an IP
allow-list (`Observability:Metrics:AllowedScrapeIps`, addresses or CIDR, 403
otherwise). With `Observability:Enabled=false` the endpoint is absent. Operator
setup — including why the reverse proxy's address must never be allow-listed —
is [`DEPLOYMENT.md` §14](../../docs/DEPLOYMENT.md#14-tracing-and-metrics-intent-020--bolt-044).

## Business metrics

| Name | Type | Unit | Labels (cardinality) | Where incremented |
|---|---|---|---|---|
| `orders_created_total` | counter | `1` | `processor` (2) × `status` (3) = 6 | [`OrderService.CreateFromCartAsync`](../../src/PhotoPrint.API/Services/OrderService.cs) after order persist |
| `payment_webhook_total` | counter | `1` | `processor` (2) × `result` (6) = 12 | [`WebhooksController`](../../src/PhotoPrint.API/Controllers/WebhooksController.cs) — every receipt that reaches a terminal decision records exactly one; unhandled Stripe event types and requests that throw before the decision record none |
| `upload_size_bytes` | histogram | `By` | none | [`UploadService.UploadAsync`](../../src/PhotoPrint.API/Services/UploadService.cs) after upload persist |
| `order_processing_duration_seconds` | histogram | `s` | none | [`AdminOrderService.UpdateStatusAsync`](../../src/PhotoPrint.API/Services/AdminOrderService.cs) on Paid→Shipped transition (`ShippedAt - PaidAt`) |
| `awb_creation_total` | counter | `1` | `result` (5) | [`AwbCreator.CreateForOrderAsync`](../../src/PhotoPrint.API/Services/Sameday/AwbCreator.cs) — one increment per invocation, label mapped from the discriminated outcome (or `error` when the call throws) |
| `invoice_anaf_status_total` | counter | `1` | `status` (4) | Meter defined here; increment sites ship with bolt 039 (intent 016). |

### Label value enumerations

All label values are constants in [`MetricNames`](../../src/PhotoPrint.API/Observability/MetricNames.cs) — no free-form strings reach a metric. Cardinality is therefore static and small.

#### `processor`
| Value | When |
|---|---|
| `stripe` | Stripe-routed payment |
| `euplatesc` | EuPlatesc-routed payment |

#### `status` (order lifecycle, `orders_created_total`)
| Value | When |
|---|---|
| `created` | Order moves from cart to persisted order (initial state = `AwaitingPayment`) |
| `paid` | Reserved for future use (today's Paid transition is captured via `payment_webhook_total{result=ok}`) |
| `cancelled` | Reserved for future use (today's Cancelled transitions are not yet metered) |

#### `result` (`payment_webhook_total`)
| Value | When |
|---|---|
| `ok` | Webhook verified, order transitioned to `Paid`, side effects fired |
| `signature_invalid` | HMAC / Stripe-Signature verification failed |
| `order_not_found` | Webhook referred to an order that doesn't exist |
| `amount_mismatch` | Vendor-reported amount differs from `Order.TotalRon` |
| `duplicate` | Idempotent receipt — the order has already been paid, whether it is still `Paid` or has moved on to `Printing`, `Shipped` or `Delivered` |
| `failed` | Vendor reported the payment failed, unparseable payload, or a receipt the order's state could not accept — a paid notification for an order that never can be `Paid` (`Cancelled`, `PaymentFailed`) is logged at `Error`, because the customer is charged and needs manual reconciliation. Also covers a paid notification whose invoice number could not be allocated before the retry budget ran out: the order stays `AwaitingPayment` and needs the same reconciliation. A fulfilled order is a `duplicate`, not this. |

#### `result` (`awb_creation_total`)
| Value | When |
|---|---|
| `ok` | Sameday created the AWB, persisted `AwbNumber` + `AwbLabelUrl` |
| `skipped` | No label was needed: order missing, not in `Paid`, already has an `AwbNumber`, another worker holds a fresh claim, or the vendor deduped onto the number already persisted. Excluded from SLO 4 on both sides — counting these would flag the retry loop rather than a failure |
| `orphaned` | A billable label was created but the order was no longer writable, so nothing references it and the vendor has no void endpoint here — logged at `Error`, and counted as a **failure** in SLO 4 rather than lumped in with `skipped` |
| `retry_later` | Transient failure (network, Sameday auth / protocol drift) — retry job will pick this up |
| `give_up` | Permanent failure (invalid request, vendor validation error) — no retry, ops attention needed |
| `error` | The creation attempt threw before producing an outcome (database unreachable, unexpected fault). Host-shutdown cancellation is deliberately excluded so a deploy does not depress the SLO |

#### `status` (`invoice_anaf_status_total`, future)
| Value | When |
|---|---|
| `accepted` | ANAF SPV accepted the e-Factura submission |
| `rejected` | ANAF SPV returned a rejection (must be corrected and resubmitted) |
| `pending` | Awaiting ANAF processing |
| `failed` | Network / submission failure |

## Auto-instrumented metrics

These are produced by the OpenTelemetry instrumentation packages, not by
application code. Names follow the OTel semantic conventions, not our
naming scheme.

| Source | Notable metric names |
|---|---|
| `OpenTelemetry.Instrumentation.AspNetCore` | `http.server.request.duration` (histogram, seconds) — the source of the latency-p95 panel on the SLO dashboard |
| `OpenTelemetry.Instrumentation.Http` | `http.client.request.duration` (histogram) — visibility into outbound Stripe / Sameday / ANAF calls |
| `OpenTelemetry.Instrumentation.Runtime` | `process.runtime.dotnet.gc.collections.count`, `process.runtime.dotnet.thread_pool.queue.length`, etc. |

These cover availability + latency without any application code changes.

## How to add a new metric

1. Add the constant for the metric name to [`MetricNames.Instruments`](../../src/PhotoPrint.API/Observability/MetricNames.cs).
2. If it has labels, add the label key constant to `MetricNames.Labels` and the enumerated value constants to a nested class (e.g. `MetricNames.NewResultValues`).
3. Add the `Counter<long>` / `Histogram<long>` / `Histogram<double>` static property to [`FotoMetrics`](../../src/PhotoPrint.API/Observability/FotoMetrics.cs).
4. Add the instrument to `MetricNames.LabelContract` — its labels and each label's allowed
   value set (an empty dictionary for an unlabelled instrument). Two tests fail until you do:
   the contract and `FotoMetrics` must declare exactly the same instruments.
5. Increment at the call site using `TagList` (stack-allocated, no GC pressure).
6. Add a test in `PhotoPrint.Tests/Unit/Observability/FotoMetricsTests.cs` that the new instrument has the expected name + type.
7. Add an emission test beside the call site that observes it through `MetricCapture` and
   asserts `ContractViolations()` is empty — a reflection-only test cannot tell whether the
   call site fires or what tags it attaches.
8. Add the instrument's exact expected series count to `MetricsCardinalityTests.DeclaredInstruments`.
9. Update this document.
10. If the metric drives a dashboard panel, edit [`ops/dashboards/fototipar-overview.json`](../../ops/dashboards/fototipar-overview.json) and update the SLO doc. `DashboardMetricNamesTests` holds every dashboard and SLO query against a real `/metrics` exposition, so **a queried name this repo does not declare** fails the build rather than rendering "No Data". It does **not** prove production emits it: the exposition is seeded by the test itself, one observation per declared instrument, so a panel on a declared-but-never-incremented metric (`invoice_anaf_status_total` today) stays green. The test also expects every queried metric to appear in that seeded exposition, so a panel on an instrument the test does not emit needs a seed added there.

Adding a label or a label value is the same flow: extend the nested value class, extend the
instrument's `LabelContract` entry, and update its expected series count.

## Cardinality budget

Per the design (ADR-017 + technical design §NFR), each instrument is
budgeted at ≤ 100 distinct series. The Stage-5 `MetricsCardinalityTests`
enumerate the label combinations and assert the budget.

| Metric | Series count | Headroom |
|---|---|---|
| `orders_created_total` | 6 | 94 |
| `payment_webhook_total` | 12 | 88 |
| `upload_size_bytes` | 1 (no labels) | 99 |
| `order_processing_duration_seconds` | 1 (no labels) | 99 |
| `awb_creation_total` | 5 | 95 |
| `invoice_anaf_status_total` | 4 | 96 |

Plenty of headroom; the budget exists to prevent free-form label leaks,
not to limit growth.

## Operating considerations

- **Scrape interval**: 15s is the typical Prometheus default; we don't
  tune this server-side. Counters are monotonic so any interval is
  correct; histograms see slight quantile drift at long intervals.
- **Storage**: Prometheus retains data per its own configuration; our
  side is push-rate (~6 instruments × small label cardinality × 15 s
  scrape) is negligible.
- **Restart resets counters**. This is standard Prometheus behaviour
  (`rate()` and `increase()` handle the reset correctly). No state is
  persisted across restarts.
- **Histogram buckets** use the OTel default exponential buckets. If a
  future panel needs custom buckets (e.g. for sub-second checkout
  latency precision), define them in the `Histogram<T>` constructor.
