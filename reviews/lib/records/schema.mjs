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

// ---------------------------------------------------------------------------
// The doc-facing half of the vocabulary: the prose cells that go with the keys and numbers
// above, so cli/docs-sync.mjs can render each authority's table from this file instead of the
// tables being kept in step by hand. Only cells no machine reads live here; every key, number
// and field name in a rendered table comes from the constants above.

// The "Covers" column of doc-contracts' area table, one entry per AREAS word.
export const AREA_COVERS = {
  payments: 'charging, webhooks, idempotency, invoices',
  orders: 'order lifecycle, admin ops',
  shipping: 'Sameday, AWB, couriers',
  uploads: 'upload handling, originals and thumbnails, storage tiers, S3 and local',
  gallery: 'customer-facing photo UI',
  auth: 'identity, sessions, guest tokens',
  edge: 'proxy, endpoint exposure, rate limiting, health and metrics gating',
  observability: 'metrics, tracing, Sentry, SLOs, dashboards',
  jobs: 'background jobs, retries, sweeps',
  data: 'EF, migrations, schema fidelity',
  tests: 'test infrastructure: flakes, helpers, coverage gaps whose fix is test-only',
  records: 'docs, memory-bank, process records',
}

// doc-contracts' size-cap table: every number interpolated from CAPS.
export const CAP_ROWS = [
  { file: '`summary-v<n>.md`', cap: `${CAPS.summaryBodyLines} lines of body` },
  { file: '`review-v<n>.md`', cap: `${CAPS.reviewBodyLines} lines of body; table rows are single lines` },
  { file: '`resolution-v<n>.md`', cap: `${CAPS.resolutionBodyLines} lines of body (the Findings table rows live here); \`Note\` cell ≤ ${CAPS.resolutionNoteChars} characters; each Decision ≤ ${CAPS.decisionLines} lines` },
  { file: '`ledger.md` detail block', cap: `${CAPS.ledgerBlockLines} lines per defect; table cells one line; Status cell is the status word only` },
  { file: '`backlog.md` row', cap: '1 table line' },
]

// runbook-discovery's two lens tables. The core six run on every full pass; the rest are added by
// what the change touches. `key` is the MANIFEST_LENSES key the row launches — null on the one row
// that names a perspective with no key of its own (it rides along inside the lenses that have one),
// and a repeat of a core key where the trigger only raises that lens's strength.
export const CORE_LENSES = [
  { key: 'correctness', lens: 'Correctness', question: 'What input/state/timing makes this wrong?', backing: '`/code-review`, Explore finders' },
  { key: 'security', lens: 'Security', question: 'Authz bypass, tenant isolation, injection, secret/PII exposure', backing: '`/security-review`' },
  { key: 'requirements', lens: 'Requirements', question: 'Delivers the claimed scope, at the contract level?', backing: '`/review`' },
  { key: 'quality', lens: 'Quality / altitude', question: 'Reuse, simplification, right layer — **report-only, never auto-apply**', backing: '`/simplify`' },
  { key: 'tests-coverage', lens: 'Tests & verification', question: 'Untested failure modes; test the tests', backing: 'main agent + Explore' },
  { key: 'completeness-critic', lens: 'Completeness critic', question: 'What did we *not* look at? Runs **last**', backing: 'Explore' },
]

export const ADDED_LENSES = [
  { key: 'db-parity', touches: 'DB migration / schema', lens: 'DB / migration-parity (does the DDL run in any test? provider divergence)' },
  { key: null, touches: 'Second provider behind one interface', lens: 'Per-provider / per-entry-point symmetry' },
  { key: 'input-validation', touches: 'New request header / external input', lens: 'Input-validation (trim, length, null, encoding)' },
  { key: 'observability', touches: 'New exception type / error path', lens: 'Observability (distinguishable triage signal?)' },
  { key: 'race', touches: 'Concurrency / idempotency / retries', lens: 'Race (TOCTOU, transaction boundaries, crash windows)' },
  { key: 'security', touches: 'Money / charges / orders', lens: 'Security at full strength' },
  { key: 'frontend-ux', touches: 'Frontend change', lens: 'Accessibility / UX' },
]

// The fixer's worklog table (.claude/skills/fix-review/SKILL.md): which events it stamps, when,
// and the fields beyond the required ones. The Event cell is rendered from `events`, so a table
// row can never name an event the stamper does not know.
export const FIXER_EVENTS = [
  { events: ['round-start', 'round-end'], when: 'first action / after hand-back summary', extra: '`round`' },
  { events: ['triage-done'], when: 'cluster plan written', extra: '`round`, `clusters` (both required), plus `checks_needed`, `pre_cleared` (review pre-checks consumed), `gates`' },
  { events: ['protocol-written'], when: 'a cluster\'s protocol block is written — BEFORE any of its `finding` events', extra: '`round`, `cluster`, `ids` (the cluster\'s PPW ids)' },
  { events: ['gate-open', 'gate-closed'], when: 'before asking the owner / when the answer arrives', extra: '`reason`' },
  { events: ['check-dispatched', 'check-returned'], when: 'approach-check out / verdict in', extra: 'out: `round`, `cluster`, `ids` (the PPW ids the check covers — the auditor matches on them); back: `round`, `cluster`, `verdict`, and `tokens` if known. `ids` belongs to the dispatch only' },
  { events: ['test-run'], when: 'stamped for you by `run-scoped-tests.mjs` when a run finishes — never by hand', extra: '`kind: red\\|green\\|final\\|baseline\\|revert-and-rerun`, `filter`, `passed`, `failed`, `duration_s`' },
  { events: ['finding'], when: 'a finding reaches a status', extra: '`id`, `status`, `commit`' },
  { events: ['round-review-dispatched', 'round-review-returned'], when: 'once, when the last cluster\'s commits land', extra: '`round`, on return `found`' },
  { events: ['test-audit-dispatched', 'test-audit-returned'], when: 'once, alongside the round review', extra: '`round`, on return `verdict`' },
]

// doc-contracts' hand-back evidence list. `{fields:<ev>}` renders that event's required fields and
// `{extra:<ev>}` the fields it adds over the event before it, both from EVENTS — the wording and
// the line breaks are the authority's own.
export const HANDBACK_EVENT_DOCS = [
  { bullet: '- `protocol-written` ({fields:protocol-written}) — appended when a cluster\'s\n  protocol block is written, **before** any of that cluster\'s `finding` events.' },
  { bullet: '- `check-dispatched` events carry `ids`: the `PPW-<n>` list the approach-check\n  covers. A trigger-classified fix with no consumed pre-check verdict must\n  appear in one.' },
  { bullet: '- `round-review-dispatched` / `round-review-returned` ({fields:round-review-dispatched}, on return\n  {extra:round-review-returned}) — the one composition review over the round\'s whole diff.' },
  { bullet: '- `test-audit-dispatched` / `test-audit-returned` ({fields:test-audit-dispatched}, on return\n  {extra:test-audit-returned}) — the test-meaning check; required whenever the round ran a red\n  test run.' },
]
