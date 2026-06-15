# Cross-System Critical Review v3 — Bug-Hunter v3.3 × Knowledge-Builder v3.2 × Integration Contract v1.2

> **Status: APPLIED (2026-06-12).** All 13 findings landed: bug-hunter **v3.4**, integration
> contract **v1.3**, knowledge-builder **v3.3**. **I3 was resolved by owner decision B** (stricter
> than the proposed fix): `closed-unverified` fixes produce **no oracle entry of any kind** — not
> even a queue-only candidate; the blind spot is accepted and recorded in contract §4 as a
> deliberate decision. Findings I1–I13 continue the F → G → H sequence; none required structural
> change. Several findings are residuals of an H-round fix that landed as policy but did not carry
> through to the runtime/co-residence concern it introduced — those name the H they extend.

*Review date: 2026-06-12. Documents reviewed in full (current spec-of-record, not archive):*

- `docs/agent-systems/archive/bug-hunter-build-guide-v3.3.md` (BH)
- `docs/agent-systems/archive/knowledge-builder-build-guide-v3.2.md` (KB)
- `docs/agent-systems/archive/integration-contract-v1.2.md` (IC)

*Method: a close read of all three documents produced an initial candidate set; an independent
fan-out of 5 parallel dimension reviewers (security/adversarial threat model, concurrency &
git-model, cross-system loop & interface, operational/lifecycle, internal consistency) — each
re-reading all three documents — then surfaced 11 candidates, independently re-discovering 5 of the
initial candidates (corroboration). Every candidate was adversarially verified line-by-line against
the cited text by an independent checker instructed to refute it and to default to "already
addressed"; **11 survived, 0 were refuted** (several were severity-trimmed during verification — see
the verification notes). Two further findings from the close read that the fan-out did not surface
(I12, I13) were verified directly against the text and are included as low severity. Applied findings
F1–F23, G1–G16, and H1–H35 are not re-reported.*

---

## Overall verdict

The H-round fixes genuinely landed, and most landed well. This round found **no re-litigation** of an
already-closed finding — the verification pass rejected nothing as invented or previously covered.
Verified still-true strengths (each re-checked against the current text):

- the injection convention (G1/H5) is now a full carrier — candidate shape, per-hunter instantiation,
  Verifier, and a poison fixture (BH 201–206, 214–220, 527–531, 636, 1006–1010);
- secret **and** PII redaction (G2/H6/H13) happen at candidate emission and re-render only downstream
  (BH 221–231, 438–443);
- the run lock (H3) is created/removed by each orchestrator with a stale-lock rule (BH 547–550;
  KB 655–659);
- `tamper_warning` (H4) now reaches the envelope and weakens corroboration (IC 87–90; BH 925);
- the write audit (H12), publish-commit + restore path (H16), `closed-unverified` terminal (H19),
  `correlation_id` allocation rule (H15), single-history stores (H2), and the run-open mailbox scan
  built by the new Prompt 31b (H1) all landed where they were assigned;
- the two-axis lifecycle, firewall ratification chain, twin-name discipline (H8), and the §7
  build interleave (no dependency cycle) are intact.

**The dominant pattern in this round:** H2/H3/H12/H16 collapsed the three stores onto a *single git
history* in *one designated integration worktree on `main`*, and gave each orchestrator a per-store
run lock, a whole-tree write audit, and a publish-commit. Those fixes addressed the **git-history**
hazard of parallel branches — but they did not address the **runtime co-residence** hazard they
introduced: two systems, each with its own lock, are now invited (IC §7, lines 182/184) to run *at
the same time in the same working directory*, sharing one `.git` index, one working tree, and one
`code-index/`. Nothing serializes them, and the very safety mechanisms H12/H16 added become
self-defeating under that overlap (I1, I6, I7). The second pattern repeats the H-round's own lesson —
a *measured* behavior whose grader can't actually measure it (I9, the BH analogue of the gap H5 fixed
for the schema) — and the third is a pair of fix-loop **state-machine** imprecisions that strand a
real fix forever (I2) or skip the loop's headline output for a whole bug class (I3).

---

## High severity

### I1 — H2/H3/H12 left a runtime hole: the two systems share one worktree with no cross-system mutex, and each orchestrator's whole-tree write audit will falsely abort the other

H2 put both stores on one history in one integration worktree; H3 gave each orchestrator its **own**
per-store lock (`bug-hunting/.run-lock`, BH 547; `knowledge/.run-lock`, KB 655); H12 gave each a
run-close audit that diffs the **whole tree** and fails "if anything outside the allowed write set
changed" (BH 562–565 — set excludes `knowledge/**`; KB 668–670 — set excludes `bug-hunting/**`).
Nothing serializes the two systems against each other — IC §1 (44–54) says runs happen "only in the
designated integration worktree on `main`" but never *one at a time*, and IC §7 actively invites
overlap ("KB Phases 1–2 may run parallel with bug-hunter bolts 089/090", line 182; "Bug-hunter bolts
092–093 (parallel with KB Phases 3 and 5)", line 184). So when a KB run is mid-flight as a BH run
reaches Close, `git status` shows dirty `knowledge/**` files — outside BH's allowed set — and the BH
run aborts though it wrote nothing illegal; symmetrically for KB. The H12 tamper gate fires a
**guaranteed false positive on the documented happy path**, and the single-active-run invariant H2
intended is unenforced.

**Fix:** (1) **Cross-system mutex (IC §1, normative):** the integration worktree admits at most one
active run across BOTH systems — at Open each orchestrator checks for the *other* system's `.run-lock`
(same stale-lock rule) and refuses/queues if it is present and fresh; the two locks together act as
one mutex. Mirror one line into BH Prompt 7 Open, KB Prompt 8 Open, and note it in Prompt 31b's Open
extension; update the §8 consumer rows (201–202). (2) **Scope each write-audit to its own store:**
change BH Close (562) and KB Publish (668) from "`git status`/diff the tree" to diff only the run's
own store paths (`-- bug-hunting/` / `-- knowledge/` + the shared code index), so a sibling's
in-flight files can never trip it. Apply both — the mutex restores the invariant, the scoped audit is
the root-cause fix for the false abort (and the other system's own close-audit still covers its store,
so H12 coverage across the pair is preserved).

### I2 — H1's run-open scan can never re-check a `fix-failed` record, contradicting Prompt 31b's own claim that it "stays eligible"

The H1 mailbox scan — the *only* mechanism that notices a fix — selects records with
`fix_status: open | fix-reported`, stated identically in IC §4 (131–132) and BH Prompt 31b
(1118–1119). But Prompt 31b then asserts the opposite for the failure state: "Skip records already
terminal (`verified-fixed`, `closed-unverified`); **a `fix-failed` record stays eligible so a re-fix
is re-checked**" (1124–1125). `fix-failed` is neither `open` nor `fix-reported`, so it falls outside
the inclusion predicate and is never revisited; no other mechanism resets it (the only re-emission
path, BH 1151–1154 / IC 117–119, mints a *fresh* id for a `Reopened` regression and explicitly does
not reset the existing record). The §4 prose compounds it by defining eligibility through *exclusion*
("`closed-unverified` … excluded from the run-open scan", 124) — implying `fix-failed` is in — while
the binding *inclusion* predicate omits it. Net effect: when AI-DLC re-fixes a bug whose first fix
failed its proving test, the new `bolt.md: complete` is never noticed, the record parks at
`fix-failed` forever, `verified-fixed` is never written, and KB re-distillation (IC 137–138) stalls
**silently and permanently** for that `correlation_id`. Extends H1 (the scan) and H19 (which added the
terminal state but left the non-terminal predicate too narrow).

**Fix:** expand the inclusion predicate to `fix_status: open | fix-reported | fix-failed` in both
binding locations (IC §4 line 132; BH Prompt 31b 1118–1119); keep terminal =
`verified-fixed | closed-unverified` (1124). Optionally tidy the §4 enum comment (122/124) so the
exclusion wording names only the two genuinely terminal states. Add the missing BH 31b test: a
re-completed `fix-failed` bolt is re-discovered at run open and moves
`fix-failed → fix-reported → verified-fixed`. (Preferred over having `fix-verification` reset
`fix-failed → open`, which risks aliasing two fix cycles.)

### I3 — Statically-confirmed fixes (`closed-unverified`) never produce a negative-invariant contract, so the loop's own "highest value" output is silently skipped for an entire bug class

KB itself names the bug-fix negative invariant the **"Highest value"** oracle entry, the one that
"pairs with the harvested regression test" (KB contract-kinds table, line 307). Re-distillation
requires `verified-fixed` (IC 137–138; KB Prompt 18b line 931, "re-distil verified fixes only"), and
the firewall structurally rejects a bug-derived invariant lacking the full chain — "the same entry
missing `verified-fixed` → held" (KB 593–594, 604–605). But `fix-verification`'s documented fallback
for a bug confirmed only *statically* (no runnable proving test) is to "mark the closure 'unverified'
and write the terminal `fix_status: closed-unverified`" (BH 1103–1105), on which KB explicitly **does
not re-distil** (IC 124–126). So a genuine, fixed defect never becomes a regression-guard contract.
This is systematic, not an edge case: the concurrency-auditor produces statically-confirmed bugs *by
design* — "hard to confirm by execution, so the Verifier will usually mark them Medium confidence
('reasoned, not reproduced')" (BH 861–862) — so every concurrency bug that gets fixed exits via
`closed-unverified`. With no governing contract, a later run's `intent-lookup` returns nothing for
that location, the finding is no longer high-confidence-on-contradiction (BH 917–920), and the
regression guard the whole loop exists to create is absent for the bug class most likely to regress
subtly. Extends H19 (which introduced `closed-unverified` as mailbox hygiene without noticing the
oracle-coverage loss). *(Verification trimmed the finding's "re-reported forever" phrasing — the bug
is not lost; static hunters can still re-find it at Low/Medium — but confirmed the core: the
highest-value contract is never minted for this class.)*

**Fix:** when a fix-request reaches `closed-unverified` AND the bug was Confirmed with a recorded
human triage action, route its negative invariant into the **existing inferred-intent queue** (KB
Prompt 15, 821–824) — queue-only, `verification: not-checked`, ratification at most `inherited`, never
auto-activated, tagged "fix-unverified, no proving test". This deliberately fails the firewall's
auto-admit rule (KB 594), so it can become a contract only via explicit human ratification through
`approval-intake` (Prompt 16). Add the clause to IC §4 (after the `closed-unverified` definition) and
to KB Prompt 18b / `correlation-tracking`; add a KB test: a `closed-unverified` fix with a triage
action → a queue-only inferred-intent candidate (not a contract); without one → nothing queued. This
reuses two already-planned seams and preserves the "no proof, no auto-contract" invariant.

---

## Medium severity

### I4 — Only the *sandbox* is network/secret-locked; the hunting host that makes live advisory calls runs against a secret-bearing checkout with unrestricted egress (extends H14)

H14 hardened the **sandbox** (dummy creds, repo+fixtures mount, "nothing worth stealing", BH 261–265),
and the broader network lockdown — "Lock down the sandbox's outbound network … never load real
production data" (BH 253) — is sandbox-scoped throughout. But `dependency-audit` and `config-auditor`
do **not** run in the sandbox: the cost-control ordering runs deterministic tools "before the LLM
hunters, and only spin a sandbox for candidates that survive cheaper checks" (BH 946), so the Hunt
stage executes in the hunting environment, which IC §1 pins to the integration worktree on `main`.
There, `dependency-audit` makes **live** outbound calls ("a **live** vulnerability source (OSV, GitHub
Advisory) … query at run time", BH 819–820) and `config-auditor` runs gitleaks/checkov/tfsec
(841–842) over a real e-commerce checkout whose committed secrets are its quarry (839). So the
privileged hunting process simultaneously has (a) read access to live secrets/PII and (b) unrestricted
egress — exactly the exfiltration channel the sandbox lockdown closes, left open one layer up. The
injection convention defends against *obeying* a poisoned advisory; it does not constrain egress or
secret-read posture.

**Fix:** add a short "Hunting-environment posture" convention near BH 214–236, cross-referenced from
the Operating runbook (269–279) and Prompts 20/21: (1) **clean-checkout default** — hunts run against
a checkout that carries no live production secrets in the working tree (`config-auditor` still finds
*committed* secrets in tracked files, its job); (2) **egress allowlist** — the only component needing
network is `dependency-audit` (plus the H35 registry cross-check); restrict the hunting process's
egress to exactly those advisory/registry endpoints, no general egress; (3) fallback — if a hunt must
run on a checkout that does hold real secrets, the sandbox's "nothing worth stealing" + locked-egress
discipline applies to the host too.

### I5 — External scanners are trusted host *executables* run with full privileges against the live checkout, but nothing pins or integrity-checks them — only their *output* is treated as untrusted (extends H35/H14)

The docs treat scanner **output** as data-never-instructions (BH 607–608, 823–825) and cross-check
advisory remediation **data** (H35, BH 825–828). But the scanner binaries/packages themselves — `npm
audit`/`pip-audit` "via tool-ingest" (820), gitleaks/hadolint/checkov/tfsec (842) — run with the
hunting process's host permissions against the secret-bearing checkout, with no provenance, version
pin, or integrity check anywhere (grep across all three docs for pin/integrity/provenance/lockfile
returns only ledger/map integrity, never the scanner toolchain). A compromised or typo-squatted
scanner, or an `npm audit` that resolves and runs package lifecycle scripts, executes arbitrary code
with full privileges — directly undercutting the H14 "nothing worth stealing" posture one layer up.

**Fix:** add one convention near BH 214–220, referenced from Prompts 9/20/21 and "Fixed assets you
maintain" (277–279): "a deterministic scanner is trusted *code* we run with host privileges — pin and
integrity-check it; only its output is untrusted data." Concretely: install the scanners once into a
version-pinned, checksum-verified hunting toolchain/image rather than resolving them ad hoc at run
time; invoke audit tools with install/lifecycle-script execution disabled and egress restricted to the
allowlisted advisory endpoints (I4); list the pinned toolchain as a maintained fixed asset. The
advisory data stays live; the executable producing it becomes maintained, trusted code.

### I6 — The shared `code-index` "conflicts resolved by regeneration" covers git merges, not the live read-during-rewrite race two concurrent runs create in one worktree (extends H20/H27)

H20 placed the index under `bug-hunting/code-index/` as "the one sanctioned dual-writer; conflicts
resolved by regeneration, never merge" (IC 36) — a clause about a **git** merge between branches. But
under IC §7's sanctioned parallel operation in one worktree (and the per-system locks of I1), a BH
reader (`find_symbol`/`slice_around`/`search_text`, BH 708) can hit the index at the instant a
concurrent KB run (`current-state-description`/`drift-reconciliation`) triggers the incremental
re-index that "either system's run may refresh" (IC 36), rewriting those files in place ("re-index
only changed files when given a SHA", BH 710). That is a filesystem read-during-rewrite race, not a
git conflict — a reader can observe a half-regenerated index (torn symbol tables) and silently corrupt
reachability, taint, and oracle-anchoring results, with no detectable error. "Resolved by
regeneration" presumes regeneration is the only write event and that nobody reads mid-regeneration;
neither holds when both systems run concurrently. The atomic-publish discipline H27 defined for
ledgers was never extended to this derived store.

**Fix:** apply the `ledger-io` publish discipline (the versioned-filename + pointer-file pattern, BH
409–411) to the index — regenerate into a temp location, publish by pointer swap, never rewrite in
place — so a concurrent reader always resolves a complete index; stamp the index with `built_at_commit`
(mirroring `app-mapping`'s §3 freshness stamp) so staleness is detectable. Add one line to IC §1's
code-index row: "refresh is atomic — published via pointer swap, never in-place; readers always see a
complete index." (Lock-free, reuses an established pattern; complementary to the I1 mutex.)

### I7 — `publish = commit` (H16) does `git add`/commit of a sub-tree in a shared worktree with no path-scoping or serialization, so the "commit IS the restore point" invariant can capture a torn or interleaved state (extends H16/H2)

H16's owner decision A makes "every publish … followed by a git commit of the published store … the
commit IS the restore point" (IC 154–158; BH 564; KB 669), with rollback = "restore to
`ledger_version` N−1". That guarantee assumes each publish maps to exactly one clean commit of exactly
that store. But two orchestrators committing in the same `.git` are not isolated: §5 mandates no
`git add -- <path>` discipline and no serialization of the shared index. A BH `git commit` while a KB
run has staged or dirtied `knowledge/**` can sweep the sibling's partial state into BH's commit, or
race the index lock, or capture a `knowledge-ledger.json` mid-rename — so "restore to N−1" may land on
a commit interleaving two systems' half-written stores, a state that never cleanly existed. The H12
write-audit is a partial backstop against the *silent* variant, but its only recourse is the spurious
abort of I1, not correctness.

**Fix:** (1) make the publish-commit **path-scoped** — "commit only the publishing system's own store:
`git add -- bug-hunting/` or `git add -- knowledge/`, never `git add -A`/`-a`" (IC §5; mirror into BH
Prompt 7 line 564 and KB Prompt 8 line 669) so a stray sibling change can never enter the wrong commit
and N−1 always holds one system's clean publish; (2) **serialize** the two commit windows — tie both
orchestrators' commit step to the I1 cross-system mutex, or have each acquire a single shared
`.publish-lock` for the git-commit window only; (3) note in §5 that the write-audit remains the
detection backstop, not serialization.

### I8 — There is no oracle-coverage signal, so a mid-backfill oracle (`as_of_commit == HEAD`, but only N of 35 intents distilled) reads as "no governing contract" everywhere undistilled, with zero operator warning

Staleness is defined exclusively as `as_of_commit` trailing HEAD — "stale = more than 20 commits or 14
days behind HEAD" (IC §5, 146–147) — and the BH orchestrator warns only on that (24d, 933–934). But KB
backfill is explicitly multi-session and chunked — "at 35 intents plan on chunking across several
sessions (default 10/pass) … an abandoned half-backfill is a permanently stale oracle" (KB 288–290) —
and per §7 the bug-hunter reaches oracle grounding (bolt 091) right when KB has finished only Phase 2.
During that entire bootstrap window the ledger can be published with `as_of_commit == HEAD` (so **not
stale** by the only defined test) while 25 of 35 intents have no contracts at all. For those areas
`intent-lookup` returns empty (BH 887–899) — indistinguishable from "genuinely no governing contract"
— so every logic finding is silently downgraded to `intent-unconfirmed` (BH 242, 919–920) and the
entire contract-contradiction channel (Prompt 24b) is silently inert, with no operator signal that the
oracle is merely *incomplete* rather than authoritatively silent. The system advertised as telling "a
real spec violation from the model's opinion" quietly behaves as if there are no specs. The §2
envelope (87–90) carries `as_of_commit`/versions/staleness/tamper but **no coverage field**; KB's
health report shows coverage but is KB-side and a Phase-3 deliverable not yet built at bolt 091.

**Fix:** add an envelope-level coverage signal to IC §2 — e.g.
`oracle_coverage: {intents_distilled, intents_total, backfill_complete}` — populated by `ledger-query`
(KB Prompt 6) from the KB's existing `coverage` section (349) and `runs.mode` (351), mirroring the
precedented §3 `flow-map-unavailable` present-vs-absent distinction. In BH Prompt 24d's Map step, emit
a run-open warning when `backfill_complete` is false (or coverage is below a configurable fraction):
"oracle backfill incomplete (N/M intents); contract-grounding unavailable for undistilled areas".
Optionally tag findings `intent-unconfirmed: oracle-incomplete` vs `: no-contract`. All additive; add
the §8 consumer note since §2 changed.

### I9 — The BH eval cannot measure the injection convention its poison fixture claims to grade — the hit matcher and `eval-metrics` ignore `injection_suspected` and the not-obeyed condition (the BH analogue of the gap H5 fixed)

`eval-corpus` declares a three-part expected result for its poison fixture — "found and reported,
instruction not obeyed, `injection_suspected` flagged … so the injection convention is *measured*, not
assumed" (BH 1006–1010). But the only grading machinery is the hit matcher = "signature/location
proximity + category match" (1004), and `eval-metrics` scores solely TP/FN/FP through it (1021–1024).
Neither inspects the `injection_suspected` flag on the reported record nor checks that the embedded
suppression instruction was disobeyed — so only condition 1 (bug found at the right place) is scored;
the actual injection-resistance behavior is asserted, not measured. A regression where a hunter starts
obeying suppression comments would pass the eval silently as long as the bug is still located. The
asymmetry is decisive: the KB twin genuinely scores "firewall leak rate (poison reaching the intent
side — target zero, injection included)" as a graded metric with regression tests (KB 973–974, 983–984)
— BH has the equivalent fixture but no equivalent metric. The same dead-ends the H5 carrier's last
leg: `report-rendering` (Prompt 4) is never told to surface the flag either, so both terminal
consumers drop it. Extends H5 (which added the schema slot but not the eval/report consumers the
convention's own see-list promises, BH 217).

**Fix:** (1) Prompt 27 — give each adversarial-content fixture an `expected_disposition` beyond the hit
matcher (`reported == true` AND `injection_suspected == true` on the matched record; evidence the
suppression instruction was not obeyed where checkable). (2) Prompt 28 — add a scored
"injection-resistance" outcome per fixture (the BH analogue of KB's firewall-leak-rate, target zero),
recorded in the per-run metrics/trend; tests mirroring KB 20(a)/(b): a clean run flags the fixture; a
run that obeys the comment is scored as a regression. (3) Prompt 4 — surface the `injection_suspected`
flag on any finding rendered from a record carrying it, realizing the record→report leg of the
declared hunter→Verifier→record→report/eval carrier.

---

## Low severity / polish

### I10 — `fix-request-emit`'s idempotency ("update this *bug*'s request") collides in wording with the never-reuse-on-`Reopened` rule, though the behavior is derivable (extends H15)

Prompt 33 keys idempotency on the bug — "if a fix-request for **this bug** already exists, update
rather than duplicate" (1161–1162; test b, 1166–1167) — while the H15 allocation rule keys freshness
on the cycle — "never reused even on re-emission after a `Reopened` regression — a re-emission gets a
fresh id" (1151–1154; IC 117–119). For a reopened bug both read as true. The operative behavior *is*
determinable (the adjacent allocation rule is the tiebreaker: a fresh `correlation_id` is a fresh
record key, so the idempotency check — read against the key — cannot match the prior terminal record),
so this is a drafting-precision gap plus a missing linkage, not undefined behavior. **Fix:** reword the
idempotency clause to key on `correlation_id` (not "this bug"): "idempotent on `correlation_id` — a
re-emission after a `Reopened` regression mints a fresh id per the allocation rule and therefore writes
a **new** record (it must not overwrite the prior cycle's terminal `verified-fixed`/`closed-unverified`
record); link the new record to the prior via `related`." Add `related` to the documented fix-request
fields and a test for the reopen case.

### I11 — `fix-verification` guards that the sandbox builds HEAD, but not that the *fixing commit* is present at HEAD before writing `fix-failed`

"Fix done" is purely `bolt.md: complete` (IC §4 line 120; BH 1096–1097); on that signal the proving
test runs "against the current commit" = integration HEAD (1101), guarded only by sandbox-vs-commit
("confirm the container builds the commit under analysis", 257–258) — which proves the recipe is
fresh, not that HEAD contains the merged fix for this `correlation_id`. `bolt.md` lives in
`memory-bank/**` (writer AI-DLC) while the fix lives in application source (a different store/writer,
IC 32/35), and nothing pins the two to arrive at HEAD together. If the frontmatter is visible at HEAD
before the source fix merges, the test fails and writes `fix-failed`. *(Verification trimmed this:
once I2 is fixed, `fix-failed` is re-checkable, so the false verdict self-corrects next run and the
bug stays `Confirmed` meanwhile — hence low, cheap hardening, not a sticky failure.)* **Fix:** add a
one-line precondition to Prompt 31 — before treating a proving-test failure as `fix-failed`, use
`git-revision-tracking` to confirm the bolt's completion commit is an ancestor of the
commit-under-analysis; if not yet reachable, leave the record at `fix-reported` ("fix not yet present
at HEAD — merge pending") so the run-open scan re-picks it. Reserve `fix-failed` for a fix that *is*
present and still fails. Optionally note in IC §4 that "fix done" = `bolt.md: complete` AND the bolt's
commit reachable from the run's commit.

### I12 — The `pre-merge` run trigger contradicts the single-worktree-on-`main` rule (extends H2/H17)

H17 added trigger wiring and both IC §5 (line 152, "bolt-completion step, weekly batch, or pre-merge")
and the BH Operating runbook (269–272, "a pre-merge hook") offer **pre-merge** as an option. But H2's
single-history rule (IC §1, 44–46) makes every worktree *except* the designated integration one
read-only on the stores. A pre-merge hook fires on a feature-branch worktree, where the ledger,
coverage, and the I2/H1 mailbox writes cannot be persisted without violating that rule. The two
statements are never reconciled. **Fix:** in IC §5 and the BH runbook, either drop `pre-merge` as a
trigger, or define it explicitly as a **read-only advisory** run (no ledger/coverage/mailbox writes —
findings reported to the PR only), or require it to run against the branch but commit through the
integration worktree, and state which.

### I13 — Ledger rollback (H16) has no cross-system consumer invalidation

H16's recovery is single-system: "restore to `ledger_version` N−1" (IC §5). A bug-hunter run that
already consumed the rolled-back oracle version — recording its `as_of_commit` and raising confidence
on a since-withdrawn contract (BH 891, 917–925) — is not re-evaluated; future BH runs self-heal
against the corrected oracle, but emitted reports remain grounded in a version that was withdrawn.
`tamper_warning` covers out-of-band edits, not a legitimate rollback. Marginal (reports are
point-in-time), but undocumented. **Fix:** one acknowledging sentence in IC §5, or have rollback bump
a recovery epoch consumers can compare against their recorded `as_of_commit` to flag potentially
affected prior findings.

---

## Verification notes (for transparency)

The adversarial pass **refuted nothing** this round — no candidate was found to be already-addressed
or invented — but it materially sharpened several findings, recorded so the next round inherits the
honest version:

1. **I1** was argued down from a clear-cut high toward "high defensible, medium reasonable": it defeats
   a safety mechanism and fires on the documented parallel path, but it aborts at Close *before*
   publish/commit, so the worst outcome is a failed/rolled-back run and operator confusion, **not
   silent data corruption**. Kept high because a self-defeating safety gate on the happy path is a real
   reliability defect.
2. **I3**'s original "the fixed bug is re-reported forever" was trimmed — the bug is not lost (static
   hunters still find it at Low/Medium); the confirmed harm is the **missing highest-value contract**
   for the class, which is why it stays high.
3. **I10** was reduced medium → low: the colliding rules have a derivable tiebreaker (the adjacent
   allocation rule), so the gap is wording precision + a missing `related` link, not undetermined
   behavior.
4. **I11** had one sub-claim rejected: its harm originally leaned on "the false `fix-failed` is then
   never re-checked", which is only true while **I2** is unfixed; on its own merits it is a transient,
   self-correcting skew — low-severity hardening.

I12 and I13 did not pass through the dimension fan-out; they come from the close read and were verified
directly against the cited lines.

---

## Summary table

| ID | Severity | Finding | Where the fix lands |
|---|---|---|---|
| I1 | High | No cross-system run mutex; whole-tree write audits falsely abort each other (extends H2/H3/H12) | IC §1 + BH Prompt 7 + KB Prompt 8 |
| I2 | High | Run-open scan predicate can never re-check `fix-failed`, vs 31b's own "stays eligible" (extends H1/H19) | IC §4 + BH Prompt 31b |
| I3 | High | `closed-unverified` fixes never mint the negative-invariant contract for a whole bug class (extends H19) | IC §4 + KB Prompts 15/18b |
| I4 | Medium | Hunting host has secret-read + unrestricted egress; only the sandbox is locked (extends H14) | BH conventions + sandbox + Prompts 20/21 |
| I5 | Medium | External scanners run as unpinned privileged host executables (extends H35/H14) | BH conventions + Prompts 9/20/21 |
| I6 | Medium | Shared `code-index` live read-during-rewrite race; atomic publish never extended to it (extends H20/H27) | BH Prompt 13 + IC §1 |
| I7 | Medium | `publish = commit` not path-scoped/serialized in the shared worktree (extends H16/H2) | IC §5 + BH Prompt 7 + KB Prompt 8 |
| I8 | Medium | No oracle-coverage signal; mid-backfill oracle reads as "no contract" silently | IC §2 + KB Prompt 6 + BH Prompt 24d |
| I9 | Medium | BH eval can't measure the injection convention its poison fixture claims to grade (extends H5) | BH Prompts 27/28 + Prompt 4 |
| I10 | Low | `fix-request-emit` idempotency wording collides with never-reuse-on-`Reopened` (extends H15) | BH Prompt 33 |
| I11 | Low | `fix-verification` doesn't confirm the fix is present at HEAD before `fix-failed` | BH Prompt 31 |
| I12 | Low | `pre-merge` trigger contradicts the single-worktree rule (extends H2/H17) | IC §5 + BH Operating section |
| I13 | Low | Ledger rollback has no cross-system consumer invalidation (extends H16) | IC §5 |

**Suggested application:** a **bug-hunter v3.4** (carries most: I1-BH, I2, I4, I5, I6-BH, I9, I10, I11,
I12-BH + its share of I3/I7/I8), an **integration contract v1.3** (I1, I2, I3, I7, I8, I12, I13 + the
code-index line of I6), and **knowledge-builder point fixes → v3.3** (I3-KB, I8-KB + shares of I1/I7).
The dominant root cause (I1/I6/I7) is one decision — two systems co-resident in one `main` worktree
with per-system locks — closed by a single cross-system mutex plus atomic, path-scoped publishes; fix
that cluster first, alongside the two fix-loop state-machine defects (I2, I3).
