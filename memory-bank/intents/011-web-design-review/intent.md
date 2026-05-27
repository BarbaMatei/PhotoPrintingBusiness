---
intent: 011-web-design-review
phase: workflow
type: meta-intent
skill: web-design-reviewer
status: active
created: 2026-05-25T00:00:00Z
---

# Intent: 011-web-design-review

## Purpose

A reusable meta-intent that encapsulates the full live web design review workflow for the FotoTipar application. Trigger this intent in any session to systematically audit the running UI, capture screenshots, analyse source code, and produce planning artifacts (bolts + intent) for all identified issues.

## When to Trigger

Trigger this intent when:
- A significant feature bolt has been completed and the UI needs a holistic quality pass
- A new breakpoint or device category needs validation
- Design inconsistencies are suspected across multiple pages
- A sprint or milestone ends and a visual regression check is due

---

## Workflow

### Phase 1 — Environment Setup

1. Confirm the API server is running (default: `https://localhost:7001`)
2. Confirm the Angular dev server is running (default: `http://localhost:4200`)
3. If either is not running, start them:
   - API: `dotnet run --project src/PhotoPrint.API`
   - UI: `cd src/PhotoPrint.UI && ng serve`
4. Open a browser page at `http://localhost:4200`

### Phase 2 — Page Navigation & Screenshots

Navigate to every page listed below in sequence. For each page:
1. Screenshot the page at **desktop** (1440px)
2. Screenshot at **tablet** (900px)
3. Screenshot at **mobile** (375px)
4. Note any visual anomalies, broken layouts, or UX inconsistencies

**Page list:**
| Route | Auth Required | Notes |
|-------|--------------|-------|
| `/` | No | Home / landing |
| `/tipareste` | No | Product catalog |
| `/tipareste/:id` | No | Product detail |
| `/inregistrare` | No | Register |
| `/autentificare` | No | Login |
| `/cos` | Yes/Guest | Cart |
| `/checkout` | Yes/Guest | Checkout (all steps) |
| `/comenzi` | Yes | Order history |
| `/comenzi/:id` | Yes | Order detail |
| `/cont` | Yes | Account / profile |
| `/admin` | Admin | Admin dashboard |
| `/admin/comenzi` | Admin | Admin orders |
| `/admin/comenzi/:id` | Admin | Admin order detail |
| `/admin/produse` | Admin | Admin products |

### Phase 3 — Source Code Analysis

For each identified visual issue:
1. Open the relevant component file(s) using `read_file` or `grep_search`
2. Identify the root cause (missing style, wrong breakpoint, structural anti-pattern, etc.)
3. Classify as:
   - **Already Fixed** — issue existed but was resolved during this session
   - **Quick Fix** — can be applied inline during the review
   - **Bolt Required** — requires a planned bolt for construction

### Phase 4 — Skill Invocation

Read and follow the `web-design-reviewer` skill:

```
d:\photo printing website\.github\skills\web-design-reviewer\SKILL.md
```

The skill guides:
- How to interpret screenshots
- What design patterns to look for
- How to classify severity (P1 critical / P2 structural / P3 minor)
- How to write clear issue descriptions with root cause and fix guidance

### Phase 5 — Artifact Creation

For every **Bolt Required** issue found:

1. Assign the next available bolt number (`ls memory-bank/bolts/` to find the highest)
2. Determine which intent the bolt belongs to (existing or new `012+`)
3. Create `memory-bank/bolts/NNN-bolt-name/bolt.md` following the existing bolt structure
4. If a new intent is needed, create its `requirements.md`, `units.md`, and `inception-log.md`
5. Update `memory-bank/story-index.md` with the new bolt entries

---

## Skill Reference

**Skill file**: `d:\photo printing website\.github\skills\web-design-reviewer\SKILL.md`

The `web-design-reviewer` skill provides:
- Step-by-step browser navigation instructions
- Screenshot capture and analysis guidelines
- Issue severity classification matrix
- Source code inspection checklist (SCSS, component templates, breakpoint mixins)
- Fix vs bolt decision criteria
- Output format for design review findings

---

## Output Format

After each review session, produce:

### Session Summary
```markdown
## Web Design Review — [DATE]

### Pages Reviewed: [N]
### Issues Found: [N]
### Issues Fixed Inline: [N]
### Bolts Created: [N]

### Summary Table
| Issue | Severity | Action | Bolt |
|-------|----------|--------|------|
| ...   | P2       | Bolt   | 027  |
```

### For each Bolt Required issue:
- Bolt number and name
- Short description of the issue
- Root cause (file + line reference)
- Proposed fix

---

## Reference: Previous Review Sessions

| Date | Bolts Created | Notes |
|------|---------------|-------|
| 2026-05-25 | 027–032 | First review; 6 issues found, 3 applied inline, 6 bolted |
