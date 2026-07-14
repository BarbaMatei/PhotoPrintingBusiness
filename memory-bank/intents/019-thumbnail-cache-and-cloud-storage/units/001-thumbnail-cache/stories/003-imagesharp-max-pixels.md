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

- [x] `Configuration.Default.MemoryAllocator` allocation cap **and** a decode-dimension guard.
  - **AC amended (REQ-1 + BUG-1, review 042-v1):**
    - The `MemoryAllocator` allocation cap (`AllocationLimitMegabytes = 512`) is now set in `Program.cs` — it had been silently dropped, which is what left a within-dimension bomb able to allocate GBs (REQ-1).
    - `MaxImageWidth/MaxImageHeight` **do not exist** in ImageSharp 3.1.11, so the guard is a per-call check. It was originally a per-axis cap (`> 25000`), which a 25000×25000 (≈625 MP) or multi-frame bomb bypassed. It is now a **total pixel-area cap** (`ImageProcessor.ExceedsDecodeLimits`) plus `DecoderOptions.MaxFrames = 1` (BUG-1). The cap is **100 MP** (NEW-1, review 042-v2) — sized to accept large-format prints (A1 @ 300 DPI ≈ 70 MP) and high-res camera originals while a 100 MP decode (~400 MB RGBA) stays under the 512 MB allocator backstop.
- [x] Decoding an image exceeding the cap throws `DecompressionBombException` (subclass of `UnprocessableEntityException`) → 422 `"Image dimensions exceed limits."`, and emits the reserved `uploads.decompression_bomb.rejected` event (OBS-3).
- [x] Test: `ImageProcessorTests` exercises the REAL processor — an oversized image (110 MP, over the 100 MP cap) is rejected before decode; a valid image yields a ≤800 px JPEG; an unreadable file → 422 (TEST-2).

## Technical Notes

As-built (review 042-v1). `MaxImageWidth/Height` don't exist in ImageSharp 3.1.11:

```csharp
// Program.cs — global allocation backstop (REQ-1)
SixLabors.ImageSharp.Configuration.Default.MemoryAllocator =
    SixLabors.ImageSharp.Memory.MemoryAllocator.Create(
        new SixLabors.ImageSharp.Memory.MemoryAllocatorOptions { AllocationLimitMegabytes = 512 });

// ImageProcessor — per-call area cap + single-frame decode (BUG-1; cap raised to 100 MP by NEW-1)
public const long MaxDecodePixels = 100_000_000; // 100 MP
public static bool ExceedsDecodeLimits(int w, int h) => (long)w * h > MaxDecodePixels;
// ... var info = await Image.IdentifyAsync(new DecoderOptions { MaxFrames = 1 }, stream, ct);
//     if (info is not null && ExceedsDecodeLimits(info.Width, info.Height))
//         throw new DecompressionBombException(info.Width, info.Height, DimensionsExceededMessage);
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
