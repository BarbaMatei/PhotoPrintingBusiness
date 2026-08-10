---
type: findings
target: 044-045-observability
version: 6
for: review-v6.md
---

# Findings — v6 (verification of the v5 fix round)

One finding. Full verdicts on the four verified fixes are in [review-v6.md](review-v6.md); this
file carries the detail for what the pass newly names.

## F1 ⚪ — the guarded-selector list is hand-maintained (D124)

**Where.** `src/PhotoPrint.Tests/Integration/DashboardMetricNamesTests.cs:29-35` —
`GuardedSuccessSelectors`, four literal strings.

**What is true today.** Those four *are* every hand-named literal-value selector in `slos.md` and
`ops/dashboards/fototipar-overview.json`; the pass enumerated both files. Each is required to appear
in both copies and to carry `or vector(0)` within 24 characters, and both halves were measured red
(review-v6, M1 and M2).

**The gap.** A fifth ratio of the same shape — say a future `email_send_total{result="ok"}` numerator
— ships unpinned. The general rule that would cover it (`An_added_sum_term_always_carries_an_absent_series_guard`)
cannot see it, because a lone `sum()` is one term; that is exactly D121, fixed here for four
instances rather than for the class.

**The fixer disclosed this** ([resolution-v5.md](resolution-v5.md#why-f1-pins-four-selectors-by-name-instead-of-stating-the-general-rule))
and gave a reason for not writing the class rule: it would red SLO 1's `!~"5.."` numerator and the
error-rate panel's `=~"5.."` numerator. **That reason does not hold for a narrower rule.** Both of
those numerators use a *negative* or *regex* matcher; a rule keyed on a **literal `=` matcher on the
numerator side of a division** matches the four pinned selectors and neither of the two panels the
fixer wanted to leave alone. Checked against every query in both files.

**Why it is still ⚪ and not 🟡.** Writing that rule is not free: the division split has to be
brace-aware, because a label value can contain `/` (SLO 2's `http_route="api/payments/stripe/intent"`),
while the matcher detection has to happen *before* brace groups are stripped — the two existing
walkers do these in the opposite order. Today's coverage is complete and measured; this is a
maintenance cost, not a defect. It becomes real the day someone adds a fifth guarded ratio.

**Failure scenario.** A later bolt adds a success-ratio panel with a hand-named value, forgets
`or vector(0)`, and the suite stays green; the panel reads "No Data" instead of a red 0% in exactly
the case an operator needs it — a fresh process where every attempt is failing.

**Suggested action.** Backlog. Either write the literal-matcher rule when a fifth selector appears,
or leave the list and accept that adding a guarded ratio means adding a line here.
