---
type: review
target: 043-cloud-storage-provider
version: 9
supersedes: 8
commit: ac97e42
branch: feat/bolt-043-cloud-storage-provider
pass-type: discovery
subtype: certification-single
date: 2026-07-22
reviewer: "certification — single blinded full-manifest pass (owner-approved deviation from the two-pass protocol; see README calibration)"
verifies: null
verdict: approve-with-followups
certified: serious-clean
blockers: []
findings: { high: 0, medium: 10, low: 14, cleanup: 6, refuted: 3 }
tests: { dotnet: "719/719 (+10 skipped MinIO)", frontend: "439/439" }
---

# Review v9 — 043-cloud-storage-provider (certification, single pass)

The certification gate, run as a **single blinded full-manifest pass** (all 11 lenses) on the frozen
post-fix commit `ac97e42` — the owner-approved deviation from the two-parallel-pass protocol,
justified because the v7 pair audited this same feature hours earlier and the v7→v8 fix round moved
only 8 files, all independently verified (README, *The pair is not sacred*). 45 agents, 2.87M tok,
31 raw → **30 canonical**.

**Verdict: `approve-with-followups` — CERTIFIED CLEAN OF SERIOUS DEFECTS.** **0 High, 0 fix-caused
regressions.** The one High that ever existed (D49) is fixed + verified; this independent
full-manifest look found no new High and nothing the v7 fixes broke. Per the severity-based stop
rule, the loop does **not** re-arm: the serious-defect population is closed and confirmed closed.

**What this verdict does and does not claim.** It claims: no serious defect (data loss, security,
broken core promise) survives, on the evidence of a full-manifest pass plus the v7 pair on
near-identical code — all three High-clean. It does **not** claim zero defects remain: this single
pass surfaced 10 Mediums (mostly known/accepted/folded) and a Low/Cleanup tail, and a second
independent pass would likely find a *different* handful of Mediums (the medium population is not
saturated — the bolt-035 lesson). The stop rule deliberately does not gate closure on Mediums; they
are follow-ups, not blockers.

## The 10 Mediums — none reopens the loop

| # | D# | Disposition |
|---|----|-------------|
| Shared-upload purge guard misses AwaitingPayment sibling (conv 2) | D50 residual | **Accepted (owner)** — the exact residual recorded in resolution-v7; corner-case double-checkout flow, not currently reachable in the UI. Independently re-found → corroborates it's real; decision stands. |
| Admin ZIP races concurrent promotion → truncated archive | **D62** (widened) | Same class as the backlogged D62 (ZIP mid-loop `GetStream` TOCTOU), new trigger (promotion Local→Cloud+delete vs purge). Admin-only, narrow window. **Backlog.** |
| Promotion worker no in-flight dedup → double promotion | D35 / cluster A | The deferred concurrency work already owned by **bolt-035** (with D9/D27). Framed via the worker; same root. **Deferred.** |
| /photos returns 403 not 404 for non-owner | D10 (re-raise) | **Wont-fix upheld** (403 = codebase convention). *But* the finder is right that 053's implementation-plan AC says 404 — a **doc-vs-code discrepancy** to reconcile (owner's eye). |
| "Photos no longer available" shown before promotion / for pending orders | **D83** (new) | Genuine UX medium: a just-paid customer sees "gone" for the seconds before the in-process promotion runs. The concrete form of the known D13 four-way-empty follow-up (needs an order-status/backend-state gate). **Fixable now or backlog — owner's call.** |
| EuPlatesc IPN → promotion enqueue never asserted | **D84** (new) | Coverage sibling of the D59 Stripe test I added — the EuPlatesc twin path is untested. Cheap test. **Fixable now or backlog.** |
| Backfill CLI omitted from the review manifest (plausible) | **D85** (new) | Review-scope gap: `BackfillCommand.cs` wasn't in `changedFiles`, so it got lighter scrutiny; backfill×live-worker concurrent promotion is also untested. Low-risk (ops CLI). **Backlog; noted as a confidence caveat for that file.** |
| New sweep queries InMemory-only (plausible) | D20 class | Only `ArchiveRetentionJob` got the SQLite parity smoke; `OriginalPurgeRecoveryScanner`'s sweep didn't. Same DB-1/3-env remit. **Deferred (D20).** |
| No lens owns steady-state S3 cost / poison-order amplification | D46/D51 | Completeness-critic flag; overlaps the deferred D46 (terminal re-enqueue) + fixed D51 (retry amplification). **Noted; deferred.** |
| Frontend manifest path wrong (photo-thumbnail) | — | **Refuted** by skeptic (the file was reviewed). Dropped. |

## Low (14) & Cleanup (6) — backlog

New Lows worth naming: retention deletes blobs before persisting the key-null (broken-URL window,
`ArchiveRetentionJob:146` → **D86**); retention sweep query omits the `DeletedAt` filter → reprocesses
deleted rows + false audit (`:96` → **D87**, a mini class-sweep miss of the D52/D56 area); promoter
tests assert cloud keys but never the bytes written (**D88**); D36 close-*during*-refresh resolve-time
re-read is unverified (**D90**, coverage of the async branch I fixed). Re-raises upheld: D42, D69, D74,
D75, D80. Refuted: ImageProcessor fail-open (fail-closed confirmed), MimeValidator partial-read.

**Cleanup — D89 (new, codebase-wide):** fix comments cite finding/decision/ADR IDs
(`(D50, review 043-v7)`), violating the CLAUDE.md hard rule — **67 occurrences across 27 files**,
mostly pre-existing from earlier bolts; this session's fixes added to the pattern. A dedicated
comment-hygiene sweep, not a per-file scramble. **Backlog.**

## Loop state — the review is complete

043's serious-defect population is **closed and independently confirmed closed** (v7 caught the
High + data-loss class; v7→v8 fixed and verified all of it; v9 found no new serious defect). Under
the severity-based stop rule, **no further discovery or certification pass is warranted** — a second
pass would re-mine Mediums, not change the serious verdict. What remains is a triage decision on the
Mediums/Lows (D83–D90 + the backlog), which do not gate closure. This is the honest end state:
**certified clean of serious defects; a triaged follow-up list of minor items remains.**

Provenance: single pass `wf_f9c87958-fa1`. Frozen `ac97e42`; suites **.NET 719/719 · FE 439/439**.
