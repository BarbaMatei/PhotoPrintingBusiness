---
id: 010-confidence-axis-reporting-floor
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-09-03T21:40:00Z
assigned_bolt: 087-phase-2-trust
implemented: false
---

# Story: 010-confidence-axis-reporting-floor (gap from story 004, verified by bolt 085)

**Status:** gap confirmed by bolt 085-phase-1-skeleton-core
(`memory-bank/bolts/085-phase-1-skeleton-core/test-walkthrough.md`, story 004 rows 3–6).

## User Story

**As** the owner reading a pass
**I want** to see how sure the finders were, not only how bad the defect would be
**So that** a shaky 🔴 and a proven one are not presented as the same thing, and a dangerous
unconfirmed item is never quietly out of sight

## The defect, concretely

Every lens already returns a confidence of 1–10 per finding — it is a required field of the
lens schema (`reviews/lib/discovery-review.wf.js:135,214,216`) — and the skeptic stage already
produces a confidence-shaped verdict (`confirmed` / `plausible` / `refuted` /
`unverified-low` / `unverified-over-budget`, `wf.js:29-32`). **None of it survives into a
published record.** `grep -rn "confidence" reviews/templates reviews/rules/doc-contracts.md`
returns nothing. The floor the loop does apply is on severity: 🟡/⚪ go to the ledger as
`backlog` and appear in the summary's "Filed automatically". So a low-confidence 🔴 is
foregrounded exactly like a proven one, and the brief's mandatory callout — "⚠ unconfirmed but
Critical if real" — has nothing to attach to.

Three smaller record-contract holes were confirmed at the same time and belong with this story,
because all four are one change to what a record carries:

- **No `injection_suspected` carrier.** Nothing marks source text that tried to instruct the
  reader; the `security` lens hunts injection as a defect class (`schema.mjs:132`), which is a
  different thing.
- **No redaction rule.** Nothing says a secret-involving finding carries a location and a
  fingerprint rather than the value. The only backstop is repo-wide and after the fact:
  `.github/workflows/secret-scan.yml`.
- **No Observations section.** A non-defect observation has nowhere to go; the Refuted table is
  for disproved suspicions, not observations.

## Acceptance Criteria

- [ ] **Given** a finding whose lens confidence and skeptic verdict are known, **When** the
      review is written, **Then** the record carries that confidence — the ledger row or its
      detail block — and `reviews/rules/doc-contracts.md` states the field in the same change
- [ ] **Given** a low-confidence finding, **When** the review renders, **Then** it is separated
      from the confirmed ones rather than interleaved, whatever its severity
- [ ] **Given** a low-confidence finding of high severity, **When** it is separated, **Then** the
      body carries a one-line callout naming it — the dangerous-if-real item is never only in an
      appendix
- [ ] **Given** a finder that met instruction-like text in the source, **When** it reports,
      **Then** the finding carries an injection flag and the report shows it
- [ ] **Given** a finding about a secret, **When** evidence is written, **Then** the record
      carries location + fingerprint, never the value, and the doc gate can see the difference
- [ ] **Given** a pass with a non-defect observation, **When** it renders, **Then** there is a
      place to put it that is not the Refuted table
- [ ] **Given** the caps, **When** any of the above lands, **Then** the review body cap (120
      lines) still holds — this is a change of what a record carries, not a licence to grow it

## Technical Notes

- The data already exists at the source; this is a carrying problem, not a measurement problem.
  Start at the synthesis step of `reviews/runbooks/runbook-discovery.md:110-142`, where lens
  output becomes records.
- The doc gate is the place the new fields become enforceable (`reviews/lib/records/doc-gate.mjs`),
  and every gate change needs its fixture case.

## Dependencies

### Requires
- 004-report-rendering (verified satisfied-with-this-gap by bolt 085)
- 002-bug-documentation (re-assigned to 087) — the record's field set is the same surface

### Enables
- A first-contact report that separates "this is real" from "this might be real"
