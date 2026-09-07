---
id: 015-blinding-auditor-and-scheduled-self-review
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-09-04T01:20:00Z
assigned_bolt: 087-phase-2-trust
implemented: false
---

# Story: 015-blinding-auditor-and-scheduled-self-review (gap from story 007, verified by bolt 086)

**Status:** gap confirmed by bolt 086-phase-1-skeleton-agents
(`memory-bank/bolts/086-phase-1-skeleton-agents/test-walkthrough.md`, story 007, v3.7 extensions
1 and 3). Both are extensions of the same component — the orchestrator — so they are filed
together.

## User Story

**As** the owner trusting a "blind" pass to be blind
**I want** blinding enforced when the hunter is dispatched, not requested in its prompt
**So that** the convergence numbers the loop's stop rule depends on mean what they say

## The defect, concretely

**Blinding (v3.7 extension 1).** The lens prompt asks for it: "do NOT read anything under the
`reviews/` directory, and do NOT run any git history command"
(`reviews/lib/discovery-review.wf.js:127-129`). Nothing checks. The loop states this against
itself in both places a reader would look:

- "Discovery is **blinded** (best-effort: enforced by prompts, unverified until the blinding
  auditor exists)" — `reviews/README.md:198`
- the same, plus the known leak: "commit messages and test names are an accepted leak" —
  `reviews/runbooks/runbook-discovery.md:14-16`

The brief's answer is an auditor at launch: inspect the inputs a hunter would receive and refuse
to dispatch when they carry prior records, finding ids or repository history — with verification
and the re-argument of a decided finding exempt, because those are anchored on purpose
(`docs/agent-systems/integration-contract.md` §6). This matters more than it looks: convergence
between lenses is the loop's precision signal and the discount that skips skeptics
(`wf.js:39-42`), and agreement a shared hint could have planted earns no credit. If blinding
silently fails, the convergence count is inflated and the stop rule fires early.

**Scheduled self-review (v3.7 extension 3).** The system does review itself — `reviews/system/`
holds two system reviews and a ledger of 47 `SF<n>` rows (20 fixed, 18 verified, 4 open, 2
deferred, 2 false-positive, 1 wont-fix), matching the guide's "raised 47 findings against its own
machinery". But nothing *schedules* it. Both runs happened because a person asked, which means
the machinery is reviewed exactly as often as somebody remembers to ask.

## Acceptance Criteria

- [ ] **Given** a discovery-type pass about to dispatch a hunter, **When** its inputs are
      assembled, **Then** they are inspected and the dispatch is refused if they carry prior
      records, finding ids or repository history for the target
- [ ] **Given** a verification pass or the re-argument of a decided finding, **When** it
      dispatches, **Then** the auditor does not refuse it — those postures are anchored by design
- [ ] **Given** the accepted leaks (commit messages, test names), **When** the auditor runs,
      **Then** each is either closed or recorded as a named, deliberate exception with its reason
- [ ] **Given** a refusal, **When** it happens, **Then** it says which input carried what, so the
      fix is obvious rather than a hunt
- [ ] **Given** the fixture suite, **When** this lands, **Then** a case dispatches a hunter with a
      prior record in its inputs and proves the refusal — the guide's own v3.7 test
- [ ] **Given** the loop's own machinery, **When** a period passes without a system review,
      **Then** one is proposed — the schedule is written down and something raises it, rather
      than it depending on somebody remembering
- [ ] **Given** the descriptive-standards rule, **When** the auditor exists, **Then**
      `reviews/README.md:198` and `reviews/runbooks/runbook-discovery.md:14-16` are corrected in
      the same change — both currently say it does not

## Technical Notes

- The dispatch point is one place: the lens fan-out at `reviews/lib/discovery-review.wf.js:331`,
  with the inputs assembled by the caller per the runbook's "Before the script" section. An
  auditor there sees everything a lens would.
- The scheduling half is small and can be a router note rather than a cron: the driver already
  audits and routes at every invocation, and "the system has not been reviewed since <date>" is
  the same kind of fact as "a manifest lens is owed".

## Dependencies

### Requires
- 007-orchestrator-skeleton (verified satisfied-with-these-gaps by bolt 086)

### Enables
- Convergence counts, and therefore the stop rule, resting on something checked rather than
  something asked for
