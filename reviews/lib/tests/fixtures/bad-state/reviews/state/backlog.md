---
type: review-backlog
updated: 2026-08-11
---

# Backlog — unfixed minors from closed targets

Deliberately broken fixture: one row per rule the state lint enforces on this
file. Every violation here is asserted by run-tests.mjs.

| ID | Target | Sev | What | Area |
|---|---|---|---|---|
| PPW-9301 | 901-good-target | 🟡 | The Area cell is missing altogether |
| BUG-2 | 901-good-target | 🟡 | An old-style key survives the re-key | `uploads` |
| PPW-9302 | 901-good-target | High | The severity is a word, not one of the four | `uploads` |
| PPW-9303 | 901-good-target | 🟡 |  | `uploads` |
| PPW-9304 | 901-good-target | 🟡 | The area cell holds two areas | `storage/gallery` |
| PPW-9305 | 901-good-target | 🟡 | The What cell <br> runs onto a second line | `uploads` |
