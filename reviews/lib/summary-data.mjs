#!/usr/bin/env node
// Computed half of the owner-summary skill's "Reasons to doubt" section
// (.claude/skills/owner-summary/SKILL.md): reads one pass's metrics.jsonl line and prints
// markdown-ready bullet fragments for the skill to assemble into prose. Never writes anything.
// Lens list: reviews/lib/vocab.mjs's MANIFEST_LENSES (the 11-lens manifest) is the authority
// here — not a hardcoded core list — so a manifest change there is picked up for free.
//
// Usage: node reviews/lib/summary-data.mjs [--root <repoRoot>] <target> <pass>
// Exit: 0 = fragments printed · 2 = usage error or no metrics line for that pass.
import { readFileSync, existsSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { MANIFEST_LENSES } from './vocab.mjs'

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
const metricsPath = join(targetDir(target), 'metrics.jsonl')
if (!existsSync(metricsPath)) { console.error(USAGE); process.exit(2) }

const lines = readFileSync(metricsPath, 'utf8').split(/\r?\n/).filter(l => l.trim()).map(l => JSON.parse(l))
const matches = lines.filter(l => Number.isFinite(l.pass) && l.pass === pass)
if (!matches.length) { console.error(USAGE); process.exit(2) }
const L = matches[matches.length - 1]

const out = []
const say = s => out.push(s)

const ran = new Set(Array.isArray(L.lenses) ? L.lenses : [])
const owed = MANIFEST_LENSES.filter(k => !ran.has(k))
if (owed.length) say(`- Owed manifest lenses: ${owed.join(', ')}`)

// The decay curve is blind (full-scope) passes only, matching route-next-pass.mjs's grouping;
// lines after this pass are excluded so a re-run for an old pass never leaks later history.
const isDiscoveryType = l => l.type === 'discovery' || l.type === 'delta-discovery'
for (const l of lines) {
  if (!isDiscoveryType(l) || !Number.isFinite(l.pass) || l.pass > pass) continue
  say(`- v${l.pass}: ${l.new_findings?.high ?? 0}+${l.new_findings?.medium ?? 0}`)
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
