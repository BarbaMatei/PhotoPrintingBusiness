---
type: review-index
updated: 2026-07-24
---

# Review Index

One line per pass — detail lives in each pass's review/resolution/findings files; lessons and
numbers in [rationale.md](rationale.md). The old essay-style rows are in this file's git
history. Findings = new 🔴/🟠/🟡/⚪. Dormant targets live under `archive/`.

## Targets at a glance

| Target | State |
|---|---|
| 043 cloud storage | **Certified 2026-07-22** (no serious defect survives; D83–D90 → backlog) — loop complete |
| 042 thumbnail cache | Archived. Loop quiet after v9; certification never ran (pre-dates the risk tiers) |
| 035 payment idempotency | Archived. Fix loop complete at v10; never certified (pre-dates the protocol) |

## Passes

| Date | Target | Pass | Verdict | New H/M/L/C | Outcome | Files |
|---|---|---|---|---|---|---|
| 2026-07-22 | 043 | v9 certification (single-pass deviation) | approve-with-followups | 0/10/14/6 | **Certified** — 0 High, 0 regression; owner triage 2026-07-27: D83 fixed `d041295` · D84 wont-fix (EuPlatesc removal planned) · D85 → 3-env; D86–D90 backlog | [review](043-cloud-storage-provider/review-v9.md) · [resolution](043-cloud-storage-provider/resolution-v9.md) · [ledger](043-cloud-storage-provider/ledger.md) |
| 2026-07-22 | 043 | v8 verification | approve-with-followups | 0/0/0/0 | 10/10 v7 fixes non-vacuous; both deferrals upheld; 0 reopened | [review](043-cloud-storage-provider/review-v8.md) · [resolution](043-cloud-storage-provider/resolution-v7.md) |
| 2026-07-22 | 043 | v7 certification pair | request-changes | 1/11/17/5 | **Not certified**: 🔴 D49 S3-retry data loss + D50 shared-upload class → fix round (all 8 serious fixed same day) | [review](043-cloud-storage-provider/review-v7.md) · [resolution](043-cloud-storage-provider/resolution-v7.md) |
| 2026-07-22 | 043 | v6 verification | approve-with-followups | 0/0/0/0 | D36/D38 held; 12 deferrals upheld; loop quiet → certification | [review](043-cloud-storage-provider/review-v6.md) · [resolution](043-cloud-storage-provider/resolution-v5.md) |
| 2026-07-20 | 043 | v5 delta | approve-with-followups | 0/3/10/1 | v3 fix round was fix-generative (promotion sweep + lightbox clusters); D36+D38 fixed, rest → 035/backlog | [review](043-cloud-storage-provider/review-v5.md) · [resolution](043-cloud-storage-provider/resolution-v5.md) |
| 2026-07-20 | 043 | v4 verification | approve-with-followups | 0/0/1/0 | 14/14 v3 fixes held; new Low NF1 → loop not quiet | [review](043-cloud-storage-provider/review-v4.md) · [resolution](043-cloud-storage-provider/resolution-v3.md) |
| 2026-07-14 | 043 | v3 delta (+ owed frontend pass) | approve-with-followups | 0/8/10/0 | v1 fix round fix-generative; 14 fixed / 4 deferred | [review](043-cloud-storage-provider/review-v3.md) · [resolution](043-cloud-storage-provider/resolution-v3.md) |
| 2026-07-14 | 043 | v2 verification | approve-with-followups | 0/0/0/0 | 14 verified · 4 decisions upheld · 0 reopened | [review](043-cloud-storage-provider/review-v2.md) · [resolution](043-cloud-storage-provider/resolution-v1.md) |
| 2026-07-14 | 043 | v1 discovery (lean, 5 lenses) | request-changes | 1/6/11/0 | 🔴 F1 admin ZIP read local-only storage; 14 fixed / 2 wont-fix / 2 deferred | [review](043-cloud-storage-provider/review-v1.md) · [resolution](043-cloud-storage-provider/resolution-v1.md) |
| 2026-07-14 | 042 | v9 verification | approve-with-followups | 0/0/0/0 | 8 verified · 20 deferrals reaffirmed · 0 reopened; loop quiet | [review](archive/042-thumbnail-cache/review-v9.md) · [resolution](archive/042-thumbnail-cache/resolution-v8.md) |
| 2026-07-14 | 042 | v8 discovery (3rd full pass) | approve-with-followups | 0/7/17/4 | Decay 32→24→13, 15/28 re-raises; headline D85 SplitQuery mis-paging; 8 fixed, tail deferred | [review](archive/042-thumbnail-cache/review-v8.md) · [resolution](archive/042-thumbnail-cache/resolution-v8.md) |
| 2026-07-14 | 042 | v7 verification | approve-with-followups | 0/0/0/0 | 7 verified · deferrals upheld · 0 reopened | [review](archive/042-thumbnail-cache/review-v7.md) · [resolution](archive/042-thumbnail-cache/resolution-v6.md) |
| 2026-07-14 | 042 | v6 discovery (2nd full pass) | approve-with-followups | 0/8/17/4 | 24 new, mostly fix-generated residuals; 4+2 fixed, tail deferred | [review](archive/042-thumbnail-cache/review-v6.md) · [resolution](archive/042-thumbnail-cache/resolution-v6.md) |
| 2026-07-14 | 042 | v5 verification | approve-with-followups | 0/0/0/0 | all 26 v4 fixes non-vacuous; 3 follow-ups (2 doc fixed) | [review](archive/042-thumbnail-cache/review-v5.md) · [resolution](archive/042-thumbnail-cache/resolution-v5.md) |
| 2026-07-14 | 042 | v4 discovery (1st scripted full pass) | approve-with-followups | 0/11/14/7 | 110 agents / ~3.5M tok (the skeptic-calibration datapoint); 26 fixed | [review](archive/042-thumbnail-cache/review-v4.md) · [resolution](archive/042-thumbnail-cache/resolution-v4.md) |
| 2026-07-14 | 042 | v1–v3 (initial review + follow-ups) | approve-with-followups | 3/8/14/3 (v1 set) | 26 verified (v2); NEW-1/2/4 verified (v3); NEW-3 → 043 | [review](archive/042-thumbnail-cache/review-v3.md) · [resolution](archive/042-thumbnail-cache/resolution-v2.md) |
| 2026-07-04 | 035 | v8–v10 (fresh audit + verification) | approve-with-followups | 0/2/9/7 (v8) | v8 found 18 on code v7 called clean; 14 verified · 4 accepted-deferred · 0 open | [review](archive/035-payment-idempotency/review-v10.md) · [resolution](archive/035-payment-idempotency/resolution-v8.md) |
| 2026-06-19 | 035 | v1–v7 (pre-system loop) | approved (v7 — verdict later shown premature) | 15 (v1) | 13 verified · 2 deferred; the loop that taught the two-loops split ([rationale](rationale.md)) | [review](archive/035-payment-idempotency/review-v7.md) · [resolution](archive/035-payment-idempotency/resolution-v5.md) |

System improvements backlog: moved to
[self-driving-loop-design.md](self-driving-loop-design.md) (tools table + backlog).
