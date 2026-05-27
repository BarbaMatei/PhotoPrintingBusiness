import { computeQuality, qualityLabel, QualityLevel } from './quality.utils';

// 10x15 cm print = 100mm x 150mm
// At 300 DPI: 1181 x 1772 px
// At 200 DPI:  787 x 1181 px

describe('computeQuality', () => {
  it('returns "green" when image meets 300 DPI for the print size', () => {
    // 10x15 cm at 300 DPI requires 1181×1772 px
    expect(computeQuality(1200, 1800, 100, 150)).toBe('green');
  });

  it('returns "yellow" when image meets 200 DPI but not 300 DPI', () => {
    // 10x15 cm at 200 DPI requires 787×1181 px; use 900×1300 (fails 300, passes 200)
    expect(computeQuality(900, 1300, 100, 150)).toBe('yellow');
  });

  it('returns "red" when image is below 200 DPI', () => {
    // 10x15 cm at 200 DPI requires 787×1181 px; use 500×700 (too small)
    expect(computeQuality(500, 700, 100, 150)).toBe('red');
  });

  it('returns "green" for rotated orientation (landscape image, portrait size)', () => {
    // 1800×1200 px for 100×150 mm: rotated → 1800≥1181 and 1200≥787 at 200 DPI
    // Also 1800≥1772 and 1200≥1181 at 300 DPI (rotated)
    expect(computeQuality(1800, 1200, 100, 150)).toBe('green');
  });

  it('returns "yellow" for rotated orientation at minimum quality', () => {
    // Portrait size 100×150, landscape image 1300×900
    // Rotated: 1300≥1181 (150mm@200dpi) and 900≥787 (100mm@200dpi) → yellow
    // At 300 DPI: 1181 and 1772 required; 1300<1772 fails → not green
    expect(computeQuality(1300, 900, 100, 150)).toBe('yellow');
  });

  it('returns "green" when both dimensions exactly meet 300 DPI threshold', () => {
    // 100mm at 300dpi = 1181px, 150mm at 300dpi = 1772px
    expect(computeQuality(1181, 1772, 100, 150)).toBe('green');
  });

  it('quality changes correctly when print size changes', () => {
    // Same image 800x1200, small format 50×75 mm
    // 50mm@300dpi = 591px, 75mm@300dpi = 886px → 800≥591, 1200≥886 → green
    expect(computeQuality(800, 1200, 50, 75)).toBe('green');

    // Same image, large format 200×300 mm
    // 200mm@200dpi = 1575px, 300mm@200dpi = 2362px → 800<1575 → red
    expect(computeQuality(800, 1200, 200, 300)).toBe('red');
  });
});

describe('qualityLabel', () => {
  it('returns Romanian label for green', () => {
    const label = qualityLabel('green');
    expect(label).toBeTruthy();
    expect(typeof label).toBe('string');
  });

  it('returns Romanian label for yellow', () => {
    const label = qualityLabel('yellow');
    expect(label).toBeTruthy();
  });

  it('returns Romanian label for red', () => {
    const label = qualityLabel('red');
    expect(label).toBeTruthy();
  });

  it('returns different labels for each quality level', () => {
    const green = qualityLabel('green');
    const yellow = qualityLabel('yellow');
    const red = qualityLabel('red');
    expect(green).not.toBe(yellow);
    expect(yellow).not.toBe(red);
    expect(green).not.toBe(red);
  });
});
