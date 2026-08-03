# FotoTipar — photo-printing e-commerce (Romanian market)

ASP.NET Core 8 API (`src/PhotoPrint.API`) + Angular 21 SPA (`src/PhotoPrint.UI`) +
xUnit tests (`src/PhotoPrint.Tests`). UI strings are Romanian.

## Constraints that bite when unknown

- **Dual database.** SQLite in dev (via `EnsureCreated` — migrations never run in dev),
  PostgreSQL 16 in prod (migrations at boot), EF InMemory as the integration-test default.
  Nothing currently proves migrations/SQL against Postgres. Details + test implications:
  `memory-bank/standards/data-stack.md` — read it before touching entities, queries, or
  migrations.
- **Two-tier storage.** Every upload read/write/delete must route via
  `IStorageRouter.For(upload.StorageLocation)` — never assume local disk. See
  system-architecture.md (storage section) and ADR-007/008/011.
- **Frontend is Angular 21, standalone, zoneless, Vitest.** No NgModules, no Karma, no zone.js
  (OnPush/signals or change detection misses), **no ESLint** (Prettier only). Folder:
  `src/PhotoPrint.UI`.
- **No refresh-token flow in the SPA**; 401 = logout or clear-guest-token. Guest state lives in
  localStorage (`guestSession`) and is merge-preserved — changes here are the most re-found
  defect cluster in review history (definition-of-done class 11).
- **No optimistic concurrency anywhere** — unique indexes + violation-detection are the
  concurrency mechanism (payment idempotency lives on `Orders.IdempotencyKey`).

## Commands

- API tests: `dotnet test src/PhotoPrint.Tests` (MinIO S3 suite auto-skips without
  `STORAGE_TEST_*` env vars) · run API: `dotnet run --project src/PhotoPrint.API`
- UI (from `src/PhotoPrint.UI`): `npm test -- --watch=false` · `npm start` · `npm run build`

## Running tests (hard rule — full runs overload this machine)

A full test run saturates this machine and blocks everything else on it. **Never run the
whole suite by default.**

- Run only what the change can affect, in this order: tests you added or modified → tests
  covering the code you modified (same feature/namespace/component) → nothing more.
- Untouched side = untouched tests: a frontend-only change never triggers `dotnet test`; a
  backend-only change never triggers `npm test`. Tests for functionality nobody touched
  don't run at all.
- Scope with:
  - API: `dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~<Namespace.OrClass>"`
    (namespaces mirror folders, e.g. `PhotoPrint.Tests.Unit.Controllers`)
  - UI: `npm test -- --watch=false --include='**/<name>*.spec.ts'`
- A full run is a **last resort**, only when genuinely unavoidable (cross-cutting change to
  shared infrastructure whose blast radius can't be traced, or the user explicitly asks).
  Even then, run it in **sequential batches** — API by namespace
  (`…~PhotoPrint.Tests.Unit.<Area>`, then `…~PhotoPrint.Tests.Integration`), UI by feature
  folder via `--include` — one batch at a time, never both suites at once, never batches in
  parallel.

## The map (read-when routing)

| Working on… | Read first |
|---|---|
| Any construction bolt | `memory-bank/standards/bolt-process.md` (stages, gates, required reading) |
| Any code before hand-back | `memory-bank/standards/definition-of-done.md` (the defect-class checklist) |
| Design decisions | `memory-bank/standards/decision-index.md` — scan the "Read when" lines |
| DB / entities / migrations | `memory-bank/standards/data-stack.md` |
| Architecture (storage, jobs, auth, payments) | `memory-bank/standards/system-architecture.md` |
| API shapes, errors, auth headers | `memory-bank/standards/api-conventions.md` |
| Naming, testing, logging rules | `memory-bank/standards/coding-standards.md` |
| Reviewing / fixing review findings | `reviews/README.md` (the review loop — say "continue the review loop for `<target>`") |

## Hard rules (any entry point, not just bolts)

- Apply `definition-of-done.md` before declaring work done; regression tests must fail when
  the fix is reverted.
- Mock only at system boundaries in tests — a mocked-out real component proving "green" is the
  most expensive defect class in this repo's history.
- **Comments are a last resort, kept to one short line.** Never add a comment to narrate a
  change, a bug fix, or a feature. Only two reasons justify one: (a) to explain *why*
  non-obvious code exists — state the constraint or gotcha itself, with **no reference** to the
  bolt, review, finding/decision ID (`F3`, `D50`, `BUG-2`, `SEC-1`…), ADR, ticket, PR, or past
  discussion where it was decided (that history lives in commits/resolution files); (b) a short
  behaviour description on an **interface** member (`///`, JSDoc) — never on concrete classes.
  When you edit a file, delete non-essential comments you pass through.
- Never edit `reviews/**/review-v*.md` (immutable) — fixers respond in resolution files.
- Standards are **descriptive**: if you change reality (a version, a tool, a contract), update
  the standard that states it in the same change.
- Commits: conventional style, **exactly one sentence, subject line only** — no body and no
  trailers (no `Co-Authored-By`). Sole exception: a breaking change may carry a body.
  Reference bolt/finding IDs in the subject where relevant, e.g.
  `fix(orders): guard duplicate AWB creation (D45, review 015-v3)`.

## Response style (chat replies to the user — not code, commits, or docs)

When these rules collide: reporting problems > keeping facts > plain words > brevity.

- Lead with the result — or the question, if you're blocked — in 1–3 plain
  sentences. Failures, skipped work, risks, and judgment calls you made
  belong in those sentences; bad news is never shortened away or buried.
- Whole reply under ~10 short lines by default. Longer is fine when the
  question needs it or the deliverable is long (a plan, a review, a report
  on many changes). Get short by cutting words and decoration — never
  numbers, paths, error text, or caveats a decision could turn on.
- Plain words a non-programmer could follow. Name files, tools, tests, and
  errors exactly; gloss any other technical term in a few plain words on
  first use. Never swap a precise term for a vaguer one.
- No filler: preamble, restating the request, closing recaps, decorative
  headers or bold labels in short replies, play-by-play of tool calls.
- Work reports: one line per meaningful change plus how it was verified.
  No pasted code unless asked; short exact quotes of errors or commands
  are fine.
