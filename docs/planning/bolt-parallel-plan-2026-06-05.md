# Bolt Parallel Implementation Plan — 2026-06-05

> Author: bolt-parallel-planner. Repo: photo-printing e-commerce (.NET 8 API + Angular + PostgreSQL/EF Core).
> Scope: every **planned, unimplemented** bolt remaining in `memory-bank/bolts/`.
> Branch state at planning time: `analysis/architect-review` (43 commits ahead of `main`, `main` is a strict ancestor).
> **How to execute this plan**: see `docs/planning/agent-commands.md` — the command reference for the planner and orchestrator agents.

---

## 0. CRITICAL PRE-FLIGHT — main is stale, fast-forward it first

This is the single most important finding. **Do not cut any worktree from `main` until this is done.**

- `main` is **43 commits behind** `analysis/architect-review` and **0 commits ahead** — i.e. `main` is a strict ancestor (`git merge-base --is-ancestor main analysis/architect-review` → true). A fast-forward is possible with zero conflicts.
- Everything since the last `main` push lives only on `analysis/architect-review`: shipped bolts **035, 036, 038, 039, 040, 041, 042, 043, 044, 045** (505 files, ~43.8k insertions), **plus** the deprioritization of 046.
- The inception artifacts for bolts **054–069** (and intents 025–031, and `docs/analysis/architect-review-2026-06-03*.md`) are **UNCOMMITTED untracked files** on `analysis/architect-review`. A worktree cut from today's `main` would contain **none of these bolt definitions** — the instances would have nothing to read.

**Therefore, before Wave 1, run this once (PR-based — per user decision, no direct
fast-forward/push to main):**

```powershell
# 1. Commit the uncommitted inception artifacts (+ plan + agents) on the analysis branch
git -C "D:\photo printing website" add memory-bank/ docs/analysis/ docs/planning/ .claude/agents/
git -C "D:\photo printing website" status --short    # sanity-check what is staged
git -C "D:\photo printing website" commit -m @'
docs(memory-bank): inception artifacts for architect-review intents 025-031 (bolts 054-069)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>
'@
git -C "D:\photo printing website" push -u origin analysis/architect-review
```

```text
# 2. On GitHub, open ONE PR: analysis/architect-review → main.
#    main is a strict ancestor, so the PR merges with ZERO conflicts.
#    Review it once (it is the 43 commits already reviewed piecewise in the cascaded PRs),
#    then merge with a MERGE COMMIT (not squash — preserves the per-bolt history).
#    https://github.com/BarbaMatei/PhotoPrintingBusiness/compare/main...analysis/architect-review?expand=1

# 3. After the PR merges: the old cascaded feat/bolt-* PRs/branches are redundant —
#    close/delete them. Then locally:
#      git -C "D:\photo printing website" switch main
#      git -C "D:\photo printing website" pull origin main
```

Every worktree command below assumes `main` now contains bolts 054–069 (the orchestrator
verifies this with `git ls-tree origin/main` before launching any wave).

> The previously-merged `feat/bolt-035 … feat/bolt-045` branches are all already contained in `main` after the fast-forward. They can be deleted (`git branch -d`, `git push origin --delete`) as cleanup — none are in-flight work.

---

## 1. Inventory & drift findings

Candidate = `status: planned` in `bolt.md` frontmatter, cross-checked against `git log --all` and `git branch -a`.

| Bolt | Intent | Frontmatter status | Git evidence | Verdict |
|------|--------|--------------------|--------------|---------|
| 047-coupon-domain-and-api | 022 | `planned` | no feat commit, no branch | **PLAN — fresh work** |
| 048-coupon-frontend | 022 | `planned` | none | **PLAN — fresh work** |
| 054-dependency-and-boot-hardening | 025 | `planned` | none (created 2026-06-05) | **PLAN — fresh work** |
| 055-boot-composition-and-flags | 026 | `planned` | none | **PLAN — fresh work** |
| 056-system-manifest-and-liveness | 026 | `planned` | none | **PLAN — fresh work** |
| 057-architecture-and-standards-docs | 026 | `planned` | none | **PLAN — fresh work** |
| 058-observability-boot-manifest-ui | 026 | `planned` | none | **PLAN — fresh work** |
| 059-layering-foundation | 027 | `planned` | none | **PLAN — fresh work (structural refactor)** |
| 060-conventions-and-policy | 027 | `planned` | none | **PLAN — fresh work** |
| 061-handler-pattern | 027 | `planned` | none | **PLAN — fresh work** |
| 062-test-infrastructure | 028 | `planned` | none | **PLAN — fresh work** |
| 063-access-hardening | 029 | `planned` | none | **PLAN — fresh work** |
| 064-service-decomposition | 029 | `planned` | none | **PLAN — fresh work** |
| 065-persistence-config | 029 | `planned` | none | **PLAN — fresh work** |
| 066-ci-quality-gates | 030 | `planned` | none | **PLAN — fresh work** |
| 067-ui-scaling-and-e2e-ui | 030 | `planned` | none | **PLAN — fresh work** |
| 068-refund-domain-and-api | 031 | `planned` | none | **PLAN — fresh work (DDD bolt)** |
| 069-refund-return-flow-ui | 031 | `planned` | none | **PLAN — fresh work** |

### Drift findings (reported, not silently resolved)

1. **`story-index.md` lists bolts 038, 039, 044, 045 stories as `⬜ NOT STARTED`** (lines 790–826, 956–986) **— they have SHIPPED.** Git log shows `feat(invoicing): bolt 038`, `feat(invoicing): bolt 039`, `feat(observability): bolt 044`, `feat(observability): bolt 045`, and the code exists in-tree (`Controllers/InvoicesController.cs`, `Controllers/AdminInvoicesController.cs`, `Migrations/20260603101910_AddVatAndInvoices.cs`, OTel/Sentry wiring). This is the *exact* drift class the protocol warns about. **None of these are scheduled as work.** **Recommended index fix:** flip intents 016 + 020 story lines from `⬜ NOT STARTED` to `✅ IMPLEMENTED` and update the "Bolts planned (not built)" header line which still implies 044/045 work. (The index's *overview* header already says 016/020 shipped — only the per-story lines are stale, a partial-edit drift.)

2. **Unmerged `feat/bolt-*` branches are NOT in-flight work.** `feat/bolt-035/036/038/040/041/042/043/045` all show as "not merged into main" only because **main itself is stale**. They are **fully merged into `analysis/architect-review`** (`git branch --merged analysis/architect-review` lists all of them). After the §0 fast-forward they are all contained in `main`. **No partial-bolt review is needed; safe to delete post-fast-forward.**

3. **`feat/bolt-044-error-tracking`-style branch does not exist** — only `feat/bolt-045-error-tracking-slos` exists, and 045 shipped. The kickoff hint in the task ("unmerged branches like feat/bolt-045-error-tracking-slos") is accounted for: it is merged, not in-flight.

4. **`story-index.md` does not yet list bolt 050** (noted in the index itself as "unallocated, no directory exists"). Not a candidate; no action.

5. **Self-consistency check of the dependency graph passed** — every `requires_bolts` entry on a planned bolt points either to a shipped bolt (038, 039, 014, 015 — all verified in-tree) or to an earlier planned bolt scheduled in an earlier wave. No frontmatter requires a bolt that doesn't exist. No contradictions found.

---

## 2. Exclusions

| Bolt | Reason | Action |
|------|--------|--------|
| **046-distributed-state-redis** (intent 021) | **⏸ Deprioritized 2026-06-03** — Redis multi-replica backplane. Scaling infrastructure that only pays off with multiple API instances; app not yet deployed, single server fits foreseeable traffic. ADRs 010/013/015 explicitly accept the single-server trade-offs. Confirmed in `story-index.md` line 8/992 and in user memory (`project_bolt_046_deprioritized`). | **Not scheduled.** Revisit only on real scaling pressure / zero-downtime-deploy / multi-region need. |

No in-flight bolts to carry as prerequisites (see Drift finding 2).

---

## 3. Groups

Eleven groups across the 18 candidate bolts. Branch naming follows the repo convention `feat/bolt-<NNN>-<slug>` (single) / `feat/bolts-<NNN>-<NNN>-<theme>` (multi), verified against `git branch -a`.

> **EF-migration legend:** ⚠️ = group adds an EF migration touching `PhotoPrintDbContextModelSnapshot.cs`. Only **G1 (047)** and **G9 (068)** do. They are in different waves — no migration collision anywhere in this plan.

### G1 — `feat/bolts-047-048-coupons` ⚠️ (047 migration)
- **Bolts, serial:** 047-coupon-domain-and-api → 048-coupon-frontend.
- **Theme:** coupon/promo-code domain, API, and cart UX.
- **Footprint:** *Backend* new `Coupons`/`CouponRedemptions` entities + `Orders` additions (EF migration ⚠️), `CartController`/`OrderService` redemption logic, admin coupon CRUD controller, `Program.cs` DI (append). *Frontend* cart page input, Romanian copy, summary/review/confirmation discount line, invoice PDF template line. Deps 038-VAT + 039-eFactura + 014-cart-UI + 015-order-core all **shipped** ✓.
- **Size:** medium (4 backend stories + 1 FE story; DDD bolt + simple bolt). ~2–3 days.

### G2 — `feat/bolt-054-dependency-hardening`
- **Bolts, serial:** 054 (single). Internal story order strict: P01→P02→P03→P05.
- **Theme:** dependency + boot hardening (OTel CVE patch, Central Package Management + Stripe.net unify, Renovate, ForwardedHeaders for /metrics).
- **Footprint:** *Config-heavy* — `Directory.Packages.props` (new, CPM), every `.csproj` (PackageVersion removal), `renovate.json` (new), `Program.cs` (ForwardedHeadersMiddleware — append), `DEPLOYMENT.md §14`. No EF migration. Touches all csproj files but mechanically.
- **Size:** small. ~1 day. **Enables 063.**

### G3 — `feat/bolts-055-056-058-boot-manifest`
- **Bolts, serial:** 055-boot-composition-and-flags → 056-system-manifest-and-liveness → 058-observability-boot-manifest-ui.
- **Theme:** boot composition, typed feature gate, system-info manifest + job liveness + ANAF metrics, admin System tab UI. (055→056→058 is a hard chain; keeping it on one branch keeps one instance busy serially instead of idling two.)
- **Footprint:** *Backend* `Program.cs` refactor into `Add*` extension methods (⚠️ structural-ish but additive, single-file rewrite), `FeatureFlags/*`, `Controllers/AdminSystemInfoController.cs`, `SystemInfo/*`, `IHeartbeat`, liveness health check, `FotoMetrics`, `docs/observability/slos.md`. *Frontend* `features/admin/pages/system/`. No EF migration.
- **Size:** medium. ~2–3 days. **Note the Program.cs overlap risk with G2/G6** (see matrix).

### G4 — `feat/bolt-057-architecture-docs`
- **Bolts, serial:** 057 (single).
- **Theme:** docs only — multi-replica readiness doc, tech-stack/KNOWN_FAILURES refresh, quarterly audit checklist.
- **Footprint:** *Docs-only* — `docs/architecture/*`, `tech-stack.md`, `KNOWN_FAILURES.md`, `ARCHITECTURE_AUDIT_CHECKLIST.md`. Zero code, zero conflict risk with anything.
- **Size:** small. ~0.5–1 day. Pure filler — slots into any wave with spare capacity.

### G5 — `feat/bolts-059-062-layering-and-tests` (STRUCTURAL — exclusive wave)
- **Bolts, serial:** 059-layering-foundation → 060-conventions-and-policy → 061-handler-pattern, **interleaved with** 062-test-infrastructure.
- **Theme:** the big structural move. Domain/Infrastructure/Web/Application folder+namespace reshape (~200 files), Abstractions/ convention + no-repo analyzer, handler-per-use-case, and the test-infra refactor that has to move in lockstep (the bolt notes for 062 say *"Lockstep / interleaved with intent 027 (bolts 059–061)"*; 060/061 notes say *"Lockstep with bolt 062"*).
- **Footprint:** *Structural refactor* — moves/renames namespaces across nearly the entire `PhotoPrint.API` project and the test project. This is a **group of ONE** per the protocol: it gets an exclusive wave; nothing runs beside it because every concurrent branch would have to rebase onto renamed paths.
- **Size:** large (5+2+4+4 = 15 stories, highest churn). ~4–6 days. Schedule a quiet window. **Enables 064, 065, 068.** Within the group, the bolt 059 stages mandate per-PR order: ADR → Domain → Infrastructure → Web → Application, with an **empty `Add-Migration` check after every PR** (zero schema drift).

### G6 — `feat/bolt-063-access-hardening`
- **Bolts, serial:** 063 (single).
- **Theme:** global per-IP rate limit + `Policies.Admin` constant.
- **Footprint:** *Backend* `SecurityExtensions`/`Program.cs` limiter registration (append), new `Policies` static class, 6 controllers swap `[Authorize(Roles="Admin")]` → `[Authorize(Policy=Policies.Admin)]`. **No EF migration** (verified — the index's "+migration" wording refers to *migrated controllers*, not a DB migration).
- **Size:** small. ~1 day. **Requires 054. Enables 068.**

### G7 — `feat/bolts-064-065-decomposition`
- **Bolts, serial:** 065-persistence-config and 064-service-decomposition (independent of each other; same intent + same backend area → one branch). Suggested order 065 → 064 (065 only touches `Data/`, lower risk; lands first).
- **Theme:** per-entity EF configurations (shrink `OnModelCreating`) + split AuthService into 3 + thin WebhooksController + OrderPhotoQueryService.
- **Footprint:** *Backend* `Infrastructure/Data/Configurations/*Configuration.cs` (17 files, 065), `Application/Auth/Services/*` split (064), `OrderPhotoQueryService`, thin `WebhooksController`. **065 produces an EMPTY migration** (config-only, no schema change — `Add-Migration NoOpVerify` must be empty). No real schema change.
- **Size:** medium. ~2–3 days. **Requires 059 (both) + 061 (064 only).**

### G8 — `feat/bolts-066-067-ui-scaling`
- **Bolts, serial:** 066-ci-quality-gates → 067-ui-scaling-and-e2e-ui.
- **Theme:** CI bundle-size budget + Playwright e2e smoke tests, then break up the 4 largest Angular pages + shared `BaseApiService`.
- **Footprint:** *Frontend + CI* — `angular.json` budgets, `playwright-e2e.yml` (new CI workflow), e2e specs, `base-api.service.ts`, `home-page.ts` (951 LOC) breakup, saved-addresses/profile/delivery-step breakups. **Pure frontend/CI — disjoint from all backend groups.**
- **Size:** medium. ~3 days. **066 enables 067.** Independent of every backend bolt.

### G9 — `feat/bolt-068-refund-domain` ⚠️ (068 migration)
- **Bolts, serial:** 068 (single, DDD bolt: model → design → implement → test).
- **Theme:** full server-side refund flow — schema + `OrderStatus.Refunded` (EF migration ⚠️), refund service across Stripe + EuPlatesc, ANAF credit-note (UBL 381), admin refund endpoint.
- **Footprint:** *Backend* `Application/Refunds/*`, `Infrastructure/Payments/*`, refund migration ⚠️, reuses bolt-038 `VatCalculator`, intersects bolt-039 ANAF + bolt-052 archive purge. Uses `Policies.Admin` from 063 and the layered shape from 059.
- **Size:** medium-large (DDD, regulated domain). ~3–4 days. **Requires 059 + 063. Enables 069.**

### G10 — `feat/bolt-069-refund-ui`
- **Bolts, serial:** 069 (single).
- **Theme:** admin refund action + modal on order-detail.
- **Footprint:** *Frontend* admin order-detail refund action/modal, error-code → Romanian copy. Reuses `BaseApiService` if 067 landed.
- **Size:** small. ~1 day. **Requires 068.**

> **Group-merge note:** G7 (064) also requires 061, and 068 (G9) requires 063 — these cross-group deps all point to earlier waves (see §5), never sideways.

---

## 4. Conflict matrix

Rated for groups that *could* co-occur in a wave. HIGH = same files / file-moves / dual EF migration; MED = both append to a hot shared file (trivial, append-only); LOW = disjoint.

| Pair | Rating | Reason |
|------|--------|--------|
| G1 (coupons) × G2 (deps) | **MED** | Both append to `Program.cs` DI; G1 has its own migration, G2 has none → no snapshot clash. Keep additions append-only. |
| G1 (coupons) × G3 (boot/manifest) | **MED** | Both touch `Program.cs`; G3 *rewrites* Program.cs into extensions — see special note below. G1 migration vs G3 no-migration → OK. |
| G1 (coupons) × G4 (docs) | **LOW** | Docs vs backend+FE. |
| G1 (coupons) × G8 (UI/CI) | **LOW** | G1 FE touches cart/checkout/invoice-PDF; G8 touches home/account/delivery-step + CI. Different Angular areas. |
| G2 (deps) × G3 (boot/manifest) | **MED→HIGH** | **Both edit `Program.cs`** and G2 introduces CPM editing every `.csproj` while G3 may add package refs. CPM + Program-extraction in the same wave is fragile. **Avoid co-scheduling** (handled: G2 in Wave 1, G3 in Wave 2). |
| G2 (deps) × G4 (docs) | **LOW** | Config/csproj vs docs (G2 edits `DEPLOYMENT.md §14`, G4 edits other docs — no file overlap). |
| G2 (deps) × G8 (UI/CI) | **LOW** | Backend csproj/Program vs Angular/Playwright. (G2 touches `.github/workflows` only if it adds a vuln-scan step; G8 adds `playwright-e2e.yml` — different files, append-only.) |
| G3 (boot/manifest) × G4 (docs) | **LOW** | Backend+admin-FE vs docs. |
| G3 (boot/manifest) × G8 (UI/CI) | **MED** | Both touch Angular. G3 adds `features/admin/pages/system/` (new); G8 refactors home/account/delivery-step. Disjoint components, but both may touch `app.routes` / admin route config → append-only. |
| G4 (docs) × everything | **LOW** | Docs-only. Zero code conflict. Universal filler. |
| G5 (structural) × ANY | **HIGH** | Moves/renames ~200 files across namespaces + reshapes the test project. **Exclusive wave — nothing runs beside it.** |
| G6 (access-hardening) × G7 (decomposition) | **MED** | Both post-G5 backend; G6 edits 6 controllers' `[Authorize]` attrs + Program limiter, G7 splits AuthService + Data configs. Possible overlap on AdminController attributes if 064 also moves auth controllers — keep G6's attribute swaps isolated, G7 avoids re-touching `[Authorize]` lines. No migration in either (065 empty). |
| G6 (access-hardening) × G8 (UI/CI) | **LOW** | Backend vs frontend. |
| G6 (access-hardening) × G9 (refund) | **N/A same wave** | 068 *requires* 063 → different waves by dependency. |
| G7 (decomposition) × G8 (UI/CI) | **LOW** | Backend vs frontend. |
| G7 (decomposition) × G9 (refund) | **MED** | Both post-G5 backend in `Application/`; G9 adds `Application/Refunds/*` (new namespace), G7 splits `Application/Auth/*` + edits `OnModelCreating`/Data configs. G9 has a **migration**, G7's 065 migration is **empty** → still a snapshot-ordering risk if both regenerate. **Do not co-schedule a real migration (068) with the snapshot-touching 065** — handled: G7 in Wave 4, G9 in Wave 5. |
| G8 (UI/CI) × G9 (refund) | **LOW** | Frontend/CI vs backend. |
| G9 (refund) × G10 (refund UI) | **N/A same wave** | 069 *requires* 068 → different waves. |

**`story-index.md` and all `memory-bank/*-index.md` files are guaranteed-conflict files.** Every instance is instructed (in its kickoff prompt) **not to touch them**; the index is updated once per wave at integration time.

**Special note — Program.cs:** G2, G3, G6, G1 all touch `Program.cs`. G3's bolt 055 *deliberately rewrites* Program.cs into `Add*` extension methods (~120 LOC target). To avoid a painful three-way merge, **G3 (the Program.cs rewriter) is isolated into its own wave from the other Program.cs editors** wherever possible (G2 in W1, G3 in W2, G6 after G5). Within a wave, all other Program.cs edits must be append-only single lines.

---

## 5. Wave schedule

Dependency edges (planned bolts only): `054→063`; `055→056→058`; `059→{060,061,062,065}`, `060→061`, `061→064`, `{059,061}→064`, `{059,063}→068→069`; `047→048`; `066→067`. Bolt 062 has no hard `requires` but its frontmatter mandates lockstep interleave with 059–061 → folded into G5. Bolt 057 and G8 are fully independent of the backend graph.

Six waves. Optimal width is stated per wave; the user runs 2–3 instances by default — where a wave is wider, the "if only 3 instances" deferral order is given.

---

### Wave 1 — independent quick wins (optimal width: 4; min 2)
**Branch from `main` (post §0 fast-forward).**

| Group | Branch | Bolts | Why now |
|-------|--------|-------|---------|
| G2 | `feat/bolt-054-dependency-hardening` | 054 | No deps; unblocks 063. Pre-launch Must (CVE + /metrics fix). |
| G1 | `feat/bolts-047-048-coupons` | 047, 048 | Deps (038/039/014/015) all shipped. Customer-facing value lands early. Migration ⚠️ — **the only migration in this wave.** |
| G8 | `feat/bolts-066-067-ui-scaling` | 066, 067 | Fully independent frontend/CI. Pre-launch Must (e2e). |
| G4 | `feat/bolt-057-architecture-docs` | 057 | Docs-only, zero conflict. Filler — give to whichever instance frees up, or run 4th. |

**Wave-boundary justification:** all four are LOW/MED-disjoint (matrix). Only G1 adds a migration → no snapshot collision. G2 (csproj/Program) vs G3 (Program rewrite) conflict is avoided by holding G3 for Wave 2. **If running only 3 instances:** defer **G4 (docs)** first — it can ride the tail of any instance that finishes early, or slot into Wave 2.

### Wave 2 — observability + manifest (optimal width: 1–2; min 1)
**Branch from `main` after Wave 1 fully lands.**

| Group | Branch | Bolts | Why now |
|-------|--------|-------|---------|
| G3 | `feat/bolts-055-056-058-boot-manifest` | 055, 056, 058 | 055 rewrites `Program.cs` into extensions — must NOT overlap W1's G2 Program.cs/CPM edits, hence held to W2. Self-contained chain. |
| G4 | `feat/bolt-057-architecture-docs` | 057 | *Only if not done in W1.* Docs-only, rides alongside G3 with zero conflict. |

**Wave-boundary justification:** G3 is the Program.cs rewriter; isolating it from the other Program.cs editors (G2 in W1, G6 in W3) keeps the rewrite a clean single-author change. G4 is the only safe co-runner (docs). **Optimal: 2 instances** (G3 + G4) or **1** if G4 already shipped in W1.

### Wave 3 — STRUCTURAL REFACTOR (exclusive; width: 1)
**Branch from `main` after Wave 2 fully lands. NOTHING else runs in this wave.**

| Group | Branch | Bolts | Why exclusive |
|-------|--------|-------|---------------|
| G5 | `feat/bolts-059-062-layering-and-tests` | 059, 060, 061, 062 | Moves/renames ~200 files across Domain/Infrastructure/Web/Application namespaces and reshapes the test project. Any concurrent branch would be forced to rebase onto renamed paths → merge hell. Protocol mandates a group-of-one exclusive wave. |

**Wave-boundary justification:** highest-churn change in the entire backlog; the bolt notes explicitly call for "a quiet window." Internal order is strict (ADR → Domain → Infra → Web → Application, interleaving 062's test moves), with an **empty `Add-Migration` after each PR** to prove zero schema drift. Every later wave branches from the post-merge `main`. **Optimal: 1 instance.**

### Wave 4 — post-refactor backend hardening + decomposition (optimal width: 2; min 2)
**Branch from `main` after Wave 3 fully lands (renamed namespaces now in main).**

| Group | Branch | Bolts | Why now |
|-------|--------|-------|---------|
| G6 | `feat/bolt-063-access-hardening` | 063 | Requires 054 (W1) ✓ and the layered shape (W3) ✓. Unblocks 068. |
| G7 | `feat/bolts-064-065-decomposition` | 065, 064 | Requires 059 (W3) ✓ + 061 (W3) ✓. 065 empty-migration only. |

**Wave-boundary justification:** both depend on the structural refactor → cannot precede Wave 3. G6 × G7 is MED (both backend `Application/`, both may touch `[Authorize]`/controllers) — allowed in the same wave with the append-only / no-double-touch note. Neither adds a *real* migration (065's is empty). **Optimal: 2 instances.**

### Wave 5 — refund domain (optimal width: 1; min 1)
**Branch from `main` after Wave 4 fully lands.**

| Group | Branch | Bolts | Why now |
|-------|--------|-------|---------|
| G9 | `feat/bolt-068-refund-domain` | 068 | Requires 059 (W3) ✓ + 063 (W4) ✓. Adds a real EF migration ⚠️ — must not co-run with G7's snapshot-touching 065 (W4), hence held to W5. |

**Wave-boundary justification:** hard dependency on 063 (W4) plus migration-isolation from W4's 065 snapshot edit. Solo backend wave. **Optimal: 1 instance** (G10 could *not* start yet — it requires 068's endpoint). If the user wants to fill the second instance, the only remaining work is anything deferred from earlier waves (e.g. G4 docs if still open) — otherwise leave it idle; padding here invites a 068↔unrelated migration clash.

### Wave 6 — refund UI (width: 1)
**Branch from `main` after Wave 5 fully lands.**

| Group | Branch | Bolts | Why now |
|-------|--------|-------|---------|
| G10 | `feat/bolt-069-refund-ui` | 069 | Requires 068 (W5) ✓. Frontend-only. |

**Wave-boundary justification:** pure dependency tail. **Optimal: 1 instance.** Trivial; could also be appended to the G9 instance after 068's PR merges if you prefer one fewer context switch.

> **Priority note:** Must-priority / customer-facing value (coupons G1, CVE/hardening G2, e2e G8, refund G9/G10) is front-loaded as far as the dependency graph allows. The Could/Should structural work (G5, G7) sits mid-plan because 064/065/068 *cannot* start until it lands.

---

## 6. Worktree setup

All commands are Windows-PowerShell-ready. Repo path has spaces — always quoted. Worktrees live in a sibling `D:\worktrees\` so they never nest inside the repo. **Run each wave's block only after the previous wave has fully merged to `main`** (so the new worktree branches off the updated `main`).

### Pre-flight (once — see §0 for the full PR-based flow)
```powershell
git -C "D:\photo printing website" add memory-bank/ docs/analysis/ docs/planning/ .claude/agents/
git -C "D:\photo printing website" commit -m "docs(memory-bank): inception artifacts for architect-review intents 025-031 (bolts 054-069)"
git -C "D:\photo printing website" push -u origin analysis/architect-review
# then: open + review + merge the PR analysis/architect-review → main on GitHub (see §0)
# then: git switch main; git pull origin main
```

### Wave 1
```powershell
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-054"      -b feat/bolt-054-dependency-hardening  main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-047-048" -b feat/bolts-047-048-coupons          main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-066-067" -b feat/bolts-066-067-ui-scaling       main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-057"      -b feat/bolt-057-architecture-docs     main
```

### Wave 2 (after Wave 1 merged to main)
```powershell
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-055-056-058" -b feat/bolts-055-056-058-boot-manifest main
# only if G4 (057) was NOT done in Wave 1:
# git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-057" -b feat/bolt-057-architecture-docs main
```

### Wave 3 (after Wave 2 merged to main) — EXCLUSIVE
```powershell
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-059-062" -b feat/bolts-059-062-layering-and-tests main
```

### Wave 4 (after Wave 3 merged to main)
```powershell
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-063"      -b feat/bolt-063-access-hardening    main
git -C "D:\photo printing website" worktree add "D:\worktrees\bolts-064-065" -b feat/bolts-064-065-decomposition  main
```

### Wave 5 (after Wave 4 merged to main)
```powershell
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-068" -b feat/bolt-068-refund-domain main
```

### Wave 6 (after Wave 5 merged to main)
```powershell
git -C "D:\photo printing website" worktree add "D:\worktrees\bolt-069" -b feat/bolt-069-refund-ui main
```

### Cleanup (after each branch's PR merges)
```powershell
git -C "D:\photo printing website" worktree remove "D:\worktrees\bolt-054"
git -C "D:\photo printing website" worktree prune
```

---

## 7. Kickoff prompts

One self-contained block per instance per wave. Paste as the first message of a fresh Claude Code instance launched **in that worktree directory**.

### Wave 1 — Instance A (G2 / 054)
```
You are implementing bolt group dependency-hardening on branch feat/bolt-054-dependency-hardening in this worktree.

Bolts, in strict order:
1. 054-dependency-and-boot-hardening — read memory-bank/bolts/054-dependency-and-boot-hardening/bolt.md first. Internal story order is STRICT: P01 (patch-otel-cve) → P02 (central-package-management) → P03 (renovate-config) → P05 (forwarded-headers-metrics).

For each bolt follow its staged process (plan → implement → test): write implementation-plan.md before coding, implement the stories in order, then write the test report. Mark stage checkboxes and frontmatter as you complete them.

Conflict rules for this wave (other instances are working in parallel on coupons, UI-scaling, and docs):
- Do NOT touch: any Angular/frontend files, any EF migration, Controllers business logic.
- Do NOT edit story-index.md or any memory-bank/*-index.md (updated at merge time).
- Your Program.cs change is ONLY the ForwardedHeadersMiddleware registration — keep it append-only and minimal. Do NOT refactor Program.cs into extension methods (that is bolt 055 in a later wave).
- Central Package Management WILL edit every .csproj (remove inline Version=, add Directory.Packages.props). Keep Stripe.net unified to one version.
- No EF migration in this bolt.

Done means: all stories implemented, `dotnet list package --vulnerable` clean, restore succeeds with one resolved version per package, full test suite green (dotnet test), DEPLOYMENT.md §14 updated, bolt.md status/stages updated, branch pushed, PR opened against main (gh pr create). Do NOT merge the PR — merge order is coordinated centrally.
```

### Wave 1 — Instance B (G1 / 047 + 048)
```
You are implementing bolt group coupons on branch feat/bolts-047-048-coupons in this worktree.

Bolts, in strict order:
1. 047-coupon-domain-and-api — read memory-bank/bolts/047-coupon-domain-and-api/bolt.md first. DDD bolt: model → design → implement → test. The concurrent-redemption integration test is the single most important guarantee — gate the bolt on it. Document discount-then-VAT math in decision-index.md.
2. 048-coupon-frontend — read memory-bank/bolts/048-coupon-frontend/bolt.md. Simple bolt: plan → implement → test.

Follow each bolt's staged process; write the plan/design docs before coding, implement stories in order, write the test report. Mark stage checkboxes + frontmatter as you go.

Conflict rules for this wave (others are working on dependency-hardening, UI-scaling, docs):
- Do NOT touch: Directory.Packages.props / .csproj package versions (Instance A owns CPM), home-page.ts / saved-addresses / profile / delivery-step (Instance C owns those).
- Do NOT edit story-index.md or any memory-bank/*-index.md.
- YOU OWN THE ONLY EF MIGRATION IN THIS WAVE. Generate it normally (Add-Migration). No coordination needed within the wave, but note it for the merge plan.
- Keep Program.cs DI additions append-only and minimal.
- Your frontend work is the cart/checkout/invoice-PDF area only.

Done means: all stories implemented, concurrent-redemption test green, full test suite green (dotnet test + ng build/test), bolt.md status/stages updated for BOTH bolts, branch pushed, PR opened against main. Do NOT merge the PR.
```

### Wave 1 — Instance C (G8 / 066 + 067)
```
You are implementing bolt group ui-scaling on branch feat/bolts-066-067-ui-scaling in this worktree.

Bolts, in strict order:
1. 066-ci-quality-gates — read memory-bank/bolts/066-ci-quality-gates/bolt.md first. angular.json bundle budgets + 3 Playwright e2e smoke tests (guest checkout, admin login, real-time SignalR) + playwright-e2e.yml CI workflow.
2. 067-ui-scaling-and-e2e-ui — read memory-bank/bolts/067-ui-scaling-and-e2e-ui/bolt.md. Shared BaseApiService + break up home-page.ts (951 LOC), saved-addresses, profile, delivery-step. One PR-sized commit per page.

Follow each bolt's staged process (plan → implement → test).

Conflict rules for this wave (others are on dependency-hardening, coupons, docs):
- Do NOT touch: any backend (.cs) files, .csproj, Program.cs, EF migrations.
- Do NOT touch cart/checkout/invoice frontend (Instance B owns those).
- Do NOT edit story-index.md or any memory-bank/*-index.md.
- If you add a CI workflow, add a NEW file (playwright-e2e.yml) — do not edit the existing CI workflow inline beyond an append.
- No EF migration.

Done means: budgets enforced (build fails over budget), 3 e2e pass in CI, all 4 pages broken up + routed through BaseApiService, no home visual regression, bundle within budget, ng build/test + e2e green, bolt.md status/stages updated for BOTH bolts, branch pushed, PR opened against main. Do NOT merge.
```

### Wave 1 — Instance D (G4 / 057) — optional 4th; otherwise defer to Wave 2
```
You are implementing bolt group architecture-docs on branch feat/bolt-057-architecture-docs in this worktree.

Bolts, in strict order:
1. 057-architecture-and-standards-docs — read memory-bank/bolts/057-architecture-and-standards-docs/bolt.md first. Docs only: multi-replica-readiness doc (consolidate ADRs 010/013/015/016/023), refresh tech-stack.md + KNOWN_FAILURES.md (7 failures), quarterly ARCHITECTURE_AUDIT_CHECKLIST.md.

Follow the staged process (plan → implement → review). Verify every claim against the actually-installed dependencies before writing it.

Conflict rules for this wave:
- Do NOT touch any code, .csproj, or Program.cs.
- Do NOT touch DEPLOYMENT.md §14 (Instance A owns that this wave) — your docs live under docs/architecture/, tech-stack.md, KNOWN_FAILURES.md, ARCHITECTURE_AUDIT_CHECKLIST.md.
- Do NOT edit story-index.md or any memory-bank/*-index.md.

Done means: all three docs written and self-consistent with installed deps, bolt.md status/stages updated, branch pushed, PR opened against main. Do NOT merge.
```

### Wave 2 — Instance A (G3 / 055 + 056 + 058)
```
You are implementing bolt group boot-manifest on branch feat/bolts-055-056-058-boot-manifest in this worktree.

Bolts, in strict order (hard chain):
1. 055-boot-composition-and-flags — read memory-bank/bolts/055-boot-composition-and-flags/bolt.md first. Extract Program.cs into ~5 Add* subsystem extension methods (target ~120 LOC Program.cs) + typed IFeatureGate registry. Order P07 → P10.
2. 056-system-manifest-and-liveness — /api/admin/system-info manifest + background-job liveness health check + invoice_upload metrics & ANAF SLO. Manifest reads IFeatureGate.GetAll(). Order P04 → P17.
3. 058-observability-boot-manifest-ui — admin "System" tab (features/admin/pages/system/) rendering the manifest. Use BaseApiService if it exists in main.

Follow each bolt's staged process (plan → implement → test).

Conflict rules for this wave (only the docs instance may run beside you):
- This branch OWNS the Program.cs rewrite. No other instance touches Program.cs this wave.
- Do NOT edit story-index.md or any memory-bank/*-index.md.
- No EF migration in this group.
- New admin frontend lives under features/admin/pages/system/ — do not refactor unrelated admin pages.

Done means: Program.cs ≈120 LOC with passing ordering test, all flag reads via IFeatureGate, manifest admin-only + cached + no secrets, liveness degrades on stale heartbeat, invoice metrics present, System tab renders+searches within bundle budget, full suite green (dotnet test + ng test), bolt.md updated for all three bolts, branch pushed, PR opened against main. Do NOT merge.
```

### Wave 3 — single instance (G5 / 059 + 060 + 061 + 062) — EXCLUSIVE
```
You are implementing bolt group layering-and-tests on branch feat/bolts-059-062-layering-and-tests in this worktree. THIS IS THE EXCLUSIVE STRUCTURAL-REFACTOR WAVE — no other instance is running.

Bolts, in this order, INTERLEAVING the test-infra bolt with the layering PRs as the bolt notes require:
1. 059-layering-foundation — read memory-bank/bolts/059-layering-foundation/bolt.md first. Strict per-PR order: PR1 No-split ADR → PR2 Domain/ extraction → PR3 Infrastructure/ → PR4 Web/ → PR5 Application/<Feature>/ promotion. Use namespace find/replace scripts. After EVERY PR: build+test green AND `dotnet ef migrations add NoOpVerify` produces an EMPTY diff (then remove it) — zero schema drift is a hard gate.
2. 062-test-infrastructure — interleave: adopt TimeProvider, shared PhotoPrintTestApplicationFactory base, fluent Builders, reclassify misnamed DbContext "unit" tests to Integration. Keep the suite green as namespaces move. (Bolt notes: lockstep with 059–061.)
3. 060-conventions-and-policy — Abstractions/ subfolder per feature + no-repository policy doc + IQueryable analyzer.
4. 061-handler-pattern — ICommandHandler/IEventDispatcher + CreateOrderHandler + OrderPaidEventDispatcher (folds P11) + retry-invoice/promote-photos handlers.

Follow each bolt's staged process. This is ~200 files of churn — go PR-by-PR, verifying after each.

Conflict rules:
- Do NOT edit story-index.md or any memory-bank/*-index.md.
- Zero behaviour change, zero schema drift — assert with an empty Add-Migration after each PR.

Done means: four layers by folder+namespace, four controllers no longer inject DbContext, Abstractions/ convention enforced by analyzer, four use cases are handlers with their own tests, OrderServiceTests shrinks, TimeProvider adopted (no raw UtcNow in Application/Infrastructure), factories inherit shared base, misnamed tests reclassified, FULL suite green, bolt.md updated for all four bolts, branch pushed, PR(s) opened against main. Do NOT merge.
```

### Wave 4 — Instance A (G6 / 063)
```
You are implementing bolt group access-hardening on branch feat/bolt-063-access-hardening in this worktree.

Bolts, in strict order:
1. 063-access-hardening — read memory-bank/bolts/063-access-hardening/bolt.md first. Global per-IP sliding-window rate limiter (SecurityExtensions) + centralised Policies.Admin constant; migrate the 6 controllers from [Authorize(Roles="Admin")] to [Authorize(Policy = Policies.Admin)]. Also centralise the existing DualAuth policy.

Follow the staged process (plan → implement → test).

Conflict rules for this wave (the decomposition instance runs beside you):
- Do NOT touch: Application/Auth/* service internals, Infrastructure/Data/Configurations/* (Instance B owns those).
- You own the [Authorize] attribute swaps on the 6 controllers — Instance B must not re-touch those attribute lines.
- Do NOT edit story-index.md or any memory-bank/*-index.md.
- NO EF migration in this bolt (despite older index wording — "migrated controllers" ≠ DB migration).
- Program.cs limiter registration: append-only.

Done means: global limiter active, auth policies still stricter, no Roles="Admin" literal anywhere, anonymous admin → 401, limit tuned for legit bursts, integration tests for 401+429 green, full suite green, bolt.md updated, branch pushed, PR opened against main. Do NOT merge.
```

### Wave 4 — Instance B (G7 / 065 + 064)
```
You are implementing bolt group decomposition on branch feat/bolts-064-065-decomposition in this worktree.

Bolts, in strict order (065 first — lower risk, Data/-only):
1. 065-persistence-config — read memory-bank/bolts/065-persistence-config/bolt.md first. Extract 17 per-entity IEntityTypeConfiguration<T> files into Infrastructure/Data/Configurations/; call ApplyConfigurationsFromAssembly; shrink OnModelCreating to ≤100 LOC. HARD GATE: `dotnet ef migrations add NoOpVerify` must produce an EMPTY diff (then remove it) — zero schema change.
2. 064-service-decomposition — split AuthService into 3 services (Application/Auth/Services), extract OrderPhotoQueryService, thin the WebhooksController. Scope P14 to RESIDUALS only — do NOT re-extract CreateFromCartAsync or the OrderPaid fan-out (already done in bolt 061).

Follow each bolt's staged process (plan → implement → test).

Conflict rules for this wave (the access-hardening instance runs beside you):
- Do NOT touch: any [Authorize] attribute lines on controllers (Instance A owns the Policies.Admin swap), SecurityExtensions, the rate limiter.
- Do NOT edit story-index.md or any memory-bank/*-index.md.
- 065's migration MUST be empty (no schema change). Do NOT generate a real migration. If Add-Migration is non-empty, you've changed the model — fix it.
- Keep DI registration changes in Program.cs append-only.

Done means: one config file per entity + OnModelCreating ≤100 LOC + empty Add-Migration, 3 auth services with own tests, GetOrderPhotosAsync in OrderPhotoQueryService, webhooks free of data-access orchestration, no behaviour change, full suite green, bolt.md updated for BOTH bolts, branch pushed, PR opened against main. Do NOT merge.
```

### Wave 5 — single instance (G9 / 068)
```
You are implementing bolt group refund-domain on branch feat/bolt-068-refund-domain in this worktree.

Bolts, in strict order:
1. 068-refund-domain-and-api — read memory-bank/bolts/068-refund-domain-and-api/bolt.md first. DDD bolt: model → design → implement → test. Refund schema + OrderStatus.Refunded terminal state (EF MIGRATION), refund service full/partial across Stripe + EuPlatesc (idempotent), ANAF credit-note UBL type 381 submitted by InvoiceUploadJob, admin refund endpoint (Policy = Policies.Admin from bolt 063). Reuse bolt-038 VatCalculator. This intersects bolt 039 (ANAF) and bolt 052 (archive purge) — review those before designing.

Follow the DDD staged process; write ddd-01-domain-model.md and ddd-02-technical-design.md before implementing.

Conflict rules for this wave (solo backend wave):
- Do NOT edit story-index.md or any memory-bank/*-index.md.
- YOU OWN THE EF MIGRATION this wave — generate it normally (Add-Migration). It is the only migration in the wave.
- Place code in the layered shape: Application/Refunds/*, Infrastructure/Payments/*.

Done means: full/partial refund consistent across DB/gateway/ANAF and idempotent, credit-note (381) validates + is submitted by InvoiceUploadJob, admin-only endpoint with invalid states → 409/422, full suite green (dotnet test), bolt.md status/stages updated, branch pushed, PR opened against main. Do NOT merge.
```

### Wave 6 — single instance (G10 / 069)
```
You are implementing bolt group refund-ui on branch feat/bolt-069-refund-ui in this worktree.

Bolts, in strict order:
1. 069-refund-return-flow-ui — read memory-bank/bolts/069-refund-return-flow-ui/bolt.md first. Admin refund action + modal on the order-detail view: full/partial + reason, refunded state shown, admin-only, irreversible-action confirmation, error-code → Romanian copy. Reuse BaseApiService (landed in bolt 067).

Follow the staged process (plan → implement → test).

Conflict rules:
- Frontend-only. Do NOT touch backend.
- Do NOT edit story-index.md or any memory-bank/*-index.md.

Done means: refund action on admin order-detail wired to the bolt-068 endpoint, refunded state rendered, admin-only with confirmation, Vitest spec green + error-code→copy mapping verified, ng build/test green, bolt.md updated, branch pushed, PR opened against main. Do NOT merge.
```

---

## 8. PR merge & integration plan

Each group branch → its own GitHub PR into `main`. **PRs within a wave merge sequentially, never together.** Rule for merge order within a wave: **dependency order first, then lowest-conflict-risk first, migration-bearing branch LAST** (so it rebases onto everything else and regenerates cleanly if needed).

### Per-merge protocol (apply for EVERY PR)
1. PR's CI green (build + `dotnet test` + `ng build/test`, plus e2e/budget for G8).
2. Merge PR to `main`.
3. **Sync step:** for every still-open PR in the same wave, update its branch from the freshly-moved `main` (`git -C "<worktree>" pull --rebase origin main` or "Update branch" in the GitHub UI) and let CI re-run.
4. Run the **full test suite** on `main` after the merge before merging the next PR.
5. After the wave's last PR lands, make the **single `story-index.md` update for that wave** as one small follow-up commit on `main` (flip the wave's bolt stories to ✅ IMPLEMENTED, bump "Last updated"). No instance edits the index during the wave.
6. Only then create the next wave's worktrees (they branch off the now-updated `main`).

### Wave 1 merge order
`feat/bolt-054` (no migration, unblocks 063) → `feat/bolts-066-067` (frontend/CI, disjoint) → `feat/bolt-057` (docs, if run) → `feat/bolts-047-048` **LAST** (it carries the wave's only EF migration; landing it last means no other branch's snapshot edits precede it). Since 054/066/067/057 add no migration, 047's snapshot is conflict-free regardless — but merging it last is the safe habit.
- **Migration regeneration:** none needed (047 is the sole migration).
- Index update: flip 047, 048, 054, 066, 067, 057 stories → ✅; note coupons + e2e shipped.

### Wave 2 merge order
`feat/bolt-057` (if deferred here; docs) → `feat/bolts-055-056-058`. No migrations. Index update: flip 055, 056, 058 (+ 057 if here) → ✅.

### Wave 3 merge order
`feat/bolts-059-062` only (exclusive). This may be opened as **one PR or a stacked series of PRs** mirroring the internal PR1–PR5 order — if stacked, merge them bottom-up, running the empty-`Add-Migration` check on `main` after each. Index update: flip 059, 060, 061, 062 → ✅. **Verify a clean `Add-Migration NoOpVerify` on `main` before declaring the wave done** — this is the zero-schema-drift gate for the whole refactor.

### Wave 4 merge order
`feat/bolt-063` (no migration) → `feat/bolts-064-065` (065 produces an EMPTY migration only). 
- **Migration regeneration:** 065's migration must be empty; if the snapshot moved under it during 063's merge, regenerate (`remove the empty migration, re-run Add-Migration NoOpVerify, confirm still empty`). Neither branch carries a real schema change, so no real regeneration is expected.
- After 063 merges, rebase the 064/065 branch on `main` so its `[Authorize]` context matches the merged Policies.Admin swap. Index update: flip 063, 064, 065 → ✅.

### Wave 5 merge order
`feat/bolt-068` only. Carries a real EF migration — since it's the sole branch in the wave, no snapshot contention. Confirm `dotnet ef database update` applies cleanly on a fresh DB. Index update: flip 068 → ✅.

### Wave 6 merge order
`feat/bolt-069` only. Frontend-only. Index update: flip 069 → ✅, and update the index overview ("Bolts planned (not built)" → none remaining; backlog complete).

### Migration-collision summary (the thing that destroys parallel branches)
Only **047 (W1)** and **068 (W5)** add real EF migrations, and they are four waves apart. **No wave ever contains two migration-bearing branches.** 065's "migration" is a no-op verification only. Therefore the dreaded `PhotoPrintDbContextModelSnapshot.cs` two-branch collision **cannot occur under this schedule** — no rebase-and-regenerate dance is required at any merge.
