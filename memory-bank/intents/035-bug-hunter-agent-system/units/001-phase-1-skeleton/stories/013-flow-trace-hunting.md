---
id: 013-flow-trace-hunting
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-09-04T01:20:00Z
assigned_bolt: 087-phase-2-trust
implemented: false
---

# Story: 013-flow-trace-hunting (gap from story 006, verified by bolt 086)

**Status:** gap confirmed by bolt 086-phase-1-skeleton-agents
(`memory-bank/bolts/086-phase-1-skeleton-agents/test-walkthrough.md`, story 006 row 2b).

## User Story

**As** the Hunt stage
**I want** to walk a flow from its entry point downward as a way of hunting, not only to sweep
what a diff touched
**So that** a defect in the *interaction* between a change and the untouched code above or below
it is found by design rather than by luck

## The defect, concretely

The hunt is diff-scoped by construction. The discovery runbook's first step is
`git diff main...HEAD` (`reviews/runbooks/runbook-discovery.md:20-22`), and the core lens prompts
are written "of the changed logic" and "across the change"
(`reviews/lib/discovery-review.wf.js:140,144,149`). Nothing enumerates entry points — routes,
controllers, `main`, handlers — and no lens walks a flow hop by hop checking validation, auth,
error handling and state/transaction handling at each one, which is half of what the brief's
hunter does (`docs/agent-systems/bug-hunter-build-guide.md`, Prompt 6).

The only top-down trace in the system is the per-finding trace skeptic
(`reviews/lib/discovery-review.wf.js:278-282`) — and it runs *after* a candidate exists. That is
verification, not hunting.

The per-hop checks are also scattered across lenses that are not core: validation lives in
`input-validation`, error handling in `observability`, transactions in `race` — all *added*
lenses, chosen by what the change touches (`reviews/lib/records/schema.mjs:139-147`). A change
that triggers none of them gets no hop check at all.

The lens prompts do push outward ("the highest-value defects live in the interaction between
changed and UNCHANGED code", `wf.js:100`), which is the right instinct without the method.

## Acceptance Criteria

- [ ] **Given** a target, **When** a full discovery pass runs, **Then** at least one lens hunts by
      flow: it names the entry points it walked and the hops it checked, rather than working from
      the diff alone
- [ ] **Given** a hop, **When** it is checked, **Then** validation, auth, error handling and
      state/transaction handling are each asked about — whichever added lenses the change did or
      did not trigger
- [ ] **Given** a delta pass, **When** it runs, **Then** the flow trace is scoped or skipped
      deliberately, with the choice recorded — a delta pass has a token budget and this must not
      quietly eat it
- [ ] **Given** the coverage record, **When** a pass ends, **Then** what was traced is recorded
      the way lens coverage is, so an untraced flow is visible rather than assumed
- [ ] **Given** this lens exists, **When** it first runs on a live target, **Then** its findings
      are compared against what the diff-scoped lenses found on the same commit — the point is
      to learn whether flow hunting finds anything the sweep does not

## Technical Notes

- This is a new manifest lens plus a scoping step, not a rewrite: `MANIFEST_LENSES`
  (`reviews/lib/records/schema.mjs:11-15`) and `LENS_LIBRARY` (`wf.js:139-196`) are the seams.
- The honest risk is cost. A flow trace over a whole application is a different size of job from
  a diff read, which is why the brief puts it in the standing-sweep mode. Scope it to the flows
  the change sits inside before scoping it to the application.

## Dependencies

### Requires
- 006-general-hunter (verified satisfied-with-this-gap by bolt 086)

### Enables
- Hunting that does not depend on a defect being inside the diff
