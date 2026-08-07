# loop-driver — iteration 1 benchmark (hand-aggregated; no Python on this machine)

All 6 runs on Opus. 3 evals × (with_skill, without_skill). Assertions graded against
report.md + git-status tree checks; grading.json per run carries the evidence.

| Config | Pass rate | Mean tokens | Mean duration |
|---|---|---|---|
| with_skill | **13/13 (100%)** | **39,510** (36.3k / 39.6k / 42.6k) | **204 s** |
| without_skill | 12/13 (92.3%) | 53,239 (49.5k / 53.6k / 56.7k) | 280 s |
| **delta** | **+7.7 pts** | **−25.8%** | **−27%** |

Per eval:

| Eval | with_skill | without_skill |
|---|---|---|
| 1 quiet-gate-stop | 5/5 | 4/5 — pre-answered the delta-worthiness judgment it admitted it couldn't verify ("On the record, no... patch-grade"), then offered options |
| 2 fixround-handoff | 4/4 | 4/4 |
| 3 closed-terminal | 4/4 | 4/4 |

## Analyst notes

- **The one discriminating assertion is the point of the skill**: gate discipline. The
  baseline answered an owner judgment call itself before handing it back; the skill run
  relayed it without leaning. n=1, but it is exactly the failure mode the skill encodes
  against ("never pre-answer a gate").
- **Both arms were handed the script paths in the prompt** — a generous baseline. Users who
  say only "continue the loop for X" would leave the baseline without the router; the gap
  here understates the skill's value in that case (and makes description triggering matter).
- **Consistent efficiency win**: with-skill runs ~26% cheaper and ~27% faster across all
  three evals — the skill skips the exploratory reading (e3 baseline: 20 tool calls vs 14).
- **Eval 2 does not discriminate** (identical outcomes). For iteration 2, replace with a
  rotten-records case (auditor-red fixture) to test "fix the records first", a behavior only
  the skill mandates.
- Harness note: the Write tool refused one baseline's outputs path; the agent fell back to a
  Bash heredoc. Report present; no impact.
