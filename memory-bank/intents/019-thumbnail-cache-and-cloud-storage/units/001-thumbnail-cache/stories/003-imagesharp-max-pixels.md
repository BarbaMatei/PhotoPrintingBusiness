---
id: 003-imagesharp-max-pixels
unit: 001-thumbnail-cache
intent: 019-thumbnail-cache-and-cloud-storage
status: complete
priority: must
created: 2026-05-25T10:30:00Z
assigned_bolt: 042-thumbnail-cache
implemented: true
---

# Story: 003-imagesharp-max-pixels

## User Story

**As** the platform
**I want** ImageSharp to refuse to decode pixel bombs
**So that** a crafted image cannot exhaust API memory

## Acceptance Criteria

- [ ] `Configuration.Default.MemoryAllocator` and `MaxImageWidth/MaxImageHeight` capped at 25000 each (≈ 625 MP).
- [ ] Decoding an image exceeding the cap throws an exception caught and surfaced as 422 with `"Image dimensions exceed limits"`.
- [ ] Integration test: a 30000×30000 PNG header (small file, large dims) is rejected.

## Technical Notes

```csharp
// Program.cs
SixLabors.ImageSharp.Configuration.Default.MaxImageWidth  = 25_000;
SixLabors.ImageSharp.Configuration.Default.MaxImageHeight = 25_000;
```

## Dependencies

### Requires
- None

### Enables
- Closes gap #6 from architecture analysis

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Animated GIF / TIFF with many frames | Considered per-frame; ImageSharp's own per-frame cap also applies |
| Pre-existing assets in storage | Unaffected; only new decode operations gate |

## Out of Scope

- Per-format custom limits.
