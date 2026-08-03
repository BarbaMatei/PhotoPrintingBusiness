---
type: system-scorecard
status: active — canonical rubric for all re-grades
created: 2026-08-03
owner: Matei Barba
---

# Review-system scorecard — the 11 dimensions and every grade ever given

The fixed rubric the system is graded on. Dimensions 1–10 were locked by
[review-v1.md](review-v1.md) (2026-07-29) — re-grades use **these same dimensions, never
revised ones**. Dimension 11 (Speed) was added 2026-08-03 by owner decision, with its
baseline graded from measured data the same day. Re-grades **append dated columns** to the
grades table below, every cell carrying evidence; past columns are never edited.

## Grades

| # | Dimension | Baseline | Evidence |
|---|---|---|---|
| 1 | Bug-finding power | 6 | [review-v1](review-v1.md) 2026-07-29 |
| 2 | False-alarm control | 8 | [review-v1](review-v1.md) 2026-07-29 |
| 3 | Severity judgment | 5 | [review-v1](review-v1.md) 2026-07-29 |
| 4 | Fix verification | 8 | [review-v1](review-v1.md) 2026-07-29 |
| 5 | Reviewer independence | 3 | [review-v1](review-v1.md) 2026-07-29 |
| 6 | Cost efficiency | 5 | [review-v1](review-v1.md) 2026-07-29 |
| 7 | Autonomy | 3 | [review-v1](review-v1.md) 2026-07-29 |
| 8 | Self-measurement | 7 | [review-v1](review-v1.md) 2026-07-29 |
| 9 | Rule discipline | 6 | [review-v1](review-v1.md) 2026-07-29 |
| 10 | Proven outcomes | 1 | [review-v1](review-v1.md) 2026-07-29 (no data possible yet) |
| 11 | **Speed** | **2** | this file, 2026-08-03 (below) |

## Dimension 11 — Speed

**What it grades:** the wall-clock efficiency of the loop's work, measured — never
estimated — from the `runtime` fields metrics schema v3 records (worklog-backed). The
headline number is the **median active minutes per serious finding fixed, over the last
two metered fix rounds**; blocked-on-owner share and idle share qualify the grade.

| Grade | Anchor |
|---|---|
| 10 | ≤5 active min per serious fix · blocked <10% of round span · idle ≈ 0 between round-start and hand-back |
| 8 | ≤8 active min per serious fix |
| 6 | ≤12 |
| 4 | ≤18 |
| 2 | ≥25, **or runtime unmeasured** |

**Baseline 2/10 (2026-08-03), evidence:**

- The 044-045-v1 fix round ran at **~25 min per serious finding**: git timestamps show
  ~6h+ of session time for 17 of 23 serious findings across 07-31 and 08-03 (first fix
  commit `44c3e2d` 07-31 12:52 → `ba23e3a` 08-03 15:39), with 6 still open. Earlier
  rounds ran 4–6 min/finding (043-v7: ~10 findings 15:30–16:06 on 07-22; 015-v5:
  ~35 findings in ~3.5h across 07-28/29).
- The round's own records name the sinks: approach-checks run serially before each
  mechanism cluster at ~95k/~95k/~154k tokens against a ~20k design
  ([resolution-v1](../044-045-observability/resolution-v1.md), clusters A/B/C), plus
  per-finding red/green test invocations and ~400 lines of hand-written bookkeeping.
- **No runtime was measured anywhere** — schema v2 metered pass tokens only and fix
  rounds not at all — which alone caps the grade at 2 by the anchor above.

**Re-grade protocol:** after two fix rounds metered under the v2 fixer contract (or when
the 044-045 loop closes, whichever comes first), re-grade Speed **and** the dimensions the
2026-08-03 redesign touches — cost efficiency (6), autonomy (7), self-measurement (8) —
against these baselines. Each re-grade is a new dated column here, its cells linking the
metrics lines and records that justify them.
