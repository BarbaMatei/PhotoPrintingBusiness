# The deliberately-broken records root

`--root reviews/lib/tests/fixtures/repo` is the fixture repository the doc gate, the records
auditor, the renderer and the id scan are pointed at. Its records are **broken on purpose**, so
a test can assert the exact refusal each break earns.

That only works while every break is one somebody planted. Since the chapter-close waves the
root is **intentional-only**: `node reviews/lib/records-auditor.mjs --root
reviews/lib/tests/fixtures/repo` reports **30 errors**, and every one of them is either asserted
by `reviews/lib/tests/unit/records-auditor.test.mjs` or listed below. That count is pinned by an
assertion in the same file, so a record added here carelessly reddens the suite instead of
disappearing into the noise.

## What belongs here, and what does not

A target belongs in this root when a suite reads **the files themselves** — the doc gate's
structure lint, the auditor's whole-root scan, the renderer's writes, the cross-target id scan.

A target whose only readers are the router and the autonomy policy does **not**: those read a
*routing state* (ledger rows, metrics lines, resolution frontmatter, the review-file count), and
that is built at run time from a compact spec beside the assertion — see
`reviews/lib/tests/fixture-builder.mjs`. Parking such a state here instead puts it in front of
the whole-root auditor, where every schema field the router does not care about reads as an
error nobody planted. Twenty-five states were moved out for exactly that reason.

## The planted errors

**Twelve asserted by name in `unit/records-auditor.test.mjs`**

| Where | Planted break | Errors |
|---|---|---|
| `908-verification-lineage` metrics line 3 | one verification `findings[]` entry with every field malformed at once (`d`, `new`, `sev`, `sev_delta`, `seed_round`, `area`) | 6 |
| `921-gates-bad` `resolution-v1.md` | a hand-back round missing all four kinds of gate evidence (audit R1–R4), including a protocol block stamped after the diff | 5 |
| `901-good-target` + `905-dup-ledger` | `PPW-9001` given a ledger row in both, for the cross-target id scan | 1 |

**Seventeen from placeholder commits**

Every commit this root cites — review frontmatter `commit:`, metrics `commit`, fix-round
`base_commit`/`fixed_commit` — is a made-up 7-hex placeholder (`aaaaaaa`, `abcd122`, `bbbbbb3`
…), because a fixture must not depend on this repository's history. The auditor resolves every
sha it reads, so each placeholder earns one `commit … does not resolve in this repo`. This is
permanent and deliberate; `902-broken-target`'s pair is even asserted verbatim by the doc-gate
suite (`commit ddddddd differs from review-v1.md's aaaaaaa`).

**One dangling correction round**

`907-correction-target` metrics line 4 corrects round 99, which has no `resolution-v99.md`. The
router suite asserts that such a correction never surfaces (`!out.includes('fix round 99')`), so
the dangling round is the fixture, and `correction_for.round 99 matches no resolution-v99.md` is
what the auditor is supposed to say about it.

## Warnings are not pinned

The eleven warnings — missing index rows, passes sharing a number without pair subtypes, a
fix-round line with no worklog — are byproducts of records written for one reader. Only the
error count is a pin.
