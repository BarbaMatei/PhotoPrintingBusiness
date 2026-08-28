#!/usr/bin/env node
// Id minting + scaffold generator. `mint` reads/writes reviews/state/id-counter (its whole
// content is the next free PPW-<n>, doc-contracts.md rule 3). `scaffold-ledger` appends a
// Findings row + a Details block skeleton to a target's ledger.md (from templates/ledger.md
// on first use). `scaffold-resolution` seeds resolution-v<N>.md from review-v<N>.md's
// findings table (templates/resolution.md). Templates are law: only the fields named below
// are filled in; everything else is copied from the template verbatim as <fill in>.
//
// Usage:
//   node reviews/lib/mint-id.mjs [--root <repoRoot>] mint --count N [--dry-run]
//   node reviews/lib/mint-id.mjs [--root <repoRoot>] scaffold-ledger <target> --id PPW-<n>
//     --sev <emoji> --title "<t>" --file "<path:line>" --pass v<p> [--dry-run]
//   node reviews/lib/mint-id.mjs [--root <repoRoot>] scaffold-resolution <target>
//     --version N [--dry-run]
// --dry-run: prints what would happen, writes nothing.
// Exit: 0 done · 2 refusal (non-numeric counter, duplicate id, existing file, usage error).
import { readFileSync, writeFileSync, existsSync, mkdirSync } from 'node:fs'
import { join } from 'node:path'
import { REVIEWS as REVIEWS_HOME, ID_COUNTER } from './paths.mjs'

const SEV = ['🔴', '🟠', '🟡', '⚪']
const todayIso = () => new Date().toISOString().slice(0, 10)
const fail = msg => { console.error(`ERROR ${msg}`); process.exit(2) }

function parseArgs(argv) {
  let root = null
  const rest = []
  const opts = {}
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i]
    if (a === '--root') root = argv[++i]
    else if (a === '--dry-run') opts.dryRun = true
    else if (a.startsWith('--')) opts[a.slice(2)] = argv[++i]
    else rest.push(a)
  }
  return { root, rest, opts }
}

function mint(counterPath, opts) {
  const count = Number(opts.count)
  if (!Number.isInteger(count) || count < 1) fail('--count must be a positive integer')
  if (!existsSync(counterPath)) fail(`no id counter at ${counterPath}`)
  const raw = readFileSync(counterPath, 'utf8').trim()
  if (!/^\d+$/.test(raw)) fail(`id counter content "${raw}" is not a plain number`)
  const a = Number(raw)
  const b = a + count - 1
  console.log(`PPW-${a}..PPW-${b}`)
  if (!opts.dryRun) writeFileSync(counterPath, `${b + 1}\n`)
}

// The blank-line-before-heading anchor every ledger.md carries (fresh or not), so a row can
// always be inserted right before it and a block right after the file's last one.
const DETAILS_SPLIT = '\n\n## Details'

function freshLedger(target) {
  return `---
type: review-ledger
target: ${target}
updated: ${todayIso()}
---

# Ledger — ${target}

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|

## Details
`
}

function scaffoldLedger(reviewsDir, target, opts) {
  const id = opts.id
  if (!id || !/^PPW-\d+$/.test(id)) fail('--id must look like PPW-<n>')
  if (!SEV.includes(opts.sev)) fail(`--sev must be one of ${SEV.join(' ')}`)
  if (!opts.title) fail('--title is required')
  if (!opts.file) fail('--file is required')
  if (!opts.pass) fail('--pass is required')

  const dir = join(reviewsDir, target)
  const ledgerPath = join(dir, 'ledger.md')
  const existing = existsSync(ledgerPath)
  let body = existing ? readFileSync(ledgerPath, 'utf8') : freshLedger(target)
  if (existing && new RegExp(`\\b${id}\\b`).test(body)) fail(`${id} already exists in ${ledgerPath}`)

  const idx = body.indexOf(DETAILS_SPLIT)
  if (idx === -1) fail(`${ledgerPath} has no "## Details" heading — not a valid ledger`)
  const row = `| ${id} | ${opts.sev} | ${opts.pass} | ${opts.title} | \`${opts.file}\` | open | |`
  const block = `\n\n### ${id} — ${opts.title}\n\n- **What:** <fill in>\n- **Evidence:** <fill in>\n- **Suggested fix:** <fill in>\n- **History:** <append-only, one line per event>\n  - ${opts.pass}: found by <fill in>\n`
  let next = body.slice(0, idx) + '\n' + row + body.slice(idx) + block
  next = next.replace(/^updated:.*$/m, `updated: ${todayIso()}`)

  if (opts.dryRun) { console.log(`(dry-run) would ${existing ? 'append' : 'create'} ${ledgerPath} with ${id}`); return }
  mkdirSync(dir, { recursive: true })
  writeFileSync(ledgerPath, next)
  console.log(`MINT-ID: ${existing ? 'appended' : 'created'} ${ledgerPath} (${id})`)
}

function scaffoldResolution(reviewsDir, target, opts) {
  const N = Number(opts.version)
  if (!Number.isInteger(N) || N < 1) fail('--version must be a positive integer')
  const dir = join(reviewsDir, target)
  const reviewPath = join(dir, `review-v${N}.md`)
  if (!existsSync(reviewPath)) fail(`no review-v${N}.md in ${dir}`)
  const resolutionPath = join(dir, `resolution-v${N}.md`)
  if (existsSync(resolutionPath)) fail(`${resolutionPath} already exists — refusing to overwrite`)

  const reviewBody = readFileSync(reviewPath, 'utf8')
  const findingsSection = reviewBody.split(/^## /m).find(s => s.startsWith('Findings')) ?? ''
  const ids = [...findingsSection.matchAll(/^\|\s*(PPW-\d+)\s*\|/gm)].map(m => m[1])
  if (!ids.length) fail(`review-v${N}.md has no PPW-<n> findings rows`)

  const rows = ids.map(id => `| ${id} | open | — | — |`).join('\n')
  const content = `---
type: resolution
target: ${target}
version: ${N}
answers: review-v${N}.md
status: open
fixed_commit: <fill in>
---

# Resolution v${N} — ${target}

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
${rows}

## Scope

| Cluster | Findings | Files | Protocol |
|---|---|---|---|
| A — <fill in> | ${ids.join(', ')} | \`<fill in>\` | — |

## Decisions

### <One-line decision title (PPW-<n>)>

<Why the fix took this shape and not the obvious alternative. What was measured.
Max 15 lines per decision. Longer history belongs on the ledger row.>
`
  if (opts.dryRun) { console.log(`(dry-run) would create ${resolutionPath} with ${ids.length} finding(s)`); return }
  mkdirSync(dir, { recursive: true })
  writeFileSync(resolutionPath, content)
  console.log(`MINT-ID: created ${resolutionPath} (${ids.length} finding(s))`)
}

function main() {
  const { root, rest, opts } = parseArgs(process.argv.slice(2))
  const reviewsDir = root ? join(root, 'reviews') : REVIEWS_HOME
  const [cmd, target] = rest
  if (cmd === 'mint') {
    const counterPath = root ? join(reviewsDir, 'state', 'id-counter') : ID_COUNTER
    return mint(counterPath, opts)
  }
  if (cmd === 'scaffold-ledger') {
    if (!target) fail('scaffold-ledger requires a target')
    return scaffoldLedger(reviewsDir, target, opts)
  }
  if (cmd === 'scaffold-resolution') {
    if (!target) fail('scaffold-resolution requires a target')
    return scaffoldResolution(reviewsDir, target, opts)
  }
  console.error('usage: node reviews/lib/mint-id.mjs [--root <repoRoot>] mint --count N [--dry-run]')
  console.error('       node reviews/lib/mint-id.mjs [--root <repoRoot>] scaffold-ledger <target> --id PPW-<n> --sev <emoji> --title "<t>" --file "<path:line>" --pass v<p> [--dry-run]')
  console.error('       node reviews/lib/mint-id.mjs [--root <repoRoot>] scaffold-resolution <target> --version N [--dry-run]')
  process.exit(2)
}

main()
process.exit(0)
