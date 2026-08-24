---
type: resolution
target: 038-039-invoicing
version: 12
answers: review-v12.md
status: in-progress
fixed_commit: ed3ce30
---

# Resolution v12 — 038-039-invoicing

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-579 | fixed | `8e71c63`, `ed3ce30` | runtime stage installs `icu-libs` + `icu-data-full`, its `ENV` turns the base image's invariant flag off, and the API runtimeconfig pins it off, which outranks the variable. Culture stays `ro-RO`. New surface: the image's ICU packages |
| PPW-584 | deferred | — | not addressed this round; the owner scoped the round to PPW-579. Ledger row stays open at 🔴 — the double-charge path is the loudest thing still standing |
| PPW-580 | wont-fix | — | owner ruled 2026-08-22; reaching it needs the tax authority to keep erroring on `stareMesaj` until 50 rows are stuck, and he accepts that risk |
| PPW-581 | wont-fix | — | owner ruled 2026-08-22; reaching it needs revoked or expired ANAF credentials, and he accepts that risk |
| PPW-582 | deferred | — | not addressed this round; the owner scoped the round to PPW-579. Ledger row stays open at 🔴 |
| PPW-583 | deferred | — | not addressed this round; the owner scoped the round to PPW-579. Ledger row stays open at 🔴 |
| PPW-586 | deferred | — | owner regraded 🔴 to 🟠 on 2026-08-22; not addressed this round, ledger row stays open at medium |
| PPW-585 | deferred | — | owner regraded 🔴 to 🟠 on 2026-08-22; not addressed this round, ledger row stays open at medium |
| PPW-489 | wont-fix | — | the earlier owner ruling stands; v12 raised it again and this round did not revisit it |
| PPW-524 | deferred | — | the earlier deferral stands; v12 raised it again and this round did not revisit it |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — production image globalization | PPW-579 | `Dockerfile`, `src/PhotoPrint.API/PhotoPrint.API.csproj`, `src/PhotoPrint.Tests/Unit/Services/Invoicing/InvoicePdfCultureTests.cs`, `docs/DEPLOYMENT.md` | not needed (the v12 pre-check classified it not trigger-list-shaped: a base-image and configuration change plus a renderer test) |
| B — untouched this round | PPW-580, PPW-581, PPW-582, PPW-583, PPW-584, PPW-585, PPW-586, PPW-489, PPW-524 | — | not needed (no code changed) |

## Decisions

### ICU goes into the image; the invoice keeps its Romanian culture (PPW-579)

The owner's ruling was that production must be able to render Romanian invoices, not that the
culture should be dropped. So the renderer is untouched and the deployed image changed.

- The runtime stage installs `icu-libs` **and** `icu-data-full`. Alpine's `icu-libs` pulls only
  the English data set, and a Romanian locale that falls back to root prints `1,234.56` on a
  fiscal invoice with no error anywhere — a silently wrong invoice is worse than the crash.
- Invariant mode is turned off twice: the stage's `ENV` sets
  `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false`, and the API project sets
  `InvariantGlobalization=false`. Both, because a compose `environment:` block or the server
  `.env` overrides an image `ENV`, while the runtimeconfig switch the project property emits
  outranks the variable.
- That precedence was measured, not assumed. A probe app carrying the property resolved `ro-RO`
  and printed `1.234,56` with `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` set; with the property
  removed and the same variable it threw `CultureNotFoundException` — "ro-ro is an invalid
  culture identifier". The built `PhotoPrint.API.runtimeconfig.json` now carries
  `"System.Globalization.Invariant": false`.

### What the tests prove, and what nothing here proves (PPW-579)

The fix lives in the deployed image, so two of the three tests are a deployment contract: one
reads the built `PhotoPrint.API.runtimeconfig.json` beside the test binaries, one reads the
Dockerfile's runtime stage with comments dropped and continuation lines joined, so a reflow
cannot slip past it. The third reads the renderer's culture field by reflection.

- Revert-and-rerun: with `Dockerfile` and the project file back at `8e71c63~1`, those two tests
  failed, 2 of 3. Restored: 10 of 10 across the `InvoicePdf` tests.
- The third is a canary, not a red leg — it passes on any host with ICU, however the image is
  configured. It reddens where it matters: under `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` it
  fails with `TypeInitializationException` → `CultureNotFoundException`, the production failure
  itself, and it also reddens if the renderer is switched to invariant formatting. The micro-review
  found it passing in both states, which is why the tests were retargeted before hand-back.
- **The image was not built.** There is no Docker on this machine, so the two package names are
  not proven to install. If either is wrong the deploy workflow's image build fails loudly.

### The class sweep found one site (PPW-579)

`src/PhotoPrint.API` was swept for `CultureInfo`, `GetCultureInfo`, `new CultureInfo(`,
`CreateSpecificCulture`, hard-coded culture names, `TimeZoneInfo`, `IdnMapping` and
`CompareInfo`. `Services/Invoicing/InvoicePdfDocument.cs:19` is the only site that needs a real
culture. Every other formatting call passes `CultureInfo.InvariantCulture` on purpose —
`Controllers/WebhooksController.cs:200`, `Services/EuPlatescService.cs:26` and `:120`, and six
sites in `Services/Invoicing/InvoiceXmlBuilder.cs` — which is correct there: the UBL XML and the
processor signatures are machine-read and must not carry Romanian separators.

There is no `TimeZoneInfo` or local-time use anywhere in the API, so `tzdata` was left out of the
image. Neither compose file, nor `.env.example`, nor any workflow sets the globalization variable,
so nothing else re-enables invariant mode today.

### The nine rows this round did not change (PPW-580 … PPW-524)

- PPW-580 and PPW-581 are `wont-fix` on the owner's ruling of 2026-08-22. What it costs if that
  call is wrong: PPW-580 leaves newly paid invoices unfiled past the five-day deadline while
  stuck polls hold the whole batch, and PPW-581 leaves a credential outage paging nobody — it
  shows only generic per-row failures, so nobody learns that filing has stopped.
- PPW-585 and PPW-586 were regraded 🔴 to 🟠 on 2026-08-22 after the driver checked them with the
  owner. Both stay open at medium.
- PPW-582, PPW-583 and PPW-584 are deliberately left for a later round: the owner scoped this
  round to PPW-579. PPW-584 is the double-charge path.
- PPW-489 and PPW-524 carry earlier owner decisions that v12 raised again; neither was revisited.
- Their rows above read `deferred` because that is the only legal status for "not addressed in
  this round". Their **ledger rows stay `open`**, so the next discovery pass does not read them
  as decided. The resolution stays `in-progress` for the same reason.
