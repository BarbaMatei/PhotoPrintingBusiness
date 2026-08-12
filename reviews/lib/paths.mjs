// Single source of truth for every path in the review system. Scripts import
// from here; prose links are kept in sync by lib/fix-links.mjs. Moving a file
// means: git mv, update the constant, run fix-links, run lib/tests.
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

export const REVIEWS = join(dirname(fileURLToPath(import.meta.url)), '..')
export const REPO = join(REVIEWS, '..')

export const README = join(REVIEWS, 'README.md')
export const BACKLOG = join(REVIEWS, 'state', 'backlog.md')
export const INDEX = join(REVIEWS, 'state', 'index.md')
export const TRACK_RECORD = join(REVIEWS, 'state', 'track-record.md')
export const ID_COUNTER = join(REVIEWS, 'state', 'id-counter')
export const CONTRACTS = join(REVIEWS, 'rules', 'doc-contracts.md')
export const METRICS_SCHEMA = join(REVIEWS, 'rules', 'metrics-schema.md')
export const RUNBOOK_DISCOVERY = join(REVIEWS, 'runbooks', 'runbook-discovery.md')
export const RUNBOOK_VERIFICATION = join(REVIEWS, 'runbooks', 'runbook-verification.md')
export const DEFECT_CLASSES = join(REVIEWS, 'state', 'defect-classes.jsonl')
export const RATIONALE = join(REVIEWS, 'notes', 'rationale.md')
export const LOOP_DESIGN = join(REVIEWS, 'notes', 'self-driving-loop-design.md')
export const TEMPLATES = join(REVIEWS, 'templates')
export const ARCHIVE = join(REVIEWS, 'archive')
export const ID_MAP = join(ARCHIVE, 'id-map.md')

// Resolve a target folder: live root first, then archive.
import { existsSync } from 'node:fs'
export function targetDir(target) {
  const live = join(REVIEWS, target)
  if (existsSync(live)) return live
  return join(ARCHIVE, target)
}
