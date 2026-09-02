// The block list docs-sync renders, and the marked-file set derived from it. One home, because
// three readers ask the same question — docs-sync (what to render), the suite (what to copy for
// the mutation test), and the pre-commit hook (which staged paths open the drift gate) — and a
// file with generated blocks that one of them does not know about drifts unnoticed.
// A generated block is the text between `<!-- generated:<name> -->` and its closing marker; each
// entry's `file` is repo-relative so the same list works against `--root <copy>`.
import { readFileSync, existsSync } from 'node:fs'
import { join } from 'node:path'
import { AREAS, AREA_COVERS, CAP_ROWS, CORE_LENSES, ADDED_LENSES, EVENTS, FIXER_EVENTS, HANDBACK_EVENT_DOCS, MANIFEST_LENSES } from '../records/schema.mjs'
import { V2_FIELDS, V3_FIX_FIELDS } from '../records/validate.mjs'
import { ROWS } from '../drive/rows.mjs'
import { GATE_DOCS, POLICY_NEXT } from '../drive/gates.mjs'

const README = 'reviews/README.md'
const CONTRACTS = 'reviews/rules/doc-contracts.md'
const METRICS = 'reviews/rules/metrics-schema.md'
const DISCOVERY = 'reviews/runbooks/runbook-discovery.md'
const FIX_SKILL = '.claude/skills/fix-review/SKILL.md'
const DRIVER_SKILL = '.claude/skills/loop-driver/SKILL.md'

const table = (head, rows) => [`| ${head.join(' | ')} |`, `|${head.map(() => '---').join('|')}|`, ...rows.map(r => `| ${r.join(' | ')} |`)].join('\n')
const fields = ev => (EVENTS[ev]?.required ?? []).map(f => `\`${f}\``).join(', ')
const extraFields = ev => {
  const [dispatched] = ev.split(/-returned$/)
  const before = new Set(EVENTS[`${dispatched}-dispatched`]?.required ?? [])
  return (EVENTS[ev]?.required ?? []).filter(f => !before.has(f)).map(f => `\`${f}\``).join(', ')
}
const bullet = b => b.replace(/\{fields:([a-z-]+)\}/g, (_, ev) => fields(ev)).replace(/\{extra:([a-z-]+)\}/g, (_, ev) => extraFields(ev))

export const BLOCKS = [
  { name: 'router-rows', file: README, render: () => table(['State', 'Next pass'], ROWS.map(r => [r.state, r.next])) },
  {
    name: 'policy-vocabulary',
    file: README,
    render: () => `The policy's whole answer vocabulary is ${POLICY_NEXT.map(n => `\`${n}\``).join(' · ')} —\neach executed exactly like a router answer.`,
  },
  { name: 'size-caps', file: CONTRACTS, render: () => table(['File', 'Cap'], CAP_ROWS.map(r => [r.file, r.cap])) },
  { name: 'areas', file: CONTRACTS, render: () => table(['Area', 'Covers'], AREAS.map(a => [`\`${a}\``, AREA_COVERS[a]])) },
  { name: 'handback-events', file: CONTRACTS, render: () => HANDBACK_EVENT_DOCS.map(d => bullet(d.bullet)).join('\n') },
  { name: 'metrics-v2-fields', file: METRICS, render: () => table(['Field', 'Type', 'Meaning'], V2_FIELDS.map(f => [f.cell, f.type, f.meaning])) },
  { name: 'metrics-v3-fix-fields', file: METRICS, render: () => table(['Field', 'Type', 'Meaning'], V3_FIX_FIELDS.map(f => [f.cell, f.type, f.meaning])) },
  { name: 'core-lenses', file: DISCOVERY, render: () => table(['Lens', 'Question', 'Backing'], CORE_LENSES.map(l => [l.lens, l.question, l.backing])) },
  { name: 'added-lenses', file: DISCOVERY, render: () => table(['Change touches…', 'Add lens'], ADDED_LENSES.map(l => [l.touches, l.lens])) },
  {
    name: 'fixer-events',
    file: FIX_SKILL,
    render: () => table(['Event', 'When', 'Extra fields'], FIXER_EVENTS.map(e => [e.events.map(x => `\`${x}\``).join(' / '), e.when, e.extra])),
  },
  {
    name: 'gate-kinds',
    file: DRIVER_SKILL,
    render: () => table(['Gate kind', 'Router exit', 'The router means', 'The written policy answers'],
      GATE_DOCS.map(g => [`\`${g.kind}\``, String(g.exit), g.router, g.policy])),
  },
]

// Every file that carries generated blocks, repo-relative — derived, never a second list.
export const MARKED_FILES = [...new Set(BLOCKS.map(b => b.file))]

// The lens manifest has one machine home; the two runbook tables must cover it exactly, and only
// the rows that are prose-only (a perspective with no key of its own) may carry no key.
export function lensCoverage() {
  const keyed = [...CORE_LENSES, ...ADDED_LENSES].filter(l => l.key).map(l => l.key)
  const missing = MANIFEST_LENSES.filter(k => !keyed.includes(k))
  const unknown = keyed.filter(k => !MANIFEST_LENSES.includes(k))
  return [...missing.map(k => `lens "${k}" is in the manifest but no runbook row launches it`),
    ...unknown.map(k => `runbook row launches "${k}", which is not a manifest lens`)]
}

// The hook's drift gate opens on staged paths, not on this list, so a marked file no pathspec
// matches would never reach the gate. Read the pathspecs off the hook itself and check they cover
// MARKED_FILES; the hook stays a glob (a new rules/ or runbooks/ file is covered the day it lands).
export function hookCoverage(repo) {
  const hook = join(repo, '.githooks', 'pre-commit')
  if (!existsSync(hook)) return []
  const text = readFileSync(hook, 'utf8')
  const call = text.indexOf('docs-sync.mjs --check >&2')
  if (call === -1) return ['.githooks/pre-commit no longer calls the full docs-sync --check (drift + links)']
  const gate = text.lastIndexOf('--diff-filter=ACMR --', call)
  if (gate === -1) return ['.githooks/pre-commit: no staged-path gate found before the docs-sync call']
  const spec = text.slice(gate + '--diff-filter=ACMR --'.length, text.indexOf(')', gate))
  const patterns = spec.trim().split(/\s+/).map(p => p.replace(/^['"]|['"]$/g, '')).filter(Boolean)
  const res = patterns.map(p => new RegExp(`^${p.split('*').map(s => s.replace(/[.+?^${}()|[\]\\]/g, '\\$&')).join('[^/]*')}$`))
  return MARKED_FILES.filter(f => !res.some(re => re.test(f)))
    .map(f => `${f} carries generated blocks but no .githooks/pre-commit pathspec stages it into the drift gate`)
}
