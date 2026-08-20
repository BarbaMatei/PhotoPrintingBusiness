---
stage: design
bolt: 039-efactura-anaf
created: 2026-06-03T11:30:00Z
---

## Technical Design: e-Factura Generation & ANAF Submission

> Stage 1 set the static shape and lifecycle. Stage 2 commits to the
> wires — which library renders the PDF, where the Invoice row gets
> inserted, how the ANAF cert flows through DI, how the worker polls.
> The five open invariants from Stage 1 are resolved here.

### Architecture pattern

**Layered (loose hexagonal), per project standard.** No new
architectural commitments beyond what the existing API already uses.
Bolt 039 adds one fully-formed subsystem under `Services/Invoicing/`,
preserves the established DI conventions, and reuses every
cross-cutting concern (auth, validation, logging, metrics, error
handling) that's already in place.

```text
┌─────────────────────────────────────────────────────────────┐
│  Presentation                                                │
│  ─ Controllers/InvoicesController            (customer)      │
│  ─ Controllers/Admin/AdminInvoicesController (admin)         │
│  ─ DTOs/Invoices/*                                           │
│  ─ Validators/Invoices/*                                     │
├─────────────────────────────────────────────────────────────┤
│  Application                                                 │
│  ─ Services/Invoicing/InvoiceCreationService                 │
│      (orchestrates: NumberingService → Invoice INSERT in     │
│       Stripe webhook tx; called from PaymentService)         │
│  ─ Services/Invoicing/InvoicePdfReadyNotifier                │
│      (fires follow-up email when PDF lands post-confirmation)│
├─────────────────────────────────────────────────────────────┤
│  Domain                                                      │
│  ─ Services/Invoicing/InvoiceXmlBuilder                      │
│      (pure; UBL 2.1 + CIUS-RO)                              │
│  ─ Services/Invoicing/InvoicePdfRenderer                     │
│      (Razor → QuestPDF)                                      │
│  ─ Services/Invoicing/InvoiceLifecycle                       │
│      (CAS façade per ADR-016)                                │
│  ─ Services/Invoicing/InvoiceStorageKeys                     │
│      (caller-supplied key policy per ADR-007)                │
├─────────────────────────────────────────────────────────────┤
│  Infrastructure                                              │
│  ─ Services/Invoicing/Anaf/AnafTokenProvider                 │
│  ─ Services/Invoicing/Anaf/AnafAuthHandler (Delegating)      │
│  ─ Services/Invoicing/Anaf/AnafSpvClient                     │
│  ─ Services/Invoicing/Anaf/InvoiceUploadJob (BackgroundSvc)  │
│  ─ Configuration/{Seller,Anaf,Invoicing}Settings + Validators│
└─────────────────────────────────────────────────────────────┘
```

### Open invariants — committed

The five tentative resolutions from Stage 1 are now binding decisions.

| # | Open invariant | Committed answer |
|---|---|---|
| 1 | Invoice row creation site | **Inside the Stripe webhook transaction.** Called from `PaymentService.HandleSucceededAsync` (existing) via a new `IInvoiceCreationService.CreateForOrderAsync`. One additional `INSERT` joins the existing `SaveChangesAsync`; idempotency (bolt 035) and gap-on-rollback acceptance (ADR-020) are both preserved. |
| 2 | PDF rendering library | **QuestPDF 2024.10+.** No Chromium in the prod image. Razor `.cshtml` is replaced with a QuestPDF DSL document; the email-template Razor skill doesn't transfer cleanly, but the operational simplicity wins (~15MB DLL vs ~200MB headless browser, sub-100ms cold-start vs multi-second). **Candidate for Stage 3 ADR.** |
| 3 | Dual-write rollout posture | **Feature flag**: `Invoicing:CustomerEmailAttachments:Enabled` (default `false`). When `false`, XML build + ANAF upload + PDF render all run; only the customer-facing email attachment is suppressed. Production cutover: flip to `true` after the dual-write inspection week. **Candidate for Stage 3 ADR.** |
| 4 | Worker dispatch model | **DB polling, not in-process `Channel<T>`.** `InvoiceUploadJob : BackgroundService` queries `Invoices WHERE AnafStatus IN ('Pending','Submitted')` every `Anaf:PollIntervalMinutes` (default 30). ADR-010's reasoning (low-latency reaction + per-replica crash recovery) does not transfer — ANAF's 5-business-day SLA tolerates 30-min polling. No new ADR; this is the obvious choice. |
| 5 | Email pipeline coordination | **Order-confirmation email always fires immediately** (existing behaviour, unchanged). If `Invoice.PdfStoragePath` is set at that moment, the PDF is attached. Otherwise, the existing email goes out as-is, and a separate `InvoicePdfReadyNotifier` (called from `InvoicePdfRenderer` right after `IStorageService.SaveAsync` returns) sends a small "Your invoice is ready" email with the PDF attached. The follow-up email is suppressed when the dual-write flag is `false`. |

### Layer responsibilities

#### Presentation layer

**`InvoicesController`** (customer-facing, JWT + Guest token both accepted via existing dual-auth):

```text
GET  /api/orders/{orderId}/invoice
  → 200 application/pdf   (PDF stream from IStorageService)
  → 404 Not Found         (Invoice row missing OR PdfStoragePath null)
  → 403 Forbidden         (order not owned by caller; existing ownership helper)
  Headers:
    Cache-Control: private, max-age=31536000, immutable
    Retry-After: 30        (only on 404 when Invoice row exists but PDF pending)
```

Returns the *PDF*, not the XML — the customer never sees the UBL bytes
by design (story 003).

**`Admin/AdminInvoicesController`** (Admin role required):

```text
GET  /api/admin/invoices?status=Pending|Submitted|Accepted|Rejected|Failed
                       &page=1&size=20
  → 200 application/json
  Response: { items: AdminInvoiceListItem[], total: int, page: int, size: int }
  Pagination: 1-based pages, default size 20, max size 100 (project convention)

POST /api/admin/invoices/{invoiceId}/retry
  → 200 application/json   { invoiceId, oldStatus, newStatus: "Pending" }
  → 404 Not Found
  → 409 Conflict           per ADR-004 — only Rejected/Failed are retryable
  Body: (none)

GET  /api/admin/invoices/{invoiceId}/xml
  → 200 application/xml    (raw XmlPayload bytes; charset utf-8)
  → 404 Not Found          (Invoice missing OR XmlPayload null)
```

**DTOs** (`DTOs/Invoices/`):

```text
AdminInvoiceListItem {
  invoiceId         : Guid
  orderId           : Guid
  orderNumber       : string
  invoiceNumber     : string
  issuedAt          : DateTimeOffset
  anafStatus        : string         // enum-as-string (Pending|Submitted|...)
  anafUploadId      : string?
  lastError         : string?
}

AdminInvoiceListQuery {              // bound from query string; FluentValidation
  status            : string?        // optional filter
  page              : int = 1
  size              : int = 20
}

PaymentMethodNote     // out of scope — uses existing payment processor naming
```

**FluentValidation** rejects size > 100 and page < 1 (per ADR-002).
No data annotations.

#### Application layer

**`IInvoiceCreationService`** — orchestrates the on-Paid invoice
row creation. Called from `PaymentService.HandleSucceededAsync`
(existing) inside its existing transaction.

```text
CreateForOrderAsync(orderId: Guid, ct: CancellationToken) → Invoice

Behaviour:
  1. Idempotency: SELECT 1 FROM Invoices WHERE OrderId = @orderId.
     If a row exists, return it (Stripe webhook replay path —
     bolt 035 guarantees this is reachable).
  2. Load the Order (with Items) by id.
  3. Allocate the next number:
        var number = await IInvoiceNumberingService.NextNumberAsync(
          series: VatSettings.InvoiceSeries,  // "FT"
          year:   Order.PaidAt.Year,
          ct);
  4. Construct Invoice:
        Number          = number.Number
        Series          = number.Series
        InvoiceNumber   = number.ToString()    // "FT-2026-00001"
        IssuedAt        = Order.PaidAt         // legal issue date = paid
        NetTotalRon     = Order.NetTotalRon    // bolt-038 snapshot
        VatRon          = Order.VatRon
        TotalRon        = Order.TotalRon
        AnafStatus      = Pending
        CreatedAt       = (Postgres NOW())
        XmlPayload      = null   (built async by InvoiceUploadJob)
        PdfStoragePath  = null   (rendered async)
        AnafUploadId    = null
        LastError       = null
  5. Add to DbContext. DO NOT call SaveChangesAsync — the caller
     (PaymentService) commits the whole batch in one transaction.
  6. Return the (tracked but unsaved) Invoice.
```

This is the load-bearing step the gap-free promise depends on. The
`SELECT nextval()` (in `PostgresInvoiceNumberingService`) and the
`INSERT` happen in the same transactional scope — if the Paid
transition rolls back, both are gone together, and ADR-020's
accepted gap-on-rollback is the only failure mode.

**`InvoicePdfReadyNotifier`** — small façade over the existing
`IEmailService`. Sends a follow-up email when an invoice's PDF
becomes available after the confirmation email already fired.

```text
NotifyAsync(invoice: Invoice, order: Order, ct: CancellationToken)
  - Skipped when Invoicing:CustomerEmailAttachments:Enabled == false
  - Else: fetches PDF bytes from IStorageService, sends a small
    "Your invoice is ready" email with attachment
  - Idempotency: the email body carries the invoice number; duplicate
    sends on multi-replica are tolerated (email is at-least-once
    semantics, well-understood)
```

#### Domain layer

**`InvoiceXmlBuilder : IInvoiceXmlBuilder`** — pure, no DI besides
`IOptions<SellerSettings>`. Uses `System.Xml.Linq` (not `XmlSerializer`)
per story 001's technical note — 200-line hand-rolled builder, easier
to audit than generated bindings.

Input projection:
```text
Build(order: Order, invoice: Invoice, seller: Seller) → byte[]

Reads from order:
  - Items[]  (Description, Quantity, UnitPriceRon)
  - ShippingCostRon
  - PaymentProcessor, AwbNumber  (BT-22 Note free text)
  - Buyer projection (Email, Name?, ShippingAddress)
Reads from invoice:
  - InvoiceNumber, IssuedAt, NetTotalRon, VatRon, TotalRon, VatRate
Reads from seller:
  - All fields (Name, Cui, RegistrationNumber, Address, IbanRon)

Emits UTF-8 XML byte array:
  <?xml version="1.0" encoding="UTF-8"?>
  <Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
           xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
           xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2">
    <cbc:UBLVersionID>2.1</cbc:UBLVersionID>
    <cbc:CustomizationID>urn:cen.eu:en16931:2017#compliant#urn:efactura.mfinante.ro:CIUS-RO:1.0.1</cbc:CustomizationID>
    <cbc:ID>{InvoiceNumber}</cbc:ID>                        <!-- BT-1 -->
    <cbc:IssueDate>{IssuedAt:yyyy-MM-dd}</cbc:IssueDate>     <!-- BT-2 -->
    <cbc:InvoiceTypeCode>380</cbc:InvoiceTypeCode>           <!-- BT-3 -->
    <cbc:Note>{OrderNumber}{ AWB ↦ "/ AWB " + AwbNumber }</cbc:Note>  <!-- BT-22 -->
    <cbc:DocumentCurrencyCode>RON</cbc:DocumentCurrencyCode>
    <cac:AccountingSupplierParty>...</cac:AccountingSupplierParty>  <!-- BG-4 / BT-31,32 -->
    <cac:AccountingCustomerParty>...</cac:AccountingCustomerParty>  <!-- BG-7 / BT-44+ -->
    <cac:PaymentMeans>...</cac:PaymentMeans>                        <!-- BG-19 -->
    <cac:TaxTotal>...</cac:TaxTotal>                                <!-- BG-23 -->
    <cac:LegalMonetaryTotal>...</cac:LegalMonetaryTotal>            <!-- BG-22 -->
    <cac:InvoiceLine>...</cac:InvoiceLine>                          <!-- BG-25 (one per item + 1 for shipping) -->
  </Invoice>

Currency emission rule:
  Every cbc:* monetary element carries currencyID="RON" and is
  formatted with InvariantCulture ToString("F2") to guarantee
  "1234.56" (not "1234,56") regardless of host locale.

Guest buyer rule (story 001 edge case):
  When order.User is null (guest):
    - BT-44 (BuyerName)       = "Persoană fizică"
    - BT-48 (BuyerVATIdentifier) → element omitted entirely
    - BT-50 (BuyerAddress)    = order.ShippingAddress  (still required)

Empty-line rejection:
  if (order.Items.Count == 0) throw new InvalidOperationException(
    $"Cannot build invoice {invoice.InvoiceNumber}: order has no items");
```

**`InvoicePdfRenderer : IInvoicePdfRenderer`** — QuestPDF DSL.
Replaces the originally-planned `Templates/Invoices/Invoice.cshtml`
Razor approach (resolved per invariant #2). The template lives as
C# in `Services/Invoicing/InvoicePdfDocument.cs` and consumes the
same input shape as the XML builder.

```text
RenderAsync(order, invoice, seller, ct) → byte[]
  - Builds a QuestPDF.Infrastructure.IDocument
  - Calls .GeneratePdf() → byte[]
  - One A4 page typically; auto-overflow for many-line orders
  - Locale: ro-RO for date and number formatting (DateTimeFormatInfo
    + NumberFormatInfo cached statically)

QuestPDF license:
  QuestPDF 2024.10+ requires explicit license declaration. We use
  the Community License (free for businesses < $1M revenue). The
  attribute lives in Program.cs:
    QuestPDF.Settings.License = LicenseType.Community;
  Validator throws at boot if a paid license is detected without
  ALSO declaring it explicitly.
```

**`InvoiceLifecycle : IInvoiceLifecycle`** — thin façade around
`ExecuteUpdateAsync` per ADR-016. Every status mutation in bolt 039
goes through this.

```text
TryTransitionAsync(invoiceId, expected, target, mutator, ct) → bool

Implementation sketch:
  var setters = SetPropertyCalls.For(target);   // .SetProperty(i => i.AnafStatus, target.ToString())
                                                //   then merges mutator's writes via builder pattern
  var affected = await db.Invoices
    .Where(i => i.Id == invoiceId && i.AnafStatus == expected.ToString())
    .ExecuteUpdateAsync(setters, ct);
  return affected == 1;

The mutator delegate captures field writes (AnafUploadId, LastError,
UpdatedAt = NOW()). The implementation translates them into the
ExecuteUpdate's SetProperty chain — no entity tracking, no
SaveChanges.

Caller pattern (worker):
  var ok = await lifecycle.TryTransitionAsync(
    invoiceId,
    expected: InvoiceAnafStatus.Submitted,
    target:   InvoiceAnafStatus.Accepted,
    mutator:  inv => { inv.UpdatedAt = DateTimeOffset.UtcNow; },
    ct);
  if (!ok) {
    log.LogInformation("CAS lost for invoice {id} (expected={exp})", invoiceId, expected);
    return;
  }
```

**`InvoiceStorageKeys`** — caller-supplied key helper per ADR-007.

```text
static class InvoiceStorageKeys {
  public static string ForPdf(Invoice invoice) =>
    $"invoices/{invoice.IssuedAt:yyyy}/{invoice.IssuedAt:MM}/{invoice.InvoiceNumber}.pdf";
}
```

#### Infrastructure layer

**`AnafTokenProvider : IAnafTokenProvider`** — ADR-013 shape, but
with a PKCS#12 client cert attached to the OAuth POST (which the
Sameday flow didn't need).

```text
DI lifetime: Singleton.
State: { string? token, DateTimeOffset expiresAt, SemaphoreSlim sem }

GetAccessTokenAsync(ct):
  if (token != null && expiresAt > now + 60s) return token;
  await sem.WaitAsync(ct);
  try {
    if (token != null && expiresAt > now + 60s) return token;  // double-check
    using var handler = new HttpClientHandler();
    handler.ClientCertificates.Add(LoadCert());
    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
    using var client = new HttpClient(handler);
    var response = await client.PostAsync(
      $"{anaf.BaseUrl}/oauth/token",
      new FormUrlEncodedContent(new[] {
        new KeyValuePair<string,string>("grant_type", "client_credentials"),
        new KeyValuePair<string,string>("client_id",  anaf.ClientId),
        new KeyValuePair<string,string>("client_secret", anaf.ClientSecret),
      }),
      ct);
    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
    token = body.AccessToken;
    expiresAt = DateTimeOffset.UtcNow.AddSeconds(body.ExpiresIn);
    return token;
  } finally { sem.Release(); }

InvalidateToken():
  token = null; expiresAt = default;

LoadCert():
  - Reads anaf.CertPath (absolute path, from env via Anaf__CertPath)
  - Loads with X509KeyStorageFlags.MachineKeySet | PersistKeySet
  - Cached in a static field; reloaded only on InvalidateToken with
    a force-reload path (NOT triggered by 401 — only by manual ops)
  - Boot validator (see Configuration section) confirms file exists
    and is loadable; we DO NOT log the cert subject/thumbprint
    (information disclosure)
```

**`AnafAuthHandler : DelegatingHandler`** — ADR-014 shape exactly.

```text
SendAsync(request, ct):
  request.Headers.Authorization = new("Bearer", await tokenProvider.GetAccessTokenAsync(ct));
  var response = await base.SendAsync(request, ct);
  if (response.StatusCode != HttpStatusCode.Unauthorized) return response;

  // First 401 — refresh token and retry exactly once
  log.LogWarning("ANAF returned 401 — invalidating token and retrying once");
  tokenProvider.InvalidateToken();
  response.Dispose();
  request.Headers.Authorization = new("Bearer", await tokenProvider.GetAccessTokenAsync(ct));
  response = await base.SendAsync(request, ct);
  if (response.StatusCode == HttpStatusCode.Unauthorized)
    throw new AnafAuthException("ANAF returned 401 after token refresh");
  return response;
```

Polly stays separate (named-client policy registry, identical to
Sameday). Retry set: 5xx, 408, 429. Budget: 3 retries with
exponential backoff (1s, 2s, 4s) and jitter. **401 is never in the
Polly set.**

**`AnafSpvClient : IAnafSpvClient`** — the HTTP shape against ANAF.

```text
UploadAsync(xmlBytes, ct) → AnafUploadResult
  POST {anaf.BaseUrl}/upload?standard=UBL&cif={seller.CuiNumericOnly}
  Content-Type: application/xml
  Body: xmlBytes
  Response: 200 OK
    Body: <header xmlns="..." index_incarcare="12345" data_incarcare="2026-06-03 11:30:00" />
    Parsed to AnafUploadResult(UploadId="12345", SubmittedAt=...)
  Response: 200 OK with errors (ANAF returns errors WITH 200 sometimes)
    Body contains <Errors>...</Errors>
    Throws AnafUploadException with the error text — handled by worker as TransientFailure
  Response: 400/401/5xx → standard HTTP error (Polly or AuthHandler handle)

GetStatusAsync(uploadId, ct) → AnafStatusResult
  GET {anaf.BaseUrl}/stareMesaj?id_incarcare={uploadId}
  Response: 200 OK
    <header xmlns="..." stare="ok|nok|in prelucrare" id_descarcare="..." />
  Maps stare:
    "ok"            → AnafExternalStatus.Validated
    "nok"           → AnafExternalStatus.Rejected (ErrorMessage from <Errors>)
    "in prelucrare" → AnafExternalStatus.InProgress
    other           → AnafExternalStatus.Unknown
```

Note on the auth-vs-Polly ordering in the DelegatingHandler chain
(builds outer-to-inner via `AddHttpMessageHandler`):

```text
HttpClient
  → AnafAuthHandler                  (outer — owns 401 retry)
  → Polly transient-failure handler  (inner — owns 5xx retry)
  → SocketsHttpHandler
```

The Auth handler is outer because a 401 might come back from any 5xx
that had been retried by Polly first. The order matches the Sameday
client's chain.

**`InvoiceUploadJob : BackgroundService`** — the tick loop.

```text
ExecuteAsync(ct):
  using var timer = new PeriodicTimer(TimeSpan.FromMinutes(anaf.PollIntervalMinutes));
  while (await timer.WaitForNextTickAsync(ct))
    try { await ProcessBatch(ct); }
    catch (Exception ex) { log.LogError(ex, "InvoiceUploadJob batch failed"); }

ProcessBatch(ct):
  // Fetch one batch — bounded to avoid runaway
  var batch = await db.Invoices
    .Where(i => i.AnafStatus == "Pending" || i.AnafStatus == "Submitted")
    .OrderBy(i => i.CreatedAt)
    .Take(anaf.MaxBatchSize)     // default 50
    .Select(i => new { i.Id, i.OrderId, i.AnafStatus })
    .ToListAsync(ct);

  foreach (var row in batch)
    try { await ProcessOne(row.Id, row.OrderId, row.AnafStatus, ct); }
    catch (OperationCanceledException) { throw; }
    catch (Exception ex) { log.LogError(ex, "Invoice {id} processing failed", row.Id); }

ProcessOne(invoiceId, orderId, status, ct):
  switch (status) {
    case "Pending":   await UploadPending(invoiceId, orderId, ct); break;
    case "Submitted": await PollSubmitted(invoiceId, ct);          break;
  }

UploadPending(invoiceId, orderId, ct):
  // Reload Invoice + Order (worker may have built XML on a prior tick that crashed mid-write)
  var (invoice, order) = await LoadPair(invoiceId, orderId, ct);
  if (invoice.XmlPayload == null) {
    var xml = xmlBuilder.Build(order, invoice, seller);
    invoice.XmlPayload = Encoding.UTF8.GetString(xml);  // stored as text
    await db.SaveChangesAsync(ct);                       // separate tx, idempotent rebuild
    metrics.IncrementInvoiceXmlBuilt();
  }
  if (invoice.PdfStoragePath == null) {
    var pdf = await pdfRenderer.RenderAsync(order, invoice, seller, ct);
    var key = InvoiceStorageKeys.ForPdf(invoice);
    await storage.SaveAsync(new MemoryStream(pdf), key, ct);
    invoice.PdfStoragePath = key;
    await db.SaveChangesAsync(ct);
    await pdfReadyNotifier.NotifyAsync(invoice, order, ct);  // skipped under feature flag
    metrics.IncrementInvoicePdfRendered();
  }
  try {
    var result = await anafSpv.UploadAsync(Encoding.UTF8.GetBytes(invoice.XmlPayload!), ct);
    var ok = await lifecycle.TryTransitionAsync(
      invoiceId,
      expected: InvoiceAnafStatus.Pending,
      target:   InvoiceAnafStatus.Submitted,
      mutator:  inv => { inv.AnafUploadId = result.UploadId; inv.UpdatedAt = DateTimeOffset.UtcNow; inv.LastError = null; },
      ct);
    if (ok) metrics.IncrementAnafStatus("submitted");
  }
  catch (AnafUploadException ex) {     // ANAF returned errors with 200
    await lifecycle.TryTransitionAsync(
      invoiceId,
      expected: InvoiceAnafStatus.Pending,
      target:   InvoiceAnafStatus.Pending,    // stays pending, error recorded
      mutator:  inv => { inv.LastError = ex.Message; inv.UpdatedAt = DateTimeOffset.UtcNow; },
      ct);
    log.LogWarning(ex, "ANAF upload returned errors for invoice {id}", invoiceId);
  }
  // 5xx / 401: standard HTTP exceptions propagate up; the foreach
  // catch logs and the next tick retries naturally.

PollSubmitted(invoiceId, ct):
  var invoice = await db.Invoices.AsNoTracking().FirstAsync(i => i.Id == invoiceId, ct);
  var result = await anafSpv.GetStatusAsync(invoice.AnafUploadId!, ct);
  switch (result.Status) {
    case AnafExternalStatus.Validated:
      await lifecycle.TryTransitionAsync(
        invoiceId, expected: Submitted, target: Accepted,
        mutator: inv => { inv.UpdatedAt = DateTimeOffset.UtcNow; inv.LastError = null; },
        ct);
      metrics.IncrementAnafStatus("accepted");
      break;
    case AnafExternalStatus.Rejected:
      // Decide whether to escalate to Failed based on backoff schedule
      var attempts = CountRejections(invoice);  // see "Backoff schedule" below
      var target   = attempts >= 4 ? Failed : Rejected;
      await lifecycle.TryTransitionAsync(
        invoiceId, expected: Submitted, target: target,
        mutator: inv => { inv.LastError = result.ErrorMessage; inv.UpdatedAt = DateTimeOffset.UtcNow; },
        ct);
      metrics.IncrementAnafStatus(target.ToString().ToLowerInvariant());
      break;
    case AnafExternalStatus.InProgress:
    case AnafExternalStatus.Unknown:
      // No transition. UpdatedAt mutated only.
      break;
  }
```

#### Configuration & validators

Three new option blocks; one existing block (`Vat`) untouched.

**`SellerSettings`**:
```text
public sealed class SellerSettings {
  public string Name                = "";
  public string Cui                 = "";       // "RO12345678"
  public string RegistrationNumber  = "";       // "J40/1234/2026"
  public string IbanRon             = "";
  public AddressBlock Address       = new();
}

public sealed class AddressBlock {
  public string Line1        = "";
  public string City         = "";
  public string PostalCode   = "";
  public string CountryCode  = "RO";
}
```

`SellerSettingsValidator` (FluentValidation):
- `Name`: non-empty, max 200
- `Cui`: matches `^RO\d{2,10}$`
- `RegistrationNumber`: non-empty, max 50
- `Address.*`: all non-empty; `CountryCode` matches `^[A-Z]{2}$`
- `IbanRon`: optional (some sellers operate cash-on-delivery only)

**`AnafSettings`**:
```text
public sealed class AnafSettings {
  public bool   Enabled              = false;
  public string BaseUrl              = "";       // https://api.anaf.ro/test/FCTEL/rest
  public string ClientId             = "";
  public string ClientSecret         = "";
  public string CertPath             = "";       // /etc/secrets/anaf.p12
  public string CertPassword         = "";
  public int    PollIntervalMinutes  = 30;
  public int    MaxBatchSize         = 50;
  public int[]  BackoffHours         = { 1, 4, 16, 64 };   // story 002
}
```

`AnafSettingsValidator`:
- When `Enabled = true`, ALL of `BaseUrl`, `ClientId`, `ClientSecret`,
  `CertPath`, `CertPassword` are required
- `BaseUrl` validates as `Uri` with scheme in `{http, https}`
- `CertPath` — file existence check at validator time (`File.Exists`),
  per the same "fail fast at boot" posture
- `PollIntervalMinutes` in `[1, 1440]`
- `MaxBatchSize` in `[1, 500]`
- `BackoffHours` non-empty array; each element in `[1, 168]` (1 week max)

**`InvoicingSettings`**:
```text
public sealed class InvoicingSettings {
  public InvoicingEmailSettings CustomerEmailAttachments = new();
}

public sealed class InvoicingEmailSettings {
  public bool Enabled = false;     // dual-write rollout flag (open invariant #3)
}
```

`InvoicingSettingsValidator`: shape-only; `Enabled` defaults to `false`.

**`appsettings.json` additions**:
```jsonc
{
  "Seller": {
    "Name": "FotoTipar SRL",
    "Cui": "RO12345678",
    "RegistrationNumber": "J40/1234/2026",
    "IbanRon": "",
    "Address": {
      "Line1": "Str. Exemplu 1",
      "City": "București",
      "PostalCode": "010101",
      "CountryCode": "RO"
    }
  },
  "Anaf": {
    "Enabled": false,
    "BaseUrl": "https://api.anaf.ro/test/FCTEL/rest",
    "ClientId": "",
    "ClientSecret": "",
    "CertPath": "",
    "CertPassword": "",
    "PollIntervalMinutes": 30,
    "MaxBatchSize": 50,
    "BackoffHours": [1, 4, 16, 64]
  },
  "Invoicing": {
    "CustomerEmailAttachments": { "Enabled": false }   // see Stage 3 ADR candidate
  }
}
```

Secrets (`ClientSecret`, `CertPath`, `CertPassword`) are populated
via environment variables in production:
`Anaf__ClientSecret`, `Anaf__CertPath`, `Anaf__CertPassword`. They
never live in committed config.

#### Backoff schedule

The retry budget per story 002 is `1h, 4h, 16h, 64h` then `Failed`.
The schedule is anchored on `Invoice.UpdatedAt` and the count of
rejections is computed *implicitly* from the polling window:

```text
attempts = number of completed Submitted → Rejected → Pending cycles
         = a row read from a small derived projection over the
           invoice's lifecycle, computed at PollSubmitted time
```

**Simpler model used in v1**: a synthetic `AttemptNumber` is NOT
persisted. Instead, the worker computes "is this the 4th rejection?"
by checking `UpdatedAt` against the schedule:

```text
hoursSinceCreated = (now - invoice.CreatedAt).TotalHours
if (hoursSinceCreated > sum(backoff)) → escalate to Failed
                                       (i.e. >= 85h since creation)
```

This is approximate but correct in the happy retry case; a manual
admin retry resets `UpdatedAt` and the budget rolls forward by the
same amount. Stage 5 will pin this as a test invariant.

If incidents show the synthetic count is too lossy, a follow-up bolt
can introduce an `Invoice.RejectionCount int NOT NULL DEFAULT 0`
column. Not in scope for bolt 039.

#### DI registration sketch (in `Program.cs`)

```text
builder.Services.Configure<SellerSettings>(config.GetSection("Seller"));
builder.Services.Configure<AnafSettings>(config.GetSection("Anaf"));
builder.Services.Configure<InvoicingSettings>(config.GetSection("Invoicing"));

builder.Services.AddSingleton<IValidateOptions<SellerSettings>, SellerSettingsValidator>();
builder.Services.AddSingleton<IValidateOptions<AnafSettings>, AnafSettingsValidator>();
builder.Services.AddSingleton<IValidateOptions<InvoicingSettings>, InvoicingSettingsValidator>();

builder.Services.AddScoped<IInvoiceCreationService, InvoiceCreationService>();
builder.Services.AddScoped<IInvoiceXmlBuilder, InvoiceXmlBuilder>();
builder.Services.AddScoped<IInvoicePdfRenderer, InvoicePdfRenderer>();
builder.Services.AddScoped<IInvoiceLifecycle, InvoiceLifecycle>();
builder.Services.AddScoped<InvoicePdfReadyNotifier>();

builder.Services.AddSingleton<IAnafTokenProvider, AnafTokenProvider>();
builder.Services.AddTransient<AnafAuthHandler>();

builder.Services.AddHttpClient<IAnafSpvClient, AnafSpvClient>(http => {
    http.BaseAddress = new Uri(anaf.BaseUrl);
    http.Timeout = TimeSpan.FromSeconds(30);
  })
  .AddHttpMessageHandler<AnafAuthHandler>()
  .AddTransientHttpErrorPolicy(b => b.WaitAndRetryAsync(3,
      attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

// Worker registered only when Anaf:Enabled = true
if (anaf.Enabled)
  builder.Services.AddHostedService<InvoiceUploadJob>();

QuestPDF.Settings.License = LicenseType.Community;  // Stage 3 ADR candidate
```

The conditional `AddHostedService` matches the bolt-045/036 master-flag
pattern. With `Anaf:Enabled = false`, the entire submission pipeline
is dormant — XML never built, PDF never rendered, customer endpoint
returns 404 because `PdfStoragePath` is null. The invoice row is still
created (the schema/numbering side stays on always).

### API design (consolidated)

| Method | Path | Auth | Response | Notes |
|---|---|---|---|---|
| `GET` | `/api/orders/{orderId}/invoice` | JWT or Guest | `200 application/pdf` / `404` / `403` | Customer PDF download; immutable cache header |
| `GET` | `/api/admin/invoices` | Admin role | `200 application/json` (paged) | Query: `status, page, size` |
| `POST` | `/api/admin/invoices/{id}/retry` | Admin role | `200` / `404` / `409` | 409 per ADR-004 if status not in {Rejected, Failed} |
| `GET` | `/api/admin/invoices/{id}/xml` | Admin role | `200 application/xml` / `404` | Raw UBL bytes |

Existing endpoints **unchanged**:
- `GET /api/orders/{id}` (`OrderDetailDto` already carries `NetTotalRon`, `VatRon`, `VatRate` per bolt 038)
- Stripe webhook (gets one extra service call inside the existing transaction)

### Data model

**No new tables, no new columns.** Bolt 038's migration already
shipped:
- `Orders.NetTotalRon, VatRon, VatRate`
- `Invoices` table with the full column set
- `invoice_seq_ft_2026` Postgres sequence
- Unique index `uq_invoices_series_year_number`
- Indexes on `OrderId` and `AnafStatus`

Bolt 039 generates **no migration**. The DB sees additional writes
but no DDL.

### Security design

| Concern | Approach |
|---|---|
| **ANAF client cert (`.p12`)** | Path via env var (`Anaf__CertPath`). File mode 0400 on host. Never read into a log. Boot-time validator checks `File.Exists`. Cached in a static `X509Certificate2` after first successful load. |
| **ANAF client secret + cert password** | Env vars only (`Anaf__ClientSecret`, `Anaf__CertPassword`). Sentry scope scrubber (bolt 045) blocks these key names automatically. |
| **ANAF XML payload (PII)** | Contains buyer name + address + email + total. **Never logged.** The `AnafSpvClient` logs at `Information` with status code only; body is omitted. A Serilog destructuring policy excludes `byte[]` request bodies. |
| **Customer PDF download** | `[Authorize]` + existing ownership helper (`order.UserId == caller.UserId OR order.GuestToken == caller.GuestToken`). |
| **Admin endpoints** | `[Authorize(Roles="Admin")]` — matches existing admin convention. |
| **PDF URLs are not public** | Stored at storage paths `invoices/yyyy/MM/{InvoiceNumber}.pdf`; served only via the JWT-gated controller. No pre-signed URL or public bucket policy. |
| **Cross-tenant leak prevention** | All admin queries return paged results; ownership check on customer endpoint. No tenant column in scope (single-tenant app). |
| **Cert rotation** | Process restart only. Operator workflow: replace `.p12` file → restart container. No SIGHUP listener (out of scope; the rotation cadence is annual). |

### NFR implementation

| Requirement | Design approach |
|---|---|
| **Submit within 5 business days** | 30-min polling cadence + 4 retry tiers (1h, 4h, 16h, 64h) covers the SLA with room to spare. Worst case: invoice issued Friday 17:00, 5 retry attempts complete by Tuesday 09:00. |
| **No customer-facing latency** | Invoice row INSERT inside the Stripe webhook tx adds ≤5ms; XML build, PDF render, ANAF upload all run async on the background worker. Customer sees the order-confirmation email immediately (existing). PDF arrives via follow-up email when ready. |
| **Multi-replica safety** | Per ADR-015: two replicas may dispatch the same invoice. ANAF dedupes via `InvoiceNumber`; CAS via ADR-016 ensures only one replica wins the status transition. Cost: occasional duplicate HTTP call to ANAF (vendor's rate limit absorbs). |
| **Worker resilience** | Each batch wrapped in try/catch; one bad invoice doesn't poison the batch. The next tick re-fetches; transient failures self-heal. |
| **PDF cache** | `Cache-Control: private, max-age=31536000, immutable`. PDF bytes are content-addressable by `InvoiceNumber`. |
| **Observability** | Bolt 044's `invoice_anaf_status_total{status}` counter wired in at every CAS transition. Sentry scope (bolt 045) tags ANAF spans with `vendor=anaf` for dashboard isolation. |
| **Audit trail** | Admin endpoints log at Info with admin user id + operation (matching the existing admin pattern). State transitions log at Info with old/new status. |

### Integration points

| Integration | How it lands |
|---|---|
| **Existing `PaymentService.HandleSucceededAsync`** | Add `await invoiceCreationService.CreateForOrderAsync(orderId, ct);` BEFORE `SaveChangesAsync`. One additional INSERT joins the existing tx. |
| **Existing `IStorageService`** | `SaveAsync(stream, key, ct)` for PDF; key from `InvoiceStorageKeys.ForPdf`. Adheres to ADR-007 (caller-supplied keys). |
| **Existing `IEmailService`** | Two paths: (a) `OrderConfirmationEmail` checks `Invoice.PdfStoragePath` at send time and attaches if present; (b) `InvoicePdfReadyNotifier` sends a separate "Your invoice is ready" follow-up when the PDF lands after the confirmation email already went out. |
| **Existing dual-auth (JWT or Guest)** | Customer endpoint reuses the existing dual-auth filter and ownership helper. |
| **ANAF SPV (external)** | One `HttpClient` named "anaf-spv", pipeline `AnafAuthHandler → Polly → SocketsHttpHandler`. Tests use a `WireMockServer` fixture (no production cert needed). |
| **Metrics (bolt 044)** | `FotoMetrics.IncrementInvoiceXmlBuilt()`, `IncrementInvoicePdfRendered()`, `IncrementAnafStatus(status)`. The meter names follow `bolt 044`'s `MetricNames.cs` registry. |
| **Sentry (bolt 045)** | The scope enricher already runs; ANAF exceptions propagate naturally to the existing `ExceptionHandlerMiddleware` → `IHub.CaptureEvent`. Cert/secret keys are in the scrubber list already (per `SentryDataScrubbers`). |

### Testing strategy

| Layer | Tool | Scope |
|---|---|---|
| **XML builder** | xUnit + `XmlDocument.Validate` against `UBL-Invoice-2.1.xsd` (resource-embedded) | Required UBL business terms present; guest-buyer edge case omits BT-48; zero-line throws |
| **PDF renderer** | xUnit + QuestPDF document tree assertions | Document contains seller name, buyer name, invoice number, totals string match (ro-RO formatting); page count > 0 |
| **ANAF client** | xUnit + WireMock | Happy-path upload returns `UploadId`; 401 retries once via `AnafAuthHandler`; status parser correctly maps each `stare` value |
| **Worker** | xUnit + EF InMemory (rejected — use a throwaway PostgreSQL database to cover real LINQ translation, mirroring bolt 038's lesson) + Mocked `IAnafSpvClient` | One tick processes a `Pending` invoice → `Submitted`; CAS race simulated by pre-mutating row; backoff-exhausted invoice escalates to `Failed` |
| **Customer endpoint** | xUnit + WAF (`WebApplicationFactory`) | 200 with PDF bytes when owner; 403 when not owner; 404 when row missing; cache header present |
| **Admin endpoints** | xUnit + WAF | Pagination respected; 409 on retry for `Submitted` status (per ADR-004) |
| **Sequence concurrency** | Already covered by bolt 038's tests | — |

XSD validation is the load-bearing test in the suite. It catches
schema drift, missing namespaces, omitted business terms, and the
guest-buyer omission edge case all in one assertion.

### Open questions for Stage 3 (ADR analysis)

Candidate decisions worth ADR-elevation:

1. **PDF library = QuestPDF (not PuppeteerSharp).** Documents the
   trade-off; would prevent a future PR from quietly switching to
   PuppeteerSharp under "we already use Chromium for tests" reasoning.
2. **Dual-write rollout via feature flag, not branch deploy.** Worth
   pinning the rollout posture so the next regulated integration
   (e.g. credit-note submission) uses the same pattern.
3. **Worker dispatch via DB polling rather than in-process Channel.**
   Diverges from ADR-010's promotion-queue choice; an ADR makes the
   "5-business-day SLA tolerates polling" argument explicit.
4. **Implicit attempt-count from `UpdatedAt` instead of a persisted
   counter.** A judgment call; worth pinning so a future PR doesn't
   add the column without engaging with the trade-off.
5. **QuestPDF Community License declaration.** Touches business
   constraints (the $1M revenue cap). Worth a decision record.

Stage 3 will present these to the user and create the selected ones.

### Stories coverage check

- ✅ Story **001 (UBL XML builder)** — `InvoiceXmlBuilder` implementation outline, BG/BT coverage, currency emission rule, guest-buyer rule, empty-line rejection, XSD validation in tests.
- ✅ Story **002 (ANAF SPV client + worker)** — `AnafTokenProvider`, `AnafAuthHandler`, `AnafSpvClient`, `InvoiceUploadJob`, OAuth + cert flow, retry budget, multi-replica posture, logging hygiene.
- ✅ Story **003 (PDF renderer + endpoint)** — `InvoicePdfRenderer` via QuestPDF, storage key policy, customer endpoint, email-attachment trigger, follow-up email via `InvoicePdfReadyNotifier`.
- ✅ Story **004 (Admin list + retry)** — three admin endpoints, paging, 409 on wrong-state retry, raw XML download.
