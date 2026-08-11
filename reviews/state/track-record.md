---
type: review-track-record
status: active
created: 2026-07-30
owner: Matei Barba
---

# Certification track record

The trust ledger [self-driving-loop-design.md](../notes/self-driving-loop-design.md) calls for: every
certification, and every serious defect later shown to have **survived** one (a
"post-cert escape"). Escapes ÷ certifications is the system's live false-certification rate —
the interim trust measure while seeded-bug run 2 stays deferred (owner decision 2026-07-29).

## The rule

When reconciling any pass's findings for a target listed below: a new 🔴/🟠 whose defect
mechanism **already existed in the certified commit's code** (cited site unchanged since that
commit — verify with `git diff <certified>..<reviewed> -- <file>`) is marked
`post-cert-escape` on its ledger row and appended to *Escapes* below, same day, by the
synthesizer. A serious defect **introduced by later changes is not an escape** — it belongs to
the next loop. When unsure whether it existed at the certified commit, record it as an escape
with the doubt stated: under-counting escapes corrupts the one number trust rests on.

## Certifications under watch

| Target | Certified | Commit | Honest scope | Escapes |
|---|---|---|---|---|
| 043-cloud-storage-provider | 2026-07-22 (v9, single-pass) | `ac97e42` | no serious defect survives — mediums not saturated | 0 |
| 015-sameday-shipping | 2026-07-28 (v5, single-pass) · loop closed 2026-07-29 by owner sign-off @`5734021` | `5fc330b` | no serious defect survives at `5fc330b`; the 47 later fixes were individually verified but never blind-searched; feature dormant behind two false flags, pre-enable checklist is the gate | 0 |

An 015 escape is judged against the closure commit `5734021`; note in the entry whether it
also predates `5fc330b` (that grades the certification itself, not just the sign-off).

**044-045-observability is not in the table and must not be counted as a certification.** Its loop
closed 2026-08-10 by owner sign-off at `a4eb7e5` with no certification pass ever run, so a serious
defect found later in its files is a **missed defect of the v1 discovery**, not a post-cert escape —
it does not belong in the escapes-÷-certifications rate. Recorded here so the distinction survives.

## Escapes

None recorded. This section is the counter — append, never rewrite.

| Date | Target | ID | Sev | Existed at certified commit? | Found by | Evidence |
|---|---|---|---|---|---|---|
