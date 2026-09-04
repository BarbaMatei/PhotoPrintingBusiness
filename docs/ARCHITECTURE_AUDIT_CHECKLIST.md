# Architecture Audit Checklist

**Cadence**: quarterly — end of March, June, September and December, the same dates as the
invoice audit in section 6. **Effort**: about an hour. **Who**: whoever owns the codebase that
quarter. **Output**: one row appended to the run log at the bottom of this file.

Nothing reminds you. A checklist with no reminder is a checklist that runs once: put the four
dates in a calendar or a recurring issue the first time you read this.

The point is not to fix things during the audit. It is to *notice* them: a CVE nobody saw, a
package a major version behind, a file that quietly grew to 900 lines, a decision made in code
with no record, a standards doc that has started to lie. Anything found either gets fixed there
and then if it is a five-minute job, or written into the run-log row so it is owed rather than
forgotten.

Every step below states a command or a procedure, and what counts as a bad answer. The **Baseline** column
holds the answer measured on 2026-09-04 at `main` = `182cd50`, so the first real run has
something to compare against rather than a blank page.

---

## 1. Vulnerabilities

| Step | Command / how | Bad answer |
|---|---|---|
| 1.1 | `dotnet list package --vulnerable --include-transitive` (from the repo root, covers both projects) | any row at all; a Critical or High row is a stop-everything |
| 1.2 | `npm audit --omit=dev` in `src/PhotoPrint.UI` | any High or Critical; a Moderate in something that reaches the browser bundle |
| 1.3 | Open the Renovate dependency dashboard issue and read what it has been unable to merge | an open security update older than one cadence |
| 1.4 | Confirm the secret scan is still running: last successful `secret-scan.yml` run on `main` | no run in the last month, or a run with findings nobody triaged |

**Baseline**: **none — this section has never been run.** Both audit commands restore packages
from the network, which the pass that wrote this page did not do. The first real run measures
the baseline; until then treat every threshold above as un-anchored. Bolt 054's dependency sweep
covers the same ground and may land first, in which case take its numbers as the baseline and
record where they came from.

**Note on 1.3**: the Renovate configuration arrives with bolt 054. If the dashboard issue does
not exist yet, that is itself the finding — record it and check whether the configuration
landed.

---

## 2. Outdated packages

| Step | Command / how | Bad answer |
|---|---|---|
| 2.1 | `dotnet list package --outdated` | anything a full major behind; anything whose transitive graph is held back by one stale direct reference; anything two or more majors **ahead** of the project's `TargetFramework` |
| 2.2 | `npm outdated` in `src/PhotoPrint.UI` | Angular packages not all on the same minor; TypeScript ahead of what the installed Angular supports |
| 2.3 | Look for pre-release pins and ask whether a stable line exists yet | a `-beta` / `-preview` pin that could now be stable |
| 2.4 | Check that every pinned version actually resolves to that version | a pin that was never published: NuGet substitutes the next one up and the build stays green while running something else |
| 2.5 | Check runtime vs development placement in `package.json` | a types-only or tooling package sitting in `dependencies` |

**Baseline (2026-09-04)**:

- 27 direct NuGet references in `PhotoPrint.API`, 13 in `PhotoPrint.Tests`; 13 runtime and 7
  development npm packages.
- Two OpenTelemetry packages are pinned to `-beta.1` builds — the Prometheus exporter and the
  Entity Framework instrumentation. No stable line has ever been published for either, so this
  is not drift.
- `@types/leaflet` sits in `dependencies` rather than `devDependencies`. Harmless (types are
  compile-time only) but it is a step-2.5 row that has not been cleared.
- Three `Microsoft.Extensions.Configuration*` packages in the test project are pinned at
  `10.0.8` on a `net8.0` target — two majors ahead, a step-2.1 row nobody has cleared.
- Step 2.4 has one known instance: a Stripe.net version was pinned that had never been
  published, and the build silently ran the next one up. Bolt 054 turns that substitution into a
  build error, so after it merges this step is enforced mechanically and only needs re-reading
  when the build starts failing on it.

---

## 3. Size and growth

Run from the repo root:

```sh
# API source, excluding generated migrations and build output
find src/PhotoPrint.API -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' \
  -not -path '*/Migrations/*' | wc -l
find src/PhotoPrint.API -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' \
  -not -path '*/Migrations/*' -exec cat {} + | wc -l

# Tests
find src/PhotoPrint.Tests -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' \
  -exec cat {} + | wc -l

# Migrations. One migration emits three files: the migration, its .Designer.cs and the
# model snapshot. Count timestamped migrations, not files.
ls src/PhotoPrint.API/Migrations/ | grep -c '^[0-9]\{14\}_.*\.cs$'

# Frontend, source and specs separately
find src/PhotoPrint.UI/src -name '*.ts' -not -name '*.spec.ts' -exec cat {} + | wc -l
find src/PhotoPrint.UI/src -name '*.spec.ts' -exec cat {} + | wc -l

# The ten largest source files. The `grep -v ' total$'` matters: xargs splits the list into
# batches and wc prints a subtotal per batch, which would otherwise take the top rows.
find src/PhotoPrint.API src/PhotoPrint.UI/src -name '*.cs' -o -name '*.ts' \
  | grep -v -e '/obj/' -e '/bin/' -e '/Migrations/' \
  | xargs wc -l | grep -v ' total$' | sort -rn | head -10
```

| Measure | Baseline (2026-09-04) | Bad answer |
|---|---|---|
| API source | 334 files, 20,361 lines | growth over a quarter with no matching test growth |
| API migrations | 1 timestamped migration (3 files, 3,036 lines) | more than **one** timestamped migration while nothing is deployed — the chain is a single baseline edited in place |
| Tests | 165 files, 32,591 lines | the test-to-source ratio falling below about 1.2× |
| Test-to-API-source ratio | 1.60× | see above |
| Frontend source | 100 files, 10,706 lines | — |
| Frontend specs | 50 files, 6,756 lines | fewer spec files than a third of the source files |
| Largest single file | `home-page.ts` 951 · `InvoiceUploadJob.cs` 615 — both already past the line | any file past ~600 lines that is not generated |
| Next three | `delivery-step.ts` 567 · `OrderService.cs` 565 · `order-detail-page.ts` 518 | three or more files crossing 600 in one quarter |

Growth itself is not a defect. A quarter where the API grew and the tests did not is the signal.

Two files breach the threshold at baseline, and neither is generated. They are recorded in the
run log as owed, not fixed here — this pass changed no code.

---

## 4. Decisions and their records

| Step | How | Bad answer |
|---|---|---|
| 4.1 | `find memory-bank/bolts -name 'adr-0*.md' \| wc -l` against `total_decisions` in `memory-bank/standards/decision-index.md` | the two disagree |
| 4.2 | Every ADR file has an index entry, and every entry has a "Read when" line | an ADR nobody can find from the index |
| 4.3 | Read the `status:` of each ADR against what the code now does | an ADR still `accepted` whose mechanism has been replaced — the index then teaches the wrong thing |
| 4.4 | Ask what was decided this quarter without an ADR | a new mechanism (cache, limiter, retry, queue, event) with no record |

**Baseline (2026-09-04)**: 24 ADR files, `total_decisions: 24` — matching. Two known 4.3 rows
are open: ADR-015 carries an amendment because its original stance did not hold, and ADR-023's
summary still credits compare-and-swap for the invoice-upload safety property that the
`Invoices.ClaimedAt` lease now provides. Both are recorded against the invoicing review target rather than fixed
here. `docs/architecture/multi-replica-readiness.md` states the current mechanism, so a reader
who starts there is not misled.

---

## 5. Doc rot

| Step | How | Bad answer |
|---|---|---|
| 5.1 | For each file in `memory-bank/standards/`, compare its "Rewritten \<date\>" or "Verified \<date\>" header with `git log -1 --format=%ad --date=short -- <file>` | a doc whose content changed long after its header claims it was written from the code; a doc with no date header at all, which cannot be checked |
| 5.2 | Pick three claims per standards doc at random and check them against the manifests and the code | one wrong claim means read the whole doc |
| 5.3 | Ask which features shipped this quarter, and whether each one's standard was updated in the same change | a shipped feature nowhere in `system-architecture.md` — the standards are descriptive, so this is a rule break, not a nicety |
| 5.4 | `docs/KNOWN_FAILURES.md` against the gates actually in the test project | a register entry for a gate that no longer exists, or a gate with no entry |
| 5.5 | Follow every relative link in the files touched this quarter | a link to a moved or deleted file |

**Baseline (2026-09-04)** — last commit touching each standard, against the date its own text
claims it was written from the code:

| File | Last touched | Header claims | Gap |
|---|---|---|---|
| `api-conventions.md` | 2026-07-27 | no date header | cannot be checked |
| `bolt-process.md` | 2026-09-02 | no date header | n/a — describes process, not code |
| `coding-standards.md` | 2026-09-02 | 2026-07-14 | 7 weeks |
| `data-stack.md` | 2026-09-02 | 2026-08-20 | 13 days |
| `decision-index.md` | 2026-09-02 | `last_updated: 2026-06-03` | **3 months** |
| `definition-of-done.md` | 2026-09-02 | no date header | n/a |
| `system-architecture.md` | 2026-09-02 | 2026-07-14 | 7 weeks |
| `tech-stack.md` | this commit | 2026-09-04 | none |
| `ux-guide.md` | 2026-05-27 | no date header | cannot be checked |

Four 5.1 rows are open at baseline: three standards carry no date header at all, so step 5.1
cannot be run against them, and `decision-index.md`'s `last_updated` frontmatter is three months
behind its last edit. Two 5.3 rows are open: `system-architecture.md` was never updated for the
invoicing feature, and `ux-guide.md` has not been read against the frontend since May. None is
closed here — `decision-index.md` in particular is edited by other work in flight.

---

## 6. Rituals owned elsewhere

Quarterly work that belongs to a feature rather than to the architecture, listed here so it has
a cadence:

- **Invoice-number gap audit** — [`DEPLOYMENT.md` §15.8](DEPLOYMENT.md), due at the end of
  March, June, September and December. Postgres sequences advance on rollback by design, so a
  burned number leaves a gap the Romanian Fiscal Code does not allow, and the accountant needs
  the list per fiscal period. Run the query from there rather than copying it here — one query
  in two places is how a query goes stale. Known caveat while reading its output: the query
  extracts the year in the session time zone, while the unique index it is effectively checking
  extracts it `AT TIME ZONE 'UTC'`, so an invoice issued either side of midnight UTC can be
  attributed to the wrong year by one of the two. Recorded against the invoicing review target.

---

## Run log

One row per audit. Keep the findings terse; the detail belongs wherever the work is tracked.

| Date | Ran by | Found | Fixed now | Left owed |
|---|---|---|---|---|
| 2026-09-04 | baseline, not a full run | section 1 not measured (both commands need the network); 2.1 (three config packages two majors ahead of `net8.0`), 2.4, 2.5; 3 — `home-page.ts` 951 and `InvoiceUploadJob.cs` 615 both past the 600-line line; 4.3 ×2 (ADR-015 amended, ADR-023's summary superseded); 5.1 ×4 (three standards with no date header, `decision-index.md` `last_updated` 3 months stale); 5.3 ×2 (`system-architecture.md` missing invoicing, `ux-guide.md` unread since May) | none — this pass wrote the checklist and changed no code | every row above, plus a real first run of section 1 and a calendar reminder for the next one |
