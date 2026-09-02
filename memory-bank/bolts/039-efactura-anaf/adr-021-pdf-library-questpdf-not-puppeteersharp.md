---
bolt: 039-efactura-anaf
created: 2026-06-03T12:30:00Z
status: accepted
---

# ADR-021: PDF Library — QuestPDF, Not PuppeteerSharp

## Context

Bolt 039 introduces customer-facing invoice PDFs. The PDF renderer
(`IInvoicePdfRenderer`) needs to turn an `(Order, Invoice, Seller)`
projection into a byte stream that:

- Is legally readable as a Romanian tax receipt (no signature, no
  letterhead — see story 003 acceptance criteria for the field list).
- Is rendered consistently across environments (dev laptop, CI,
  production container — no host-specific fonts or Chromium versions
  drifting between them).
- Doesn't add operational burden disproportionate to the value (we
  ship at most one PDF per paid order).

Two .NET PDF libraries were in the room at design time:

- **QuestPDF** — pure-managed C# DSL. Layout described as nested
  builder calls (`page.Header().Text(...); page.Content().Column(c => ...)`).
  No external runtime, no embedded browser. Community License free
  for companies with revenue under $1M USD/year; commercial license
  required above.
- **PuppeteerSharp** — managed wrapper over a headless Chromium
  binary. Layout described as HTML+CSS (or Razor → HTML). Faithful to
  the browser model many on the team already know. Adds ~200MB to
  the container image (Chromium binary + its own dependencies).

Story 003's text says "QuestPDF (recommended) or PuppeteerSharp",
deferring the choice to construction. The bolt's overview also
mentions both. The decision is now made.

## Decision

**`InvoicePdfRenderer` uses QuestPDF. PuppeteerSharp is not used and
will not be added in this codebase without a new ADR superseding this
one.**

Concretely:

- The PDF document tree lives as C# in
  `Services/Invoicing/InvoicePdfDocument.cs`, implementing
  `QuestPDF.Infrastructure.IDocument`. There is no Razor `.cshtml`
  intermediate.
- The QuestPDF Community License is declared at process startup:
  `QuestPDF.Settings.License = LicenseType.Community;`
- The license declaration sits in `Program.cs` next to the existing
  OpenTelemetry / Sentry bootstrap. A failed boot under a commercial
  use case will surface as a QuestPDF exception, which is the
  industry-standard way to surface this; we do not pre-empt it.
- No Chromium binary, no `node_modules`, no headless browser cache
  ever enters the production Docker image.

If a future requirement materialises that QuestPDF genuinely cannot
serve (HTML emails with the same template, complex chart embedding,
print-CSS-driven layouts authored by a third party), revisit this
decision and supersede with a new ADR.

## Rationale

The decision is driven by operational simplicity. PuppeteerSharp's
HTML+CSS model is undeniably more familiar to anyone who's done
front-end work, but the operational cost is real and recurrent:

1. **Image size**: the production container goes from ~250MB to
   ~450MB. That's a 1.8× hit on `docker pull` latency on every
   deploy, every cold-start, every CI run that builds the image.
2. **Cold-start latency**: spawning Chromium adds ~1.5–3s to the
   first PDF render after process boot. QuestPDF renders in ~50ms
   cold. For a worker that processes batches of 50 invoices on a
   30-minute timer, the cold-start hit lands on every tick after
   GC settles.
3. **Chromium version drift**: PuppeteerSharp pins a specific
   Chromium revision per package version. Patching for a security
   issue means a Chromium revision bump means re-validating PDF
   output (text-layout differences are real and have caused incidents
   in other shops).
4. **Per-host browser cache**: PuppeteerSharp's auto-download lands
   binaries in `~/.cache/puppeteer/` by default. In a multi-tenant CI
   environment or a container with read-only root, this needs
   explicit `executablePath` configuration. Another moving part.

QuestPDF's downsides are real but contained:

- The team's HTML/CSS skill doesn't transfer 1:1 to the QuestPDF DSL.
  The invoice template is one document; the cost of learning the DSL
  pays off once.
- The Community License's $1M revenue cap is a real future constraint.
  If FotoTipar exceeds that threshold, a commercial QuestPDF license
  costs ~$100/year and is the cheapest of the operational costs we'd
  be paying at that scale.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| **QuestPDF (chosen)** | No Chromium dependency; sub-100ms render; deterministic layout; ~15MB DLL; mature .NET API; Community License covers FotoTipar today. | DSL learning curve; license fee above $1M revenue (~$100/year). | — |
| **PuppeteerSharp** | Familiar HTML+CSS authoring; reuses front-end skills; Razor → HTML intermediate matches existing email-template setup. | +200MB image; Chromium version drift; cold-start latency; per-host browser cache management; security surface (Chromium CVEs). | Operational cost outweighs the authoring-familiarity benefit. |
| **iText 7 .NET** | Industry-leading PDF library; rich features. | AGPL-3.0 license requires open-sourcing the entire application or a commercial license ($$$); aggressive licensing policy historically. | License model incompatible with this codebase. |
| **PDFsharp** | MIT licensed; small footprint. | Layout API is low-level (manual positioning); no flowing document model; would require ~3× the code for the same invoice template. | Too low-level. |
| **SkiaSharp + manual layout** | Maximum control; no third-party dependency. | We'd be reimplementing layout. Multi-week effort for a one-document use case. | Out of scope. |
| **Razor → HTML → wkhtmltopdf (external binary)** | Simple HTML model; no Chromium. | Unmaintained since 2022; security CVEs unpatched; same per-host binary problem as Chromium. | Dead project. |
| **Word/OpenOffice → PDF via Aspose or similar** | Authoring in familiar tools. | Per-instance commercial license; server-side Word automation is unsupported by Microsoft. | Licensing + support model. |

## Consequences

### Positive

- **Tiny prod image.** Container stays under 300MB. CI builds and
  cold deploys stay fast.
- **Predictable render latency.** No Chromium spin-up; render
  scales with document complexity, not browser warm-up.
- **No security surface from a bundled browser.** QuestPDF's
  attack surface is its own DLL; no headless Chromium CVEs to
  track.
- **PDF output is deterministic byte-for-byte across hosts.**
  QuestPDF doesn't depend on system fonts (it bundles its own font
  loading); the same input produces the same bytes on dev and prod.
  Useful for snapshot testing.

### Negative

- **DSL learning curve.** The first contributor to modify
  `InvoicePdfDocument.cs` has to read QuestPDF's docs. Mitigation:
  the document is ~150 lines, well-commented; the DSL is shallow.
- **Razor template skill doesn't transfer.** The existing email
  templates stay in Razor; the invoice template is a separate
  authoring surface. Acceptable since the invoice template is
  rendered server-side only (no email body equivalent needed).
- **License obligation at scale.** When FotoTipar exceeds $1M
  annual revenue, a commercial QuestPDF license (~$100/year) is
  required. Operations team needs to add this to their annual
  renewal checklist. Mitigation: this ADR; a brief note in
  DEPLOYMENT.md under "License Obligations".

### Risks

- **Risk: a future contributor adds PuppeteerSharp "for HTML emails
  with attachments" without realising the duplication.** Mitigation:
  this ADR's Decision section names PuppeteerSharp as explicitly
  forbidden in this codebase. A PR adding it should require a new
  ADR superseding this one.
- **Risk: QuestPDF's licensing model changes (e.g. revenue cap
  lowered, AGPL adoption).** Quarterly review of the license terms;
  swap to PDFsharp if economics flip (the document is small enough
  that a re-implementation is bounded). Worst case: pay the
  commercial fee, which is small.
- **Risk: QuestPDF maintainership stalls.** The project is
  actively maintained as of 2026-06. If maintainership drops, the
  in-codebase document tree is portable to any layout library;
  the invoice rendering is the most isolated subsystem in the API.

## Related

- **Stories**: 003-invoice-pdf-renderer-and-endpoint.
- **Previous ADRs**: ADR-009 (Cloudflare R2) — both are
  operational-cost-driven decisions. ADR-013 (in-process token
  cache) — same "boring, well-bounded subsystem" preference.
- **Future ADRs**: any reintroduction of HTML-driven PDF rendering
  must supersede this ADR with a fresh trade-off analysis.
- **Read when**: working on `InvoicePdfRenderer` or `InvoicePdfDocument`;
  reviewing PRs that touch the PDF rendering path; reviewing PRs that
  add a new PDF use case (refund receipt, customer statement);
  evaluating PDF library alternatives; reviewing the DEPLOYMENT.md
  License Obligations section; debugging "this PDF looks different
  on prod than dev" (font drift); planning a Chromium-based feature
  (HTML email previews) that might tempt a PuppeteerSharp adoption.
