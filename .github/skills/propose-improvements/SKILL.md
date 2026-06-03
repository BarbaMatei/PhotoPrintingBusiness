---
name: propose-improvements
description: >
  Synthesises the outputs of all previous analysis skills into exactly 20 ranked
  improvement proposals. Each proposal includes complexity, business impact,
  implementation steps, schema/API changes, effort estimate, and risks.
  Run this skill last, after detect-gaps.
allowed-tools:
  - read_file
  - create_file
---

# Improvement Proposer

## How to generate proposals

You have completed the architecture scan, workflow inference, and gap analysis. Now synthesise everything into exactly 20 proposals.

### Scoring formula

Every proposal must have:
- `complexity`: 1 (trivial, hours) to 5 (multi-quarter, major rework)
- `business_impact`: 1 (minor polish) to 5 (transformative, directly drives revenue or retention)
- `priority_score`: calculated as `(business_impact × 3) + ((6 - complexity) × 2)`

The maximum possible score is 25. Sort proposals by `priority_score` descending.

### Category taxonomy

Assign exactly one category to each proposal:
- `feature` — new user-facing capability that does not currently exist
- `fix` — something broken or dangerously incomplete
- `security` — closes a security vulnerability or compliance gap
- `scalability` — removes a performance or reliability bottleneck
- `ops` — improves observability, deployment, or developer experience
- `refactor` — internal improvement with no direct user-facing change

### What makes a good proposal

**Specific, not generic.** Instead of "add caching", write "add Redis caching for the `GET /products` endpoint which currently runs a full-table scan with no cache headers — cache TTL 5 minutes, invalidate on any product update."

**Grounded in what you found.** Reference actual files, table names, and route paths from your earlier analysis.

**Aligned with the existing stack.** If the project uses TypeScript + Prisma + PostgreSQL, your schema changes must use Prisma syntax and PostgreSQL features. Do not suggest MongoDB if they use PostgreSQL.

**Include real diffs.** For schema changes, write the actual Prisma model addition or SQL ALTER TABLE. For API changes, write the new endpoint definition.

## Proposal template

For each of the 20 proposals, use this exact structure:

---

### #[rank] — [Title]

| Field | Value |
|---|---|
| Category | `feature` / `fix` / `security` / `scalability` / `ops` / `refactor` |
| Complexity | [1-5] — [1 sentence justification] |
| Business impact | [1-5] — [1 sentence justification] |
| Priority score | [(business_impact × 3) + ((6 - complexity) × 2)] |
| Estimated effort | [X developer-days] |
| Affects | [comma-separated list of files, routes, or tables] |

**What and why**
[2-3 sentences describing the problem found and the proposed solution. Be specific — name the routes, tables, and files.]

**Implementation steps**
1. [Concrete step]
2. [Concrete step]
3. [Concrete step]
(add more if needed)

**Schema / API changes**
```
[Actual Prisma schema diff, SQL ALTER TABLE, or OpenAPI endpoint definition — or write "None" if not applicable]
```

**Risks**
- [Risk 1]
- [Risk 2]

---

## After all 20 proposals

Append a **Roadmap** section with three horizons:

### Now (< 2 weeks) — Quick wins
List proposals with `priority_score ≥ 20` AND `complexity ≤ 2`. These are high-impact, low-effort. Do them first.

### Next (2–8 weeks) — Main sprint
List proposals with `priority_score 14–19` OR `complexity 3`. These require planning but should be prioritised.

### Later (> 8 weeks) — Strategic
Everything else. These are important but require dedicated resourcing.

## Reminders

- Proposals must be grounded in actual findings — do not invent problems
- Do not repeat the same root cause as two separate proposals — combine them
- Security and `fix` proposals should always rank higher than cosmetic features if their scores are equal
- The roadmap must account for dependencies between proposals (e.g. "add auth" before "add per-user rate limiting")
