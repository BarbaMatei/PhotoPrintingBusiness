#!/usr/bin/env node
// Computed half of the owner-summary skill's "Reasons to doubt" section
// (.claude/skills/owner-summary/SKILL.md): reads one pass's metrics.jsonl line and prints
// markdown-ready bullet fragments for the skill to assemble into prose. Never writes anything.
// Lens list: reviews/lib/records/schema.mjs's MANIFEST_LENSES (the 11-lens manifest) is the authority
// here — not a hardcoded core list — so a manifest change there is picked up for free.
//
// Usage: node reviews/lib/summary-data.mjs [--root <repoRoot>] <target> <pass>
// Exit: 0 = fragments printed · 2 = usage error or no metrics line for that pass.
import { existsSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { readMetrics } from './records/metrics.mjs'
import { MANIFEST_LENSES } from './records/schema.mjs'

const USAGE = 'usage: node reviews/lib/summary-data.mjs [--root <repoRoot>] <target> <pass>'
const plural = (n, word, pluralWord = `${word}s`) => `${n} ${n === 1 ? word : pluralWord}`

const argv = process.argv.slice(2)
let root = null
const positional = []
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === '--root') root = argv[++i]
  else positional.push(argv[i])
}
const [target, passArg] = positional
const pass = Number(passArg)
if (!target || !Number.isFinite(pass)) { console.error(USAGE); process.exit(2) }

const REVIEWS = root ? join(root, 'reviews') : join(dirname(fileURLToPath(import.meta.url)), '..')
const targetDir = name => existsSync(join(REVIEWS, name)) ? join(REVIEWS, name) : join(REVIEWS, 'archive', name)
const metrics = readMetrics(targetDir(target), { strict: true })
if (!metrics) { console.error(USAGE); process.exit(2) }
const { lines } = metrics

// A certification pair writes two lines at one `pass`, so discovery-type lines merge per pass.
const isDiscoveryType = l => l.type === 'discovery' || l.type === 'delta-discovery'
const discoveryGroups = new Map()
for (const l of lines) {
  if (!isDiscoveryType(l) || !Number.isFinite(l.pass)) continue
  if (!discoveryGroups.has(l.pass)) discoveryGroups.set(l.pass, [])
  discoveryGroups.get(l.pass).push(l)
}
function mergeGroup(group) {
  const lensSet = new Set()
  const nf = { high: 0, medium: 0, low: 0, cleanup: 0 }
  const findings = []
  let budgetSkipped = 0
  for (const l of group) {
    for (const k of (Array.isArray(l.lenses) ? l.lenses : [])) lensSet.add(k)
    for (const sev of Object.keys(nf)) nf[sev] += l.new_findings?.[sev] ?? 0
    if (Array.isArray(l.findings)) findings.push(...l.findings)
    budgetSkipped += l.cost?.agents_by_stage?.budget_skipped ?? 0
  }
  return { type: group[0].type, lenses: [...lensSet], new_findings: nf, findings, cost: { agents_by_stage: { budget_skipped: budgetSkipped } } }
}

let L
if (discoveryGroups.has(pass)) {
  L = mergeGroup(discoveryGroups.get(pass))
} else {
  const others = lines.filter(l => Number.isFinite(l.pass) && l.pass === pass)
  if (!others.length) { console.error(USAGE); process.exit(2) }
  L = others[others.length - 1]
}

const out = []
const say = s => out.push(s)

const ran = new Set(Array.isArray(L.lenses) ? L.lenses : [])
const owed = MANIFEST_LENSES.filter(k => !ran.has(k))
if (owed.length) say(`- Owed manifest lenses: ${owed.join(', ')}`)

// Lines after this pass are excluded so a re-run for an old pass never leaks later history.
for (const [p, group] of discoveryGroups) {
  if (p > pass) continue
  const merged = mergeGroup(group)
  say(`- v${p}: ${merged.new_findings.high}+${merged.new_findings.medium}`)
}

const findings = Array.isArray(L.findings) ? L.findings : []
const unverified = findings.filter(f => typeof f.verdict === 'string' && f.verdict.startsWith('unverified-')).length
const hinted = findings.filter(f => f.hinted === true).length
say(`- ${plural(unverified, 'unverified-* verdict')} in this pass's findings`)
say(`- ${plural(hinted, 'hinted finding')} in this pass's findings`)

const budgetSkipped = L.cost?.agents_by_stage?.budget_skipped ?? 0
say(`- ${plural(budgetSkipped, 'budget-skipped agent')} this pass`)

if (L.type === 'delta-discovery' || L.type === 'verification') say(`- This pass type (${L.type}) cannot certify.`)

const low = L.new_findings?.low ?? 0
const cleanup = L.new_findings?.cleanup ?? 0
say(`- ${plural(low + cleanup, 'new low/cleanup finding')} filed to backlog automatically`)

console.log(out.join('\n'))
process.exit(0)
