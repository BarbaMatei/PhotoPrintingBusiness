---
type: overlap-analysis
passes: v7 A/B
commit: 2d02b13
date: 2026-07-24
---

# Capture–recapture overlap — v7 certification pair (043-cloud-storage-provider)

Two independent blinded full-manifest passes ran on frozen `2d02b13` (2026-07-22): Pass A
(`wf_8e3b5928-a15`: correctness·security·race·db-parity·observability·requirements·quality) and
Pass B (`wf_9250d60a-e9f`: correctness·security·race·input-validation·frontend-ux·tests-coverage·
completeness-critic). The pair's overlap was never computed at the time; this note computes it from
the recorded data only (review-v7 Prov column, ledger First-seen column, metrics.jsonl per-pass item
lists — the three item-level records agree on every D#). **No item needed an "unknown" mark.**

## Who found what

| D# | Sev | Found by | | D# | Sev | Found by |
|----|-----|----------|-|----|-----|----------|
| D49 S3 retry data loss | High | B | | D66 ZIP ext from client name | Low | B |
| D50 shared-upload cross-order loss | Med | both | | D67 no file-count cap | Low | B |
| D51 slot held through backoff | Med | both | | D68 no thumb fallback | Low | B |
| D52 soft-deleted still presigned | Med | A | | D69 orders escape retention | Low | B |
| D53 webhook dup emails | Med | B | | D70 502 NFR unimplemented | Low | A |
| D54 HEIC re-advertised | Med | B | | D71 skip-reasons below log floor | Low | A |
| D55 filename overflows column | Med | B (hinted) | | D72 transient/permanent one Warning | Low | A |
| D56 audit before SaveChanges | Med | A | | D73 AsNoTracking dropped | Low | A |
| D57 cloud-off Error noise | Med | A | | D74 status set triplicated | Low | A |
| D58 retry path untested | Med | B | | D75 S3 retry/presign untested | Low | B |
| D59 webhook wiring untested | Med | B | | D76 DI/region foot-gun | Low | B |
| D60 real provider never run | Med | B | | D77 unindexed 6h scans | Low | B |
| D61 retention batch starved | Low | A | | D78 ToArray/undisposed streams | Clean | A |
| D62 ZIP mid-loop TOCTOU | Low | both | | D79 swallowed delete exception | Clean | A |
| D63 regen races retention | Low | both | | D80 Cache-Control ADR mismatch | Clean | A |
| D64 failed local delete leaks | Low | B | | D81 thumb re-read from disk | Clean | A |
| D65 root prefix match unbounded | Low | B | | D82 dual error feedback | Clean | B |

Re-raises of already-decided items (all Low; lenses were blind to the decided list — only the
post-lens dedup agent saw it, so these are legitimate independent captures):

| D# | Found by | | D# | Found by | | D# | Found by |
|----|----------|-|----|----------|-|----|----------|
| D9 webhook race | A | | D20 migration parity | both | | D40 refresh-guard untested | B |
| D14 regen fake-stream | B | | D27 purge/promo orphan | A | | D42 reload-during-heal | B |
| D17 purge-on-cancel docs | A | | D35 sweep dedup gap | both | | D43 guest-401 blank body | B |

Record notes: (1) the ledger's `v7 · A/B` on D49 is the pair label, not dual attribution — review-v7
("Pass B's High … absent from A") and metrics (pass A `high:0`, pass B `high:1`) both place it B-only.
(2) Review-v7's heading says 10 re-raises, its frontmatter says 9: metrics record 11 raise-events
(A:5 + B:6) over **9 distinct** decided items, D20 and D35 raised by both — the table above carries
the 9. (3) metrics.jsonl's per-pass severity tallies don't reconcile with their own item lists
(e.g. pass B `low:5` vs 9 lows listed); the item lists were used since they match ledger and review.

## The numbers — Chapman: N̂ = (N_A+1)(N_B+1)/(M+1) − 1

| Stratum | Found | N_A | N_B | M | N̂ | Still hidden | SE |
|---|---|---|---|---|---|---|---|
| **Serious (High+Med), new** | **12** | **5** | **9** | **2** | **19** | **≈7** | ±5.9 |
| Minor (Low+Clean), new | 22 | 12 | 12 | 2 | 55 | ≈33 | ±21.7 |
| All new (D49–D82) | 34 | 17 | 21 | 4 | 78 | ≈44 | ±24.2 |
| Sensitivity: minor + re-raises | 31 | 17 | 18 | 4 | 67 | ≈36 | ±20.4 |
| Sensitivity: all + re-raises | 43 | 22 | 27 | 6 | 91 | ≈48 | ±23.5 |

- Overlap is tiny: 4 of 34 new findings (12%); in the serious stratum 2 of 12 (17%).
- **D55 is flagged `hinted`** (planted by shared prompt context). It was single-pass anyway, so it
  contributes nothing to M; dropping it entirely moves the serious estimate 19 → 17 (hidden ≈6).
- All 9 re-raises are Lows, so the sensitivity run does not touch the serious stratum at all; it
  nudges the minor/combined estimates up and confirms the picture (M rises to 6 via D20/D35).
- The pair together found an estimated 12/19 ≈ 63% of the findable serious population.

## If only one pass had run

- **A alone** misses all 17 B-only items — above all **D49, the silent-data-loss High** (a retried
  S3 upload re-sends a spent stream; the customer's paid original is deleted after a truncated
  upload "succeeds"), plus 6 more serious: D53 (duplicate confirmation emails), D54 (HEIC
  regression), D55 (prod-only 500), D58/D59/D60 (the coverage gaps). 7 of 12 serious lost,
  including the single worst defect of the whole review.
- **B alone** misses all 13 A-only items — 3 serious: D52 (customers served broken thumbnails for
  soft-deleted photos), D56 (false audit records), D57 (chronic false-Error log noise) — plus 6
  Lows and 4 Cleanups. No High lost.
- The *verdict* was robust: either pass alone found serious defects, so "NOT CERTIFIED" stands
  either way. The *fix set* was not: which serious bugs got fixed depended heavily on which pass
  ran, and the High sat in the non-shared part.
- Note: D49 is a correctness find, and correctness ran in **both** manifests — pass A's correctness
  lens simply didn't look inside the S3 retry. The disjointness is not just the deliberately
  different lens rosters; identical lenses also miss things.

## Caveats

- **Shared model.** Both passes run the same underlying model, so blind spots are correlated: what
  neither can see never enters M or the estimate. That inflates apparent overlap relative to true
  independence, so **N̂ is a lower bound on the findable population** — at least ~7 serious were
  still hidden at 2d02b13, possibly more.
- **Different lens rosters** (4 of 7 lenses differ) push the other way: some disjointness is by
  design, which deflates M and inflates N̂. The two biases don't cancel exactly; treat the point
  estimates as rough.
- **M is tiny** (2 in the serious stratum), so the estimates are unstable (see SE). The robust
  facts are qualitative: tiny overlap, two large disjoint tails, population not exhausted.
- Out-of-sample corroboration: the later single full pass (v9, on the *fixed* tree `ac97e42`)
  still surfaced 8 genuinely new items (3 Medium) — consistent with a real residual population,
  though not directly comparable since the fix round changed the code.

## Conclusion (plain language)

Two reviewers examined the same frozen code. Together they found 34 new problems, 12 of them
serious. They agreed on only 4 (2 serious). When two searchers overlap that little, the search is
far from complete: the standard estimate says roughly 19 serious problems were findable, so about
7 serious ones were still hidden even after both passes — and because both reviewers share the same
brain (one model), that is a floor, not a ceiling.

If only one reviewer had run, the outcome would have depended on luck. Reviewer A alone would have
missed the worst bug in the feature — customers' paid photos silently destroyed — plus six other
serious problems. Reviewer B alone would have missed three serious problems. Either way the code
would eventually have been declared clean while known-findable serious bugs remained in it.

**Recommendation: risk-conditional.** For features that can destroy data, money, or access (this
storage feature is exactly that), run the pair — here it earned its ~4M-token cost by catching a
data-loss High that a single pass had roughly even odds of missing. For low-risk work, a single
full pass is a reasonable economy. A single pass on a high-risk feature is defensible only right
after a pair has just audited the same area (the v9 pattern). And never read one quiet pass as
proof of absence: at v7, each pass alone looked "thorough" at 25–28 findings while missing over
a third of what its twin could see.
