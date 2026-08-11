---
type: resolution
target: 042-thumbnail-cache
version: 5
answers: pass v5 (verification — index row)
status: resolved
fixed_commit: 838c9b6
closed: 2026-07-14
---

# Resolution v5 — 042-thumbnail-cache

## Findings

| D# | Status | Commit | Note |
|---|---|---|---|
| D34 | wont-fix | — | The residual window left by the D34 fix, under a symmetric interleaving. Accepted rather than patched: it is the same class as D31, whose sweep is the designed backstop. See Decisions. |
| D57 | fixed | `838c9b6` | Completed the walkthrough refresh the previous round left partly done: the provider-aware migration line, the decode-limit name and units, and the dimension text. |
| D60 | fixed | `838c9b6` | The image-processor interface comment said the thumbnail's longest side is 300 px; it now says 800 px, matching what shipped. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Document and comment completion (`838c9b6`) | D57, D60 | `memory-bank/…/implementation-walkthrough.md`, `Services/IImageProcessor.cs` | not needed (documents and one comment) |
| B — Ruled without code | D34 | — | not needed (no code changed) |

## Decisions

### The residual race was accepted rather than reopened (D34)

The D34 fix closes the ordering the finding stated. What remains is the reverse interleaving. The
preview persists the path and its re-read sees the row still live. Only then does the cleanup job
commit the soft-delete, so neither side deletes the thumbnail. That is the underlying problem of
writing a file and a row with no shared transaction. It is the same problem as D31, whose sweep is
already deferred to the cloud-storage bolt as the designed backstop. A cleanup-side unconditional
delete of the derivable key would narrow the window but not close it either, because a write strictly
after the delete and before the commit still leaks. Fully closing it needs a lock or transaction
spanning the file and the row, which is out of proportion for a cached artifact that can be
regenerated. The re-read stays as a sound mitigation and D31 carries the rest.

### The two document fixes ride along without a re-review

Both are documents or comments with no behaviour change, so no verification pass is required to accept
them. They complete work the previous round's fixes left partly done and are recorded here so the next
pass sees them.

### No code behaviour changed since the previous round

The verification of the previous round's code fixes still stands at its own commit, because everything
in this round is a document or a comment. What the feature still lacks is a blinded discovery pass that
comes back quiet; neither the previous discovery pass, which found 32, nor a verification pass can give
that.
