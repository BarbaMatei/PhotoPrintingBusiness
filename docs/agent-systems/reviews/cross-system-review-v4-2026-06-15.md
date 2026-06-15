# Cross-System Critical Review v4 — Bug-Hunter v3.4 × Knowledge-Builder v3.3 × Integration Contract v1.3

> **Status: APPLIED (2026-06-15).** All four findings landed: bug-hunter **v3.5**, integration
> contract **v1.4**, knowledge-builder **v3.4**. J1 — the close audit keeps its store-scoped diff
> **and** gains a forbidden-ground check (no write under app source / `memory-bank/` / `docs/` except
> the one approved test file). J2/J3 resolved together by making the shared code index a **gitignored,
> regenerable build artifact** (never committed, never audited) + a one-sentence KB convention
> carve-out. J4 — the schedule's ordering is intentional (Phase 5's eval doesn't exercise the loop, so
> it may precede Phase 4); both documents now say so and the KB cross-system summary is completed.
> Four findings (J1–J4), continuing the F → G → H → I sequence; none required structural change. Three
> of the four (J1–J3) are the *next* residual of the same co-residence work the I-round did.

*Review date: 2026-06-15. Documents reviewed in full (current spec-of-record):*

- `docs/agent-systems/archive/bug-hunter-build-guide-v3.4.md` (BH)
- `docs/agent-systems/archive/knowledge-builder-build-guide-v3.3.md` (KB)
- `docs/agent-systems/archive/integration-contract-v1.3.md` (IC)

*Method: a close line-by-line read of all three documents, with each candidate finding checked back
against the exact cited text before inclusion. Applied findings F1–F23, G1–G16, H1–H35, and I1–I13
are not re-reported. This round is written in plain language first — each finding opens with what it
means in everyday terms, then gives the precise location for whoever implements the fix.*

---

## A note for the non-technical reader

Three programs share one filing system here:

- the **bug-hunter** (BH) — looks for bugs,
- the **knowledge-builder** (KB) — writes down what the project is *supposed* to do, and
- the **integration contract** (IC) — the rulebook both of them must obey where they meet.

They all work inside one shared workspace, and they share one common "index" file (think of it as a
table of contents for the codebase) that *either* program is allowed to update. Most of the four
issues below come from that sharing: the latest round of safety fixes made each program tidy up only
*its own corner*, and in doing so a few things between the corners stopped being watched. None of
these are catastrophic, and the documents are otherwise in very good shape after three review rounds —
but each is a real gap worth closing before anyone builds against these specs.

---

## High severity

### J1 — The end-of-run safety check now only looks inside the program's own folder, so it can no longer catch the one thing it most needs to catch: an edit to your real app code

**In plain terms.** Both programs have one hard rule: *never touch the actual application code* — they
are only allowed to read it, never change it. To enforce that, each program runs a "guard" at the end
of every run that is supposed to shout loudly if anything was changed that shouldn't have been. A
recent fix told the guard to *only inspect the program's own folder* (this was to stop the two
programs from falsely accusing each other when they run at the same time). The side effect: the guard
now does a careful job of the one room it's allowed to write in — and never looks anywhere else in the
building. So if a program ever did edit your real app code (by accident or otherwise), the guard would
not notice, because that code lives outside the room it was told to check. The promise written in the
rulebook — *"we will loudly stop the run if anything outside the allowed set was touched"* — is no
longer true; the guard can only see one room. There's even an entry on the guard's own "allowed" list
("approved test files") that lives in a *different* room, so the guard can never actually confirm it
either way.

**Where it lives.** IC §1 first promises the audit fails "loudly on any write outside its allowed
set" (IC 66–67), then in the next breath restricts it to "diffs only the run's own store paths"
(`-- bug-hunting/` / `-- knowledge/` + the shared code index) (IC 67–69). The implementations follow
the restriction: BH Prompt 7 Close diffs `git status -- bug-hunting/` against an allowed set that
itself lists "approved test files" — which live *outside* `bug-hunting/` (BH 605–610); KB Prompt 8
does the same for `knowledge/` (KB 686–688). The core invariants this audit is meant to protect —
"No component edits your app code" / "Read-only on application source" (BH 267–269) and "Read-only on
the repo and on AI-DLC artifacts" (KB 283–284) — are now outside the audit's field of view.

**Why it matters.** The audit was the automatic backstop for the single most important promise in both
systems. After the I1 scoping it can flag a stray write *within* a program's own folder, but it is
structurally blind to a write into application source or `memory-bank/` — exactly the dangerous case.
The docs present the scoping as purely a benefit; they never acknowledge this blind spot.

**Suggested fix.** Keep the scoped, per-folder check (it correctly stops the two programs falsely
aborting each other), but add a second, narrow check that the run did **not** modify anything under
application source, `memory-bank/`, or `docs/` — the directories both systems are sworn to leave
untouched — with the *one* sanctioned exception of an owner-approved regression-test file (BH Prompt
30). Reword IC §1 so the "any write outside its allowed set" promise matches what the mechanism
actually inspects, rather than overstating it.

### J2 — The knowledge-builder's rulebook says "I only ever write in my own folder," but it is in fact required to update a shared file in the bug-hunter's folder

**In plain terms.** The knowledge-builder states, as a headline rule, *"my only writes live in my own
`knowledge/` folder."* But elsewhere the very same system is given permission — and is expected — to
refresh the shared table-of-contents index, and that index physically lives inside the *bug-hunter's*
folder. So the headline rule promises something the system is designed to break. The design itself is
fine (the shared index is meant to be updatable by both), but the plain-English rule and the fine
print contradict each other. Someone reading the headline rule could reasonably build the wrong safety
checks, or be baffled when the system writes somewhere it "promised" never to write.

**Where it lives.** KB's shared conventions: "Its only writes live under `knowledge/`" (KB 283–284).
But IC §1 names the code index "the one sanctioned dual-writer" that "either system's run may refresh,"
and locates it at **`bug-hunting/code-index/`** (IC 48); BH confirms that location (BH 751). And KB's
own end-of-run audit explicitly scopes itself to "`git status -- knowledge/` **+ the shared code
index**" (KB 686–688) — i.e., it already assumes KB writes outside `knowledge/`. So the convention is
contradicted by the contract *and* by KB's own orchestrator brief.

**Why it matters.** This is a contradiction inside a single document, on a safety-relevant rule. It is
low-risk at runtime but high-risk for misunderstanding: it is precisely the kind of "headline says X,
fine print says not-X" gap that leads a builder to implement the wrong guard (and is the same root
confusion behind J1 and J3).

**Suggested fix.** Reword the KB convention to carve out the one true exception, e.g.: *"The knowledge
builder writes only under `knowledge/`, with one exception — it may refresh the shared, regenerable
code index under `bug-hunting/code-index/`, which by contract (IC §1) is owned by neither system."*
One sentence reconciles the convention with the contract and with KB's own audit scope.

### J3 — When the knowledge-builder updates the shared index, its "save" command doesn't actually save it, so those changes are left loose and later get swept into the wrong program's save

**In plain terms.** Each program "saves its work" (commits to version control) by saving *only its own
folder* — the bug-hunter saves the `bug-hunting/` folder, the knowledge-builder saves the `knowledge/`
folder. But the shared index lives inside the bug-hunter's folder. So when the knowledge-builder
updates that shared index and then runs its save, the save command (which only ever saves the
`knowledge/` folder) **skips the index changes entirely**. Those changes sit around unsaved until, by
accident, the *next* bug-hunter run scoops them into its own save — mixing one program's work into the
other's history. The rulebook proudly says "every save is a clean point you can roll back to," but the
shared index falls through the crack whenever the knowledge-builder is the one who touched it.

**Where it lives.** IC §5 makes every save strictly folder-scoped: "stages only the publishing
system's own store (`git add -- bug-hunting/` or `git add -- knowledge/`, never `git add -A`)"
(IC 186–193). The shared index lives under `bug-hunting/` (IC 48; BH 751). KB's save is `git add --
knowledge/` (KB 689) — which cannot include the index. So a KB-side index refresh is never committed
by the run that made it. This directly undercuts the §5 guarantee that "the commit IS the restore
point."

**Why it matters.** The index is "derived and regenerable," so this is not data *loss* — but it
breaks the clean-restore-point promise (a rollback to version N−1 may not restore the index that was
live at the time), and it leaves the workspace dirty after a KB run, which can then confuse the next
bug-hunter run's audit (see J1). It is a direct consequence of the same location mismatch as J2.

**Suggested fix.** Decide, explicitly, who commits the shared index and when — either (a) have
whichever run refreshed the index also stage just that path (`git add -- bug-hunting/code-index/`)
alongside its own folder, or (b) treat the index as untracked/regenerated-on-demand and say so in
IC §1 — but pick one. Today the docs imply it is version-tracked yet leave no one responsible for
committing it after a KB refresh.

---

## Medium severity

### J4 — The shared build schedule tells you to do the knowledge-builder's "grade it" stage before its "finish it" stage, which contradicts the knowledge-builder's own step-by-step order

**In plain terms.** The knowledge-builder's instructions say: build it in order, one stage at a time,
and do the **loop-integration** stage (Phase 4 — wiring the bug→fix→learn loop closed) *before* the
**measurement** stage (Phase 5 — grading how accurate the whole thing is). That order makes sense:
you shouldn't grade a machine that isn't fully assembled yet. But the master schedule that coordinates
*both* programs quietly says the opposite — it lets you do the grading stage (Phase 5) earlier, while
the loop-integration stage (Phase 4) has to wait. The two documents disagree about which comes first,
and by the rulebook's own tie-breaker the *schedule* wins — so a builder dutifully following the
step-by-step instructions would be overruled by the schedule and could end up grading a half-finished
system.

**Where it lives.** IC §7 step 4 lets "KB Phases 3 **and 5**" run in parallel with bug-hunter bolts
092–093, while step 5 gates "KB Phase 4" to *after* bolt 093 (IC 222–224). That places Phase 5
(Measure — `distillation-eval`, which grades "the whole pipeline") ahead of Phase 4 (Loop
Integration, which fills the pipeline's loop-closing stage). But KB's master build order is explicitly
"dependency-ordered; build top to bottom," with Phase 4 listed before Phase 5 (KB 212–220). IC also
says it "wins over any brief" — so the contradiction resolves in favor of the out-of-order schedule.
Separately, KB's own "Build order across systems" summary (KB 165–172) jumps straight from bolt 091 to
"Phase 4 after bolt 093" and never states where Phases 3 or 5 belong at all — so the KB guide read
alone gives an *incomplete* interleave that only §7 completes, and completes inconsistently.

**Why it matters.** A builder following the contract would run the accuracy evaluation against a
pipeline still missing its Phase-4 loop-closing — either producing misleading "the oracle is accurate"
results or forcing a re-evaluation later. At minimum the two documents must agree on the order.

**Suggested fix.** Reconcile the two. If Phase 5's evaluation genuinely does not depend on Phase 4
(the eval fixtures don't appear to exercise the fix loop), then say so explicitly in KB's master order
("Phase 5 may precede Phase 4") so the "top to bottom" instruction no longer contradicts the schedule.
If it does depend on Phase 4, fix IC §7 step 4 to read "KB Phases 3 and 4" (or move Phase 5 to after
Phase 4). Either way, add the missing Phase-3 and Phase-5 placements to KB's cross-system summary (KB
165–172) so the KB guide alone gives the complete, correct interleave.

---

## Summary table

| ID | Severity | Finding (plain) | Where the fix lands |
|---|---|---|---|
| J1 | High | The end-of-run safety check only inspects each program's own folder, so it can no longer catch an edit to real app code — the very thing it exists to prevent | IC §1 + BH Prompt 7 + KB Prompt 8 |
| J2 | High | KB's rule "I only write under `knowledge/`" contradicts its required, sanctioned writes to the shared index under `bug-hunting/` | KB conventions (284) + IC §1 |
| J3 | High | KB's folder-scoped "save" skips the shared index it just updated, breaking the "every save is a clean restore point" promise | IC §5 + KB Prompt 8 + BH Prompt 13 |
| J4 | Medium | The contract's build schedule runs KB's "measure" stage before its "finish the loop" stage, contradicting KB's own top-to-bottom order | IC §7 + KB master order + KB cross-system summary |

**Root-cause note.** J1, J2, and J3 are three faces of one decision left half-finished: the shared
code index lives inside one system's folder while being writable by both, and the I-round's
folder-scoped audit and folder-scoped save were never extended to cover the seams *between* the
folders. Closing that seam (decide who owns/commits the shared index, and give the audit a narrow
"did we touch forbidden ground?" check) resolves all three. J4 is independent — a straightforward
ordering disagreement between two documents that simply needs the two to be made to agree.
