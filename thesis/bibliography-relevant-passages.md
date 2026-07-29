# Thesis bibliography — relevant passages (annotated)

Working title: *A Sole-Writer Ledger Architecture with a Closed Verification Loop for Coordinating Autonomous Coding Agents* — UBB FMI, Informatică.

**How to read this file**
- **VERBATIM** = copied from the source (abstract or public page). Safe to quote, but re-check exact wording/punctuation against the source PDF before it goes in the thesis.
- **POINTER / PARAPHRASE** = my accurate summary of a theme that lives in the source's body (which I could not extract verbatim) or in a book with no free full text. **Do not quote these as if verbatim** — find the exact sentence in the source and quote that.
- Each entry ends with *Why it's in our thesis* — the specific claim of ours it supports and the chapter it belongs in (Ch.2 = State of the Art, Ch.3 = Architecture/formal model, Ch.5 = Evaluation).

---

## 1. ReAct — Yao et al., ICLR 2023
*Shunyu Yao, Jeffrey Zhao, Dian Yu, Nan Du, Izhak Shafran, Karthik Narasimhan, Yuan Cao — "ReAct: Synergizing Reasoning and Acting in Language Models." arXiv:2210.03629.*

**VERBATIM (abstract excerpts):**
> "we explore the use of LLMs to generate both reasoning traces and task-specific actions in an interleaved manner … reasoning traces help the model induce, track, and update action plans as well as handle exceptions, while actions allow it to interface with external sources, such as knowledge bases or environments, to gather additional information."

> "ReAct overcomes issues of hallucination and error propagation prevalent in chain-of-thought reasoning by interacting with a simple Wikipedia API, and generates human-like task-solving trajectories that are more interpretable than baselines."

> "improved human interpretability and trustworthiness over methods without reasoning or acting components."

*Why it's in our thesis (Ch.2):* The reason-then-act-on-an-external-source loop is the primitive each of our agents runs (the Inspector reasons about a defect, then acts on the codebase/test harness). Cite for the foundational agent pattern and for the claim that **grounding actions in an external environment reduces hallucination** — which motivates why our loop verifies fixes against the real test, not the model's assertion.

---

## 2. Reflexion — Shinn et al., NeurIPS 2023
*Noah Shinn, Federico Cassano, Edward Berman, Ashwin Gopinath, Karthik Narasimhan, Shunyu Yao — "Reflexion: Language Agents with Verbal Reinforcement Learning." arXiv:2303.11366.*

**VERBATIM (abstract excerpts):**
> "Reflexion agents verbally reflect on task feedback signals, then maintain their own reflective text in an episodic memory buffer to induce better decision-making in subsequent trials."

> "Reflexion is flexible enough to incorporate various types (scalar values or free-form language) and sources (external or internally simulated) of feedback signals."

> "Reflexion achieves a 91% pass@1 accuracy on the HumanEval coding benchmark, surpassing the previous state-of-the-art GPT-4 that achieves 80%."

*Why it's in our thesis (Ch.2):* Direct precedent for our **Learn slot** and for using **test/environment feedback to iterate**. The "episodic memory buffer" is the closest published analogue to our persistent ledger of run history. Cite when arguing that feedback-driven retries improve outcomes — and contrast: Reflexion's memory is per-agent and internal, whereas ours is an *externalized, single-writer ledger* shared across agents (our delta).

---

## 3. AutoGen — Wu et al., 2023
*Qingyun Wu, Gagan Bansal, Jieyu Zhang, et al. — "AutoGen: Enabling Next-Gen LLM Applications via Multi-Agent Conversation." arXiv:2308.08155.*

**VERBATIM (abstract excerpts):**
> "AutoGen is an open-source framework that allows developers to build LLM applications via multiple agents that can converse with each other to accomplish tasks."

> "AutoGen agents are customizable, conversable, and can operate in various modes that employ combinations of LLMs, human inputs, and tools."

> "developers can also flexibly define agent interaction behaviors."

*Why it's in our thesis (Ch.2):* The canonical multi-agent-coordination baseline to **position against**. AutoGen coordinates agents through **conversation** (messages); we coordinate through **shared state with a sole-writer discipline** (a ledger, not a chat). Cite to frame our contribution: a coordination model that gives *write-safety and auditability guarantees* that conversational coordination does not.

---

## 4. SWE-agent — Yang et al., NeurIPS 2024
*John Yang, Carlos E. Jimenez, Alexander Wettig, Kilian Lieret, Shunyu Yao, Karthik Narasimhan, Ofir Press — "SWE-agent: Agent-Computer Interfaces Enable Automated Software Engineering." arXiv:2405.15793.*

**VERBATIM (abstract excerpts):**
> "we posit that LM agents represent a new category of end users with their own needs and abilities, and would benefit from specially-built interfaces to the software they use."

> "SWE-agent's custom agent-computer interface (ACI) significantly enhances an agent's ability to create and edit code files, navigate entire repositories, and execute tests and other programs."

> "We evaluate SWE-agent on SWE-bench and HumanEvalFix, achieving state-of-the-art performance on both with a pass@1 rate of 12.5% and 87.7%, respectively."

*Why it's in our thesis (Ch.2 + Ch.3):* Establishes that **interface design between agent and codebase materially changes performance** — justification for our integration-worktree + tool layer as a deliberate ACI, not an afterthought. Their "execute tests" step is the single-agent version of our verification; cite to motivate making verification a *first-class, separately-owned* stage rather than something the fixing agent self-reports.

---

## 5. SWE-bench — Jimenez et al., ICLR 2024
*Carlos E. Jimenez, John Yang, Alexander Wettig, Shunyu Yao, Kexin Pei, Ofir Press, Karthik Narasimhan — "SWE-bench: Can Language Models Resolve Real-World GitHub Issues?" ICLR 2024. arXiv:2310.06770.*

**VERBATIM (abstract excerpts):**
> "we introduce SWE-bench, an evaluation framework consisting of 2,294 software engineering problems drawn from real GitHub issues and corresponding pull requests across 12 popular Python repositories."

> "Resolving issues in SWE-bench frequently requires understanding and coordinating changes across multiple functions, classes, and even files simultaneously, calling for models to interact with execution environments, process extremely long contexts and perform complex reasoning that goes far beyond traditional code generation tasks."

> "The best-performing model, Claude 2, is able to solve a mere 1.96% of the issues."

*Why it's in our thesis (Ch.5, evaluation methodology):* The standard for **evaluating autonomous code-fixing**, and the model for our own harness: an issue + a codebase + **a test that decides pass/fail**. Cite to (a) justify using real defects with a deterministic test oracle as ground truth, and (b) set realistic expectations — resolution rates are low, so our **soundness check (was the fix actually present?) matters more than raw fix rate.** Note the date-sensitive figure (1.96% was Claude 2 at publication); use it as a historical baseline, not current SOTA.

---

## 6. Automated Program Repair — Le Goues, Pradel, Roychoudhury, CACM 2019
*Claire Le Goues, Michael Pradel, Abhik Roychoudhury — "Automated Program Repair." Communications of the ACM, Vol. 62, No. 12, Dec. 2019. DOI: 10.1145/3318162.*

**VERBATIM (abstract):**
> "Automated program repair can greatly relieve programmers from the burden of manually fixing the ever increasing number of programming mistakes. At the same time, achieving such a goal involves solving technical challenges in scalability, patch quality, and integration into developer work flows. This article presents an overview of the state-of-the-art tools and techniques in automated program repair. We also take a forward looking view of the area by presenting emerging and potential use cases for program repair, such as on-line programming education and patching of security vulnerabilities."

**POINTER (in the paper body — find & quote the exact lines yourself):**
- The **patch-overfitting problem**: repair tools validate candidate patches against a test suite, so a patch can pass all given tests yet still be incorrect because the test suite is an **incomplete specification of correctness**. This is discussed under the paper's *patch quality* challenge. **This is the single most important external citation for our thesis** — it is the published statement of exactly the failure mode our design defends against ("never closed on the Builder's word alone"). Pull the precise sentence from the full PDF: https://squareslab.github.io/materials/legoues-cacm2019.pdf
- "Integration into developer work flows" as a named challenge — supports our **human-at-two-checkpoints** operating model.

*Why it's in our thesis (Ch.2 + Ch.3, the core motivation):* This paper names the gap our architecture targets. Our **closed verification loop** (re-run the harvested proving test at HEAD; terminal `closed-unverified` with no oracle entry when there is no proving test) is a concrete response to the test-suite-as-weak-oracle problem. Frame our contribution as: *we cannot eliminate overfitting, but we make the strength of the evidence explicit and refuse to record an unproven fix as fixed.*

---

## 7. Event Sourcing — Martin Fowler (2005)
*Martin Fowler — "Event Sourcing." martinfowler.com/eaaDev/EventSourcing.html, 12 Dec 2005.*

**VERBATIM:**
> "Capture all changes to an application state as a sequence of events."

> "The fundamental idea of Event Sourcing is that of ensuring every change to the state of an application is captured in an event object, and that these event objects are themselves stored in the sequence they were applied for the same lifetime as the application state itself."

> "We can discard the application state completely and rebuild it by re-running the events from the event log on an empty application."

> "We can determine the application state at any point in time."

> "It's easy to serialize the events to make an Audit Log."

*Why it's in our thesis (Ch.3, architecture lineage):* Names the pattern our **append-only, git-backed ledger** descends from. Cite to ground the design choice and to claim the inherited properties — **auditability** ("Audit Log"), **replay/rebuild**, and **temporal query** ("state at any point in time") — which underpin our correlation-id-keyed loop and our "git is the version history" stance. Be precise: ours is *event sourcing applied to the agents' coordination state*, not to the application's domain state.

---

## 8. Domain-Driven Design — Eric Evans (2003) — PARAPHRASE ONLY
*Eric Evans — "Domain-Driven Design: Tackling Complexity in the Heart of Software." Addison-Wesley, 2003.*

> ⚠️ No free full text. The points below are **paraphrases of well-known DDD concepts**, not quotes. Cite specific page/section numbers from the book.

- **Aggregate** = a cluster of domain objects treated as a single unit, with one **Aggregate Root** as the only entry point; the aggregate is a **consistency / invariant boundary**, and external references go only through the root.
- **Bounded Context** = an explicit boundary within which a model and its terms have one consistent meaning.
- **Ubiquitous Language** = a shared, rigorous vocabulary used identically in code and conversation.

*Why it's in our thesis (Ch.3, the formal-model chapter — strong UBB-fit):* The DDD **Aggregate-as-consistency-boundary with a single root** is the conceptual ancestor of our **sole-writer rule**: each store has exactly one writer; cross-store writes never happen. Cite Evans to argue our single-writer-per-store discipline is a recognized way to preserve invariants under concurrency, lifted from object aggregates to **system-level stores**. (Bonus: the project's construction artifacts are already DDD-structured — `ddd-01-domain-model`, etc. — so the lineage is real, not decorative.)

---

## Cross-map: which source backs which claim of ours
| Our thesis claim | Primary sources |
|---|---|
| Agents must ground actions in a real environment, not self-report | ReAct (1), SWE-agent (4) |
| Feedback/test-driven iteration improves fixes | Reflexion (2), SWE-bench (5) |
| Multi-agent coordination is an open design space (and ours is state-based, not chat-based) | AutoGen (3) |
| Test suites are a weak correctness oracle → patches overfit → don't trust an unproven fix | **Le Goues et al. (6)** |
| Append-only ledger gives auditability, replay, temporal query | Fowler (7) |
| Single-writer-per-store preserves invariants under concurrency | Evans (8) |
| How to evaluate autonomous code-fixing rigorously | SWE-bench (5) |

## Before you cite anything
1. Re-verify every VERBATIM quote against the source PDF (wording can differ from abstract pages).
2. Replace the two **POINTER** entries (APR overfitting; DDD aggregates) with exact quotes + page numbers from the originals.
3. Read each paper in full — a UBB committee will probe whether you understand what you cited.
