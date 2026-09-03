# Thesis Proposal — *Lucrare de licență*

**Working title:** A Sole-Writer Ledger Architecture with a Closed Verification Loop for Coordinating Autonomous Coding Agents

**Faculty / specialization:** Universitatea Babeș-Bolyai, Facultatea de Matematică și Informatică — Informatică
**Coordonator științific:** _(to be confirmed — Software Engineering / Distributed Systems / AI)_
**Student:** _(name)_
**Academic year:** 2025–2026

> Companion reference: [`bibliography-relevant-passages.md`](bibliography-relevant-passages.md) — annotated source passages mapped to the claims and chapters below.
>
> Reconciliation of the design with the implementation: [`docs/agent-systems/theory-vs-practice-2026-09.md`](../docs/agent-systems/theory-vs-practice-2026-09.md).

---

## 1. Context and motivation
LLM-based coding agents can now build and fix software autonomously, but running *several* of them against one codebase raises a problem that is more systems-engineering than AI: how do independent agents coordinate without corrupting shared state, and how do we trust that a reported fix is *actually* present? This thesis studies that coordination problem and proposes an architecture that makes both **safety** (no conflicting writes) and **soundness** (no fix accepted on an agent's word alone) explicit, enforceable properties.

## 2. Objective and research question
Formalize, extend and evaluate a multi-agent system in which an autonomous **Inspector** agent finds defects in a real codebase and drives them through a **closed verification loop**, governed by an **integration contract** with a single-writer storage discipline. The Inspector's engine exists: the review loop built June–September 2026 (`reviews/`), running in pre-merge mode; the thesis formalizes it, extends it toward the full pipeline and measures it.

*Research question:* Can a sole-writer ledger plus a correlation-id-keyed verification loop guarantee conflict-free coordination and sound fix-confirmation, while detecting real defects at a useful rate?

## 3. Expected contributions
1. A formal model of the coordination invariants — the **sole-writer storage map**, the cross-system mutex / single-history rule — stated as concurrency-safety properties with a correctness argument.
2. The **bug → fix → re-verify** loop modeled as a state machine (`open → fix-reported → verified-fixed | fix-failed | closed-unverified`), with the "never closed on the Builder's word alone" rule as a soundness guarantee.
3. A working implementation grounded in a real .NET + Angular application — the review loop built June–September 2026 (`reviews/`), extended toward the full Inspector and formalized against the contract — plus an empirical evaluation against a baseline.

## 4. Methodology and technologies
Design-science methodology: **formalize → build → measure.** Stack: .NET / C# and Angular (the application under test), LLM agents orchestrated as skills, a git-backed append-only ledger, integration-worktree isolation. Concepts: event-sourcing / ledger patterns, single-writer concurrency, automated program repair.

## 5. Work plan (milestones)
> Weeks are illustrative — align to the faculty's actual *licență* calendar.

- **M1 (weeks 1–3):** State of the art; formalize the invariants and the loop state machine.
- **M2 (weeks 4–8):** Extend and formalize the existing Inspector pipeline (Map → Hunt → Verify → Triage → Report → Learn): add the Map slot and the contract enforcement (mutex, sole-writer checks, id reservation); document the pre-merge mode that exists.
- **M3 (weeks 9–11):** Extend the existing evaluation harness (the seeded-defect protocol and its first run, the certification track record); design and seed the second, harder run.
- **M4 (weeks 12–14):** Run experiments, analyze, write up. Future-work chapter: the full agent organization (Conductor, Analyst, Reviewer, Test-Quality, Observability).

## 6. Evaluation plan
**Quantitative:** defect detection rate, false-positive rate, loop-closure correctness (fraction of "verified-fixed" claims that hold at HEAD), time-to-verify — compared to a baseline (static analyzer + git hooks).
**Data already on disk (2026-09):** 234 fixes verified by revert-and-rerun with 6 reopened (loop-closure correctness ≈ 97%); two certifications with zero post-certification escapes so far; skeptic `refuted` verdicts on every pass as a false-positive proxy; one seeded-defect run (10/10, uninformative — the second, harder run is the thesis's key experiment); median 25 minutes per fixed finding and ≈ 293k tokens per serious finding.
**Qualitative:** a worked end-to-end case study of one defect through the full loop.

## 7. Selected bibliography
> Starting set — all real, well-known works. Verify each citation and read it in full before relying on it; add what your coordonator favors. See the companion file for the relevant passages and how each maps to this proposal.

- Yao et al., *ReAct: Synergizing Reasoning and Acting in Language Models*, ICLR 2023.
- Shinn et al., *Reflexion: Language Agents with Verbal Reinforcement Learning*, NeurIPS 2023.
- Wu et al., *AutoGen: Multi-Agent Conversation Framework*, 2023.
- Yang et al., *SWE-agent: Agent-Computer Interfaces for Automated SE*, NeurIPS 2024.
- Jimenez et al., *SWE-bench: Can LLMs Resolve Real-World GitHub Issues?*, ICLR 2024.
- Le Goues, Pradel, Roychoudhury, *Automated Program Repair*, Communications of the ACM 62(12), 2019.
- Fowler, *Event Sourcing* (martinfowler.com); Evans, *Domain-Driven Design*, Addison-Wesley, 2003.
