---
type: resolution
target: 042-thumbnail-cache
version: 2
answers: pass v2 (verification — index row)
status: resolved
fixed_commit: e3a77d9
closed: 2026-07-14
---

# Resolution v2 — 042-thumbnail-cache

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-80 | fixed | `656c2fd` | The decode cap was raised from 50 MP to 100 MP on the owner's decision, so large-format prints and high-resolution originals are accepted. A 100 MP decode stays under the unchanged 512 MB allocator cap. |
| PPW-81 | fixed | `5712aad` | A restored upload is dropped only on a definitive 404. Server errors, network failures and a still-failing 401 keep it on screen so a later refresh can retry. Regression test added. |
| PPW-82 | deferred | — | A genuine race between the preview's file-and-row write and the cleanup job's read, delete and soft-delete, over a boundary with no shared transaction. Narrowing cannot close it. See Decisions. |
| PPW-83 | fixed | `e3a77d9` | Local storage returns forward-slashed keys and maps them to operating-system paths only for file work, so a key written on Windows reads on Linux and maps to a cloud object name. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Decode cap (`656c2fd`) | PPW-80 | `Services/ImageProcessor.cs`, `Tests/…/ImageProcessorTests.cs` | not needed (one constant and its tests) |
| B — Restore drop condition (`5712aad`) | PPW-81 | `UI/…/format-selector-page.ts` | not needed (one condition narrowed) |
| C — Storage key separator (`e3a77d9`) | PPW-83 | `Services/LocalStorageService.cs`, `Tests/…/LocalStorageServiceTests.cs` | not needed (a separator change with the first real-service tests) |
| D — Left undone this round | PPW-82 | — | not needed (no code changed) |

## Decisions

### The decode cap was set to 100 MP by owner decision

The 50 MP cap refused legitimate work. An A1 poster at 300 dots per inch is about 70 MP. Ordinary
high-resolution camera and phone originals also sit above 50 MP. The owner chose about 100 MP, which accepts
both. A 100 MP decode of roughly 400 MB stays under the unchanged 512 MB allocator cap. Images of
625 MP and above are still refused at the identify step. The cap is a named constant; raising
it materially means raising the allocator cap in step.

### The orphan race was deferred rather than partly patched

Any read-then-delete narrowing shrinks the window but cannot close it, because the preview may write
after the cleanup job's delete attempt. A partial fix is also not cleanly testable: the race cannot be
interleaved inside the single cleanup call a unit test can drive. The honest fix is a periodic sweep
over stored keys with no live row, which belongs with the cloud-storage bolt where the storage
lifecycle is redesigned and one sweep covers both tiers. Today's cost is one small orphaned file, in a
sub-second window, for an unreferenced upload previewed at the instant cleanup runs.

### Each behavioural fix was proven by reverting it

Reverting the cap to 50 MP reddens the boundary test; reverting the drop condition reddens the
transient-error test; reverting the key separator reddens the key-format test. All three were run.
