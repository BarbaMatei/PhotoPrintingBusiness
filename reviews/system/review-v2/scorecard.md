---
type: system-scorecard
version: 2
date: 2026-08-12
graded-by: blinded re-grade — grader saw v1's dimension names only, no v1 grades or findings
dimensions-from: v1 scorecard, extracted by a grade-stripping agent
owner: Matei Barba
---

# Review-system scorecard — v2 grades (2026-08-12)

Grades were set fresh from the live system before the v2 findings were checker-verified,
then two were lowered when independent checkers surfaced defects the first read missed
(marked ↓). Evidence links go to the v2 review file's findings and to the metrics readout.

| Dimension | v2 grade | Evidence |
|---|---|---|
| Bug-finding power | 7/10 | 125 serious findings named across 5 targets through 2026-07-29; manifest breadth proven by seeded run 1 (2 of 10 seeds caught only by the db-parity and completeness lenses); true recall still unmeasured — run 2 deferred |
| False-alarm control | 8/10 | Trace-first skeptics, dedup-before-verify, `hinted` marking; 6 of 234 fixes reopened (2.6%); refuted findings recorded with reasons |
| Severity judgment | 5/10 | Measured once (run 1: erred high ~4 of 10, never low — the safe direction); `sev_delta` designed to accumulate calibration data but the whole history holds 1 value |
| Fix verification | 7/10 | Revert-and-rerun rule; 15 verification passes; 035 loop had 0 reopens ever; but fully manual, and 044-045 closed with 2 🟠 at `fixed` never `verified` |
| Reviewer independence | 3/10 ↓ | Blinding is prompts-only and the prompt bars only `reviews/` — the runbook's claimed git-history bar does not exist in the lens prompt while fix commits carry `PPW-<n>` ids; workspace auditor unbuilt; one model family everywhere including the gate judge |
| Cost efficiency | 5/10 | Real measured cuts (skeptic tiers, Sonnet lows, delta caps, ~⅓ fewer skeptics via trace-first); but ~37M recorded tokens over 5 targets, and the owner-facing cost estimates are stale (see Speed/self-measurement rows) |
| Autonomy | 5/10 | Mechanical router + loop-driver + designed gates operate; session-bound, nothing re-invokes the loop, verification manual, no delegated-decision policy |
| Self-measurement | 5/10 ↓ | Metrics v3 enforced at append, but: fix-generativity structurally uncapturable where it surfaces; `findings[]` written on 1 of 41 pass lines; renderer and auditor disagree on the `backlog` bucket; the router never surfaces round-keyed corrections; fix-round tokens null on all 5 recorded rounds |
| Rule discipline | 5/10 ↓ | Rule budget honored for calibrations, but the normative texts disagree with each other and with the code in 5 verified places (fix-round index rows, frontmatter findings map, `closed:` key, rendered table, stale comments/dates) — the repo's own "standards are descriptive" rule broken inside the review system |
| Proven outcomes | 3/10 | Undeviated close path: 0 completions; seeded run 2 deferred; 2 certifications under watch with 0 escapes but low statistical power; new quantified fact: no target ever reached zero new serious on a broad pass before close |
| Speed | 3/10 | The system's own v3 metric: 6.0 h active vs 114.8 h idle across 044-045's five fix rounds (95% nobody-at-the-wheel); descheduled fixer contract new, one target of data; nothing re-invokes the loop between sessions |

**Overall: ~5.1/10.** Mechanics (find, record, route) remain the strength; trust
(independence, proven outcomes) and unattended operation remain the gap; the new
drop this round is internal consistency — the machinery's texts and scripts drifted
apart during the 2026-08 redesign wave.
