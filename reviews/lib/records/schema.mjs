// The one machine home for the vocabulary and the numbers every gate shares: worklog events,
// finding statuses, severities, areas, lenses, size caps, grandfathering cut-offs, the folders
// under reviews/ that are not targets, the sha shapes, and the paths of the cross-target files.
// Reads nothing; the path constants are the only reason it touches node:path/node:url.
// Prose authorities that must change in the same commit as a change here (the
// descriptive-standards rule): the lens manifest table in runbooks/runbook-discovery.md, the
// area table, status vocabulary, event list and cap table in rules/doc-contracts.md.
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

export const MANIFEST_LENSES = [
  'correctness', 'security', 'requirements', 'quality', 'tests-coverage',
  'completeness-critic', 'db-parity', 'input-validation',
  'observability', 'race', 'frontend-ux',
]

export const AREAS = ['payments', 'orders', 'shipping', 'uploads', 'gallery', 'auth',
  'edge', 'observability', 'jobs', 'data', 'tests', 'records']

export const SEVERITIES = ['🔴', '🟠', '🟡', '⚪']

export const OPEN_STATUSES = ['open', 'in-progress']

export const STATUSES = [...OPEN_STATUSES, 'fixed', 'verified', 'wont-fix', 'deferred',
  'disputed', 'false-positive', 'backlog']

// The worklog event vocabulary and each event's required fields. `ids` marks the events whose
// "ids" field must be a non-empty array of PPW-<n>; the stamper (wl.mjs) enforces both.
export const EVENTS = {
  'round-start': { required: ['round'] },
  'round-end': { required: ['round'] },
  'triage-done': { required: ['round', 'clusters'] },
  'gate-open': { required: ['reason'] },
  'gate-closed': { required: ['reason'] },
  'gate-parked': { required: ['kind', 'default', 'reason'] },
  'protocol-written': { required: ['round', 'cluster', 'ids'], ids: true },
  'check-dispatched': { required: ['round', 'cluster', 'ids'], ids: true },
  'check-returned': { required: ['round', 'cluster', 'verdict'] },
  'test-run': { required: ['kind'] },
  finding: { required: ['id', 'status'] },
  'micro-review-dispatched': { required: ['cluster'] },
  'micro-review-returned': { required: ['cluster'] },
  'round-review-dispatched': { required: ['round'] },
  'round-review-returned': { required: ['round', 'found'] },
  'test-audit-dispatched': { required: ['round'] },
  'test-audit-returned': { required: ['round', 'verdict'] },
  'doc-gate': { required: [] },
  'pass-launch': { required: ['pass', 'type'] },
  'pass-records-done': { required: ['pass'] },
  'run-start': { required: [] },
  'run-end': { required: [] },
  note: { required: [] },
  void: { required: ['of'] },
  'verify-result': { required: ['id', 'verdict'] },
}

// Directories under reviews/ that are not review targets, so no folder scan treats them as one.
export const TARGETLESS = new Set(['lib', 'experiments', 'archive', 'state', 'rules',
  'runbooks', 'notes', 'system', 'templates'])

// Size caps the doc gate enforces (rules/doc-contracts.md keeps the reasons).
export const CAPS = {
  reviewBodyLines: 120,
  summaryBodyLines: 60,
  resolutionBodyLines: 200,
  resolutionNoteChars: 240,
  decisionLines: 15,
  ledgerBlockLines: 20,
  passDescriptionWords: 50,
  glanceStateLines: 5,
}

// A cited commit, whole-cell; SHA_SCAN_RE finds them inside prose (global, so use it with matchAll).
export const SHA_RE = /^[0-9a-f]{7,40}$/
export const SHA_SCAN_RE = /\b[0-9a-f]{7,40}\b/g

// Strict metrics validation applies to lines dated on/after this; earlier lines are lenient.
export const V2_CUTOFF = '2026-07-30'

// Fix-round lines and the runtime split exist only from this date; earlier rounds never get one.
export const V3_CUTOFF = '2026-08-03'

// Rules from the accepted fix-round audit apply to rounds closed on/after this date;
// every earlier record is grandfathered (doc-contracts.md, "Grandfathering cut-offs").
export const V4_CUTOFF = '2026-08-28'

// The live repo's paths: absolute, so a script running against a fixture repo rebases them onto
// its own --root (cli/args.mjs owns that root). Per-target files are never constants — they are
// derived from the target folder model/target.mjs resolves.
export const REVIEWS = join(dirname(fileURLToPath(import.meta.url)), '..', '..')
export const REPO = join(REVIEWS, '..')
export const BACKLOG = join(REVIEWS, 'state', 'backlog.md')
export const INDEX = join(REVIEWS, 'state', 'index.md')
export const TRACK_RECORD = join(REVIEWS, 'state', 'track-record.md')
export const ID_COUNTER = join(REVIEWS, 'state', 'id-counter')
export const DEFECT_CLASSES = join(REVIEWS, 'state', 'defect-classes.jsonl')
export const TEMPLATES = join(REVIEWS, 'templates')
