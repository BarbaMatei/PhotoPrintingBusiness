#!/usr/bin/env node
// Keeps the review system's prose in step with the code that decides. Two jobs:
//   --check (default): every generated block must equal what the code renders today, and every
//                      markdown link under reviews/ must resolve to a file.
//   --write:           regenerate every block in place.
// A generated block is the text between `<!-- generated:<name> -->` and its closing marker; the
// prose around the markers is the owner's and is never touched. The block list below names the
// one machine home of each table, so a rule with two homes cannot drift apart silently.
//
// Usage: node reviews/lib/cli/docs-sync.mjs [--check | --write]
// Exit: 0 in step (or written) · 1 a block is stale or a link is broken.
import { readFileSync, writeFileSync, existsSync, readdirSync } from 'node:fs'
import { join, dirname, relative } from 'node:path'
import { REVIEWS, REPO, AREAS, AREA_COVERS, CAP_ROWS, CORE_LENSES, ADDED_LENSES, EVENTS, FIXER_EVENTS, HANDBACK_EVENT_DOCS, MANIFEST_LENSES } from '../records/schema.mjs'
import { V2_FIELDS, V3_FIX_FIELDS } from '../records/validate.mjs'
import { ROWS } from '../drive/rows.mjs'
import { GATE_DOCS, POLICY_NEXT } from '../drive/gates.mjs'

const table = (head, rows) => [`| ${head.join(' | ')} |`, `|${head.map(() => '---').join('|')}|`, ...rows.map(r => `| ${r.join(' | ')} |`)].join('\n')
const fields = ev => (EVENTS[ev]?.required ?? []).map(f => `\`${f}\``).join(', ')
const extraFields = ev => {
  const [dispatched] = ev.split(/-returned$/)
  const before = new Set(EVENTS[`${dispatched}-dispatched`]?.required ?? [])
  return (EVENTS[ev]?.required ?? []).filter(f => !before.has(f)).map(f => `\`${f}\``).join(', ')
}
const bullet = b => b.replace(/\{fields:([a-z-]+)\}/g, (_, ev) => fields(ev)).replace(/\{extra:([a-z-]+)\}/g, (_, ev) => extraFields(ev))

const BLOCKS = [
  {
    name: 'router-rows',
    file: join(REVIEWS, 'README.md'),
    render: () => table(['State', 'Next pass'], ROWS.map(r => [r.state, r.next])),
  },
  {
    name: 'policy-vocabulary',
    file: join(REVIEWS, 'README.md'),
    render: () => `The policy's whole answer vocabulary is ${POLICY_NEXT.map(n => `\`${n}\``).join(' · ')} —\neach executed exactly like a router answer.`,
  },
  {
    name: 'size-caps',
    file: join(REVIEWS, 'rules', 'doc-contracts.md'),
    render: () => table(['File', 'Cap'], CAP_ROWS.map(r => [r.file, r.cap])),
  },
  {
    name: 'areas',
    file: join(REVIEWS, 'rules', 'doc-contracts.md'),
    render: () => table(['Area', 'Covers'], AREAS.map(a => [`\`${a}\``, AREA_COVERS[a]])),
  },
  {
    name: 'handback-events',
    file: join(REVIEWS, 'rules', 'doc-contracts.md'),
    render: () => HANDBACK_EVENT_DOCS.map(d => bullet(d.bullet)).join('\n'),
  },
  {
    name: 'metrics-v2-fields',
    file: join(REVIEWS, 'rules', 'metrics-schema.md'),
    render: () => table(['Field', 'Type', 'Meaning'], V2_FIELDS.map(f => [f.cell, f.type, f.meaning])),
  },
  {
    name: 'metrics-v3-fix-fields',
    file: join(REVIEWS, 'rules', 'metrics-schema.md'),
    render: () => table(['Field', 'Type', 'Meaning'], V3_FIX_FIELDS.map(f => [f.cell, f.type, f.meaning])),
  },
  {
    name: 'core-lenses',
    file: join(REVIEWS, 'runbooks', 'runbook-discovery.md'),
    render: () => table(['Lens', 'Question', 'Backing'], CORE_LENSES.map(l => [l.lens, l.question, l.backing])),
  },
  {
    name: 'added-lenses',
    file: join(REVIEWS, 'runbooks', 'runbook-discovery.md'),
    render: () => table(['Change touches…', 'Add lens'], ADDED_LENSES.map(l => [l.touches, l.lens])),
  },
  {
    name: 'fixer-events',
    file: join(REPO, '.claude', 'skills', 'fix-review', 'SKILL.md'),
    render: () => table(['Event', 'When', 'Extra fields'], FIXER_EVENTS.map(e => [e.events.map(x => `\`${x}\``).join(' / '), e.when, e.extra])),
  },
  {
    name: 'gate-kinds',
    file: join(REPO, '.claude', 'skills', 'loop-driver', 'SKILL.md'),
    render: () => table(['Gate kind', 'Router exit', 'The router means', 'The written policy answers'],
      GATE_DOCS.map(g => [`\`${g.kind}\``, String(g.exit), g.router, g.policy])),
  },
]

// The lens manifest has one machine home; the two tables above must cover it exactly, and only the
// rows that are prose-only (a perspective with no key of its own) may carry no key.
function lensCoverage() {
  const keyed = [...CORE_LENSES, ...ADDED_LENSES].filter(l => l.key).map(l => l.key)
  const missing = MANIFEST_LENSES.filter(k => !keyed.includes(k))
  const unknown = keyed.filter(k => !MANIFEST_LENSES.includes(k))
  return [...missing.map(k => `lens "${k}" is in the manifest but no runbook row launches it`),
    ...unknown.map(k => `runbook row launches "${k}", which is not a manifest lens`)]
}

const OPEN = n => `<!-- generated:${n} -->`
const CLOSE = n => `<!-- /generated:${n} -->`
// Every comparison runs on LF text; a file keeps the line ending it already had when written back.
const eolOf = text => (text.includes('\r\n') ? '\r\n' : '\n')
const toLf = text => text.split('\r\n').join('\n')
function splice(text, name, body) {
  const open = text.indexOf(OPEN(name)), close = text.indexOf(CLOSE(name))
  if (open === -1 || close === -1 || close < open) return null
  const head = text.slice(0, open + OPEN(name).length)
  return { text: `${head}\n${body}\n${text.slice(close)}`, current: text.slice(open + OPEN(name).length, close).replace(/^\n|\n$/g, '') }
}

const write = process.argv.includes('--write')
const problems = []
const stale = []
const files = new Map()
const eols = new Map()
for (const b of BLOCKS) {
  if (!eols.has(b.file) && existsSync(b.file)) eols.set(b.file, eolOf(readFileSync(b.file, 'utf8')))
  const text = files.get(b.file) ?? (existsSync(b.file) ? toLf(readFileSync(b.file, 'utf8')) : null)
  if (text === null) { problems.push(`${relative(REPO, b.file)}: file not found (block ${b.name})`); continue }
  const spliced = splice(text, b.name, b.render())
  if (!spliced) { problems.push(`${relative(REPO, b.file)}: no <!-- generated:${b.name} --> markers`); continue }
  files.set(b.file, spliced.text)
  if (spliced.current !== b.render()) stale.push({ block: b, current: spliced.current, wanted: b.render() })
}
if (write) {
  for (const [file, text] of files) writeFileSync(file, text.split('\n').join(eols.get(file)))
  console.log(`docs-sync --write: ${BLOCKS.length} blocks rendered into ${files.size} files`)
}
for (const s of stale) {
  if (write) continue
  console.log(`STALE ${relative(REPO, s.block.file)} — generated:${s.block.name}`)
  const now = s.current.split('\n'), want = s.wanted.split('\n')
  for (let i = 0; i < Math.max(now.length, want.length); i++) {
    if (now[i] === want[i]) continue
    if (now[i] !== undefined) console.log(`  - ${now[i]}`)
    if (want[i] !== undefined) console.log(`  + ${want[i]}`)
  }
}
problems.push(...lensCoverage())

// The link check (formerly fix-links.mjs): every markdown link under reviews/ resolves.
const mdFiles = []
;(function walk(d) {
  for (const e of readdirSync(d, { withFileTypes: true })) {
    const p = join(d, e.name)
    if (e.isDirectory()) walk(p)
    else if (e.name.endsWith('.md')) mdFiles.push(p)
  }
})(REVIEWS)
let broken = 0
for (const p of mdFiles) {
  for (const m of readFileSync(p, 'utf8').matchAll(/\]\(([^)\s]+)\)/g)) {
    const t = m[1]
    if (/^https?:|^#|^mailto:/.test(t)) continue
    if (!existsSync(join(dirname(p), t.split('#')[0]))) { console.log(`BROKEN ${relative(REPO, p)}: ${t}`); broken++ }
  }
}
console.log(broken ? `${broken} broken link(s)` : 'all reviews/ links resolve')
for (const p of problems) console.log(`PROBLEM ${p}`)
if (!write && !stale.length && !problems.length && !broken) console.log(`docs-sync --check: ${BLOCKS.length} generated blocks in step`)
process.exit((write ? 0 : stale.length) + problems.length + broken ? 1 : 0)
