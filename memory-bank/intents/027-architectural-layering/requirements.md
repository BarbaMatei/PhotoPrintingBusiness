---
intent: 027-architectural-layering
phase: inception
status: inception-complete
created: 2026-06-05T09:00:00Z
updated: 2026-06-05T09:00:00Z
source: docs/analysis/architect-review-2026-06-03.md (Group 3 — P21, P22, P23, P24, P25; folds P06, P11, P16)
priority_score: 21
---

# Requirements: Architectural Layering (Presentation / Application / Domain / Infrastructure)

## Intent Overview

This is the maintainer's **core complaint**: there is no clear Presentation / Application / Domain / Infrastructure layering anywhere — controllers, services, data, models, validators, middleware, background jobs, and configuration all sit flat at the top of one `PhotoPrint.API` project, layers leak by access path (four controllers inject `PhotoPrintDbContext` directly), interfaces are interleaved with implementations, and there is no home for multi-step use cases (the 145-LOC `OrderService.CreateFromCartAsync`, the duplicated post-Paid webhook fan-out). This intent codifies the layering **inside the single assembly (no new csproj)**, writes the ADR that records *why not* four projects, introduces the `Abstractions/` convention, locks in the no-repository posture with an analyzer, and introduces a lightweight handler-per-use-case pattern. It **subsumes** three first-pass proposals: P06 (Services feature-folders → becomes the `Application/<Feature>/` promotion), P16 (Domain extraction → becomes the first layering PR), and P11 (`OrderPaidEventDispatcher` → becomes the canonical first handler). Pure refactor — **zero behaviour change, zero EF migration drift**. Ships as a sequence of individually-mergeable PRs; must be tracked in lockstep with intent 028 (test architecture) or every PR breaks the build twice.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Clear, enforceable layer separation | Layer-rule violations caught by analyzer/review; no `DbContext` in controllers | Should |
| The "interfaces and classes in the same place" pain resolved | Every `I*.cs` lives under an `Abstractions/` subfolder | Should |
| Multi-step use cases have a discoverable home | `find Application -name '*Handler.cs'` lists the use-case inventory | Should |
| The monolith-not-microservices choice is documented | An ADR records the rejected 4-project split + revisit triggers | Could |
| No accidental data-access leakage | No service public signature returns `IQueryable<T>` (analyzer-enforced) | Should |

---

## Functional Requirements

### FR-1 (P22): Write the "no four-project clean-arch split" ADR (do first)
- **Description**: Record the load-bearing-but-currently-implicit decision to layer with folders + namespaces inside one assembly rather than splitting into `Domain`/`Application`/`Infrastructure`/`Web` csproj projects. Include the rejected alternative and the reasons (single deployable; EF migrations need `Design` reachable from the DbContext-owning project; test project would reference 4 csproj; 1–2 dev team).
- **Acceptance Criteria**:
  - ADR written under the bolts/architect-review ADR location; states revisit triggers (team > 4 devs; a domain ships as its own service; a domain's deps don't belong in the package).
  - Linked from `memory-bank/standards/system-architecture.md` so the next reviewer finds it before asking "why aren't these projects?".
- **Priority**: Could (ship first — the layering PRs reference it)
- **Related Stories**: TBD

### FR-2 (P21): Codify the four-layer folder structure inside `PhotoPrint.API` (folds P06 + P16)
- **Description**: Reshape the project into `Web/` (Presentation), `Application/` (use cases + DTOs, per-feature), `Domain/` (pure functions + POCO entities), `Infrastructure/` (EF Core, HttpClient, SDKs), with `Configuration/` (options + settings validators) flat. Namespace changes touch ~200 files; behaviour unchanged. Delivered as 5 sequenced PRs (Domain → Infrastructure → Web → Application → Configuration/Validators sweep).
- **Acceptance Criteria**:
  - Folder/namespace tree matches the review's target; the four controllers no longer inject `PhotoPrintDbContext` (data access moves behind `Application` services/handlers + `Infrastructure`).
  - Layering rules codified (CONTRIBUTING.md page + a banned-symbols analyzer rule each, or a review checklist): `Web/` ⇏ `Infrastructure`/EF; `Application/` ⇏ `Web`/`Infrastructure` (except via interface DI); `Domain/` references nothing in the project; `Infrastructure/` ⇏ `Web`/`Application` services.
  - `dotnet build && dotnet test` green after **every** PR; `Add-Migration NoOpVerify` produces empty up/down (zero schema drift).
  - The flat `BackgroundJobs/` (incl. Sameday jobs) lands under `Infrastructure/`.
- **Priority**: Should
- **Related Stories**: TBD

### FR-3 (P06 — folded into P21): Services feature-folder organisation
- **Description**: The first-pass proposal to break the flat 49-file `Services/` into feature folders (`Auth/`, `Orders/`, `Invoicing/`, `Sameday/`, `Storage/`, …) is **realized as P21-PR4** — the promotion of `Services/<Feature>/` to `Application/<Feature>/Services/`. No longer a standalone refactor.
- **Acceptance Criteria**:
  - Every service + interface lives under its feature folder within `Application/`; DI registrations updated with `using` changes only.
  - Done in small per-feature batches so git history stays bisectable.
- **Priority**: Should
- **Related Stories**: TBD

### FR-4 (P16 — folded into P21): Domain layer extraction (no new project)
- **Description**: Move the pure-functional helpers (`OrderStatusMachine`, `VatCalculator`, `StorageKeys`, `InvoiceNumber`, `PromotionOutcome`, `PurgeOutcome`) into a `Domain/` namespace. **Realized as P21-PR1.** Boundary rule: nothing in `Domain/` may reference EF Core or `System.Net.Http`.
- **Acceptance Criteria**:
  - The 6 listed types moved under `Domain/<area>/`; namespaces updated (mechanical `using static` find/replace in tests).
  - Banned-API rule (or CONTRIBUTING note) forbids EF Core / HttpClient references inside `Domain/`.
- **Priority**: Could
- **Related Stories**: TBD

### FR-5 (P23): `Abstractions/` subfolder per feature
- **Description**: Resolve the interface↔implementation interleaving (the maintainer's "interfaces and classes in the same place" complaint) by moving every `I*.cs` into an `Abstractions/` subfolder within its `Application/<Feature>/` folder. Implementations stay at the feature root. (Chosen over status-quo and over consumer-side DIP — there is no second implementation that would justify the indirection.)
- **Acceptance Criteria**:
  - All `I*.cs` relocated to `Abstractions/`; namespaces shift to `...<Feature>.Abstractions`; cross-feature consumers reference the `Abstractions` namespace.
  - DI registrations need only `using` changes; `dotnet build && dotnet test` green per batch.
- **Priority**: Should
- **Related Stories**: TBD

### FR-6 (P24): "No repositories" policy doc + `IQueryable` analyzer rule
- **Description**: Take an explicit position: keep the direct-`DbContext` posture (no repository pattern), document it, and enforce the one property that protects it — no service public method may return `IQueryable<T>` (grep confirms this holds today; lock it). Write `memory-bank/standards/data-access-conventions.md`; add a `BannedApiAnalyzers` rule.
- **Acceptance Criteria**:
  - Convention doc covers: services inject `PhotoPrintDbContext` directly; no `IQueryable<T>` in public signatures (materialise inside the service); duplicate query shapes extracted only at 3+ call sites; cross-service `SaveChangesAsync` coordination documented per-handler when load-bearing.
  - Analyzer flags any `IQueryable<T>` return in `Application/.../Services/*.cs` and `Abstractions/I*.cs`; if it fails on existing code, the leak is fixed (good outcome).
  - Linked from `system-architecture.md`.
- **Priority**: Should
- **Related Stories**: TBD

### FR-7 (P25): Handler-per-use-case pattern (no MediatR; folds P11)
- **Description**: Introduce a 30-LOC `ICommandHandler<TCommand,TResult>` + `IEventDispatcher<TEvent>` (no MediatR — avoids a tracked dependency and its relicensing surface). Migrate the four multi-step use cases: `CreateOrderCommand`/Handler (extracts the 145-LOC `CreateFromCartAsync`), `OrderPaidEvent`/Dispatcher (= P11), `RetryInvoiceUploadCommand`/Handler, `PromoteOrderPhotosCommand`/Handler. Bar for a handler: 3+ concerns or 50+ LOC — single-statement actions stay as-is.
- **Acceptance Criteria**:
  - Handler/dispatcher interfaces defined in `Application/Shared/Abstractions/`.
  - The four target use cases each become a handler with its own test file; the corresponding service methods delegate as one-liners; `OrderServiceTests.cs` shrinks proportionally.
  - No behaviour change; payment/webhook integration suite green.
- **Priority**: Should
- **Related Stories**: TBD

### FR-8 (P11 — folded into P25): `OrderPaidEventDispatcher`
- **Description**: Dedupe the identical post-Paid side-effect fan-out duplicated in `WebhooksController` for Stripe and EuPlatesc (create invoice → save → metric → SignalR broadcast → confirmation email → enqueue cloud promotion → notify AWB). **Realized as the canonical first handler in P25.** Both webhook handlers become verify-signature → transition order → `DispatchAsync(OrderPaidEvent)`.
- **Acceptance Criteria**:
  - Single `DispatchAsync(Order, CancellationToken)` (or `OrderPaidEvent`) owns the fan-out; both webhook paths call it.
  - Side-effect ordering documented as a load-bearing contract (invoice INSERT before SignalR broadcast — ADR-020) in XML docs; unit test asserts the order.
- **Priority**: Should
- **Related Stories**: TBD

---

## Non-Functional Requirements

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| No behaviour change | Test suite | 941/948 baseline maintained after every PR |
| No schema drift | `Add-Migration` verify | Empty up/down |

### Maintainability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Use-case discoverability | `find Application -name '*Handler.cs'` | Returns the full use-case inventory |
| Layer-rule enforcement | Analyzer/review | Violations blocked at PR |

---

## Constraints

### Technical Constraints
- **Sequencing is the dominant risk**: P22 → P21-PR1..PR5 → P23 → P24 → P25. Each PR must build + test green in isolation.
- Must track intent **028 (test architecture)** in lockstep — every structural PR breaks ~25 test files otherwise.
- Plan for ~1.5–2 weeks of frozen/coordinated feature work, or land in a quiet window, to avoid merge hell with in-flight bolts.
- Roslyn rules depend on `Microsoft.CodeAnalysis.BannedApiAnalyzers`; if analyzers are deemed overkill, rules degrade to CONTRIBUTING.md + code review.

### Business Constraints
- **Post-launch / non-blocking**: pure refactor, no behaviour change. High value as the codebase grows, but not a launch blocker. Doing it under deploy pressure is strictly worse than doing it in a quiet window.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| No in-flight bolt is rewriting the same files during the migration | Merge-conflict hell | Coordinate; pre-write namespace find/replace scripts per PR |
| The "no repositories" posture remains correct at this scale | A real need for repositories emerges | Documented revisit trigger; analyzer surfaces leaks early |
| Handler pattern won't be over-applied | CRUD endpoints become needless handlers | Hard bar: handler only at 3+ concerns or 50+ LOC |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Adopt Roslyn analyzers, or rely on CONTRIBUTING.md + review for layer rules? | Maintainer | 2026-06-26 | Recommend analyzer for the `IQueryable` + `Domain`-EF rules; review checklist for the rest |
| Q2: Confirm zero behaviour change is acceptable as the sole success bar (no new features in this intent)? | Maintainer | 2026-06-26 | Recommend yes — keep refactor and feature work separate |
| Q3: Which quiet window absorbs the ~2-week freeze? | Maintainer | 2026-06-26 | Pending roadmap |
