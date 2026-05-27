/**
 * Quality badge calculation for photo uploads.
 *
 * Compares a photo's pixel dimensions against the minimum (200 DPI) and
 * optimal (300 DPI) pixel requirements for a given physical print size.
 *
 * Rotation-aware: if the photo is portrait but the size is landscape (or vice versa),
 * both orientations are tried and the best result is used.
 */

export type QualityLevel = 'green' | 'yellow' | 'red';

const MIN_DPI = 200;
const OPT_DPI = 300;
const MM_PER_INCH = 25.4;

function mmToPx(mm: number, dpi: number): number {
  return Math.round((mm / MM_PER_INCH) * dpi);
}

/**
 * Returns whether the photo fits the print size at the required DPI in either
 * portrait or landscape orientation.
 */
function fits(
  uploadW: number,
  uploadH: number,
  sizeW: number,
  sizeH: number,
  dpi: number,
): boolean {
  const reqW = mmToPx(sizeW, dpi);
  const reqH = mmToPx(sizeH, dpi);
  // Normal orientation
  if (uploadW >= reqW && uploadH >= reqH) return true;
  // Rotated orientation
  if (uploadW >= reqH && uploadH >= reqW) return true;
  return false;
}

/**
 * Computes the quality level of an upload for a given print size.
 *
 * @param uploadWidthPx  Photo pixel width
 * @param uploadHeightPx Photo pixel height
 * @param sizeWidthMm    Print size physical width in mm
 * @param sizeHeightMm   Print size physical height in mm
 */
export function computeQuality(
  uploadWidthPx: number,
  uploadHeightPx: number,
  sizeWidthMm: number,
  sizeHeightMm: number,
): QualityLevel {
  if (fits(uploadWidthPx, uploadHeightPx, sizeWidthMm, sizeHeightMm, OPT_DPI)) {
    return 'green';
  }
  if (fits(uploadWidthPx, uploadHeightPx, sizeWidthMm, sizeHeightMm, MIN_DPI)) {
    return 'yellow';
  }
  return 'red';
}

/** Human-readable label for a quality level (Romanian). */
export function qualityLabel(level: QualityLevel): string {
  switch (level) {
    case 'green': return 'Calitate excelentă';
    case 'yellow': return 'Calitate acceptabilă';
    case 'red': return 'Rezoluție prea mică';
  }
}
