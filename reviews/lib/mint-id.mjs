#!/usr/bin/env node
// Id minting + scaffold generator. `mint` reads/writes reviews/state/id-counter (its whole
// content is the next free PPW-<n>, doc-contracts.md rule 3). `scaffold-ledger` appends a
// Findings row + a Details block to a target's ledger.md, reading templates/ledger.md at
// runtime to build a fresh one when absent. `scaffold-resolution` seeds resolution-v<N>.md
// from review-v<N>.md's findings table, reading templates/resolution.md at runtime. Reading
// the templates (not hand-duplicated strings) means a template edit reaches every scaffold
// without a script change.
//
// Usage:
//   node reviews/lib/mint-id.mjs [--root <repoRoot>] mint --count N [--dry-run]
//   node reviews/lib/mint-id.mjs [--root <repoRoot>] scaffold-ledger <target> --id PPW-<n>
//     --sev <emoji> --title "<t>" --file "<path:line>" --pass v<p> [--dry-run]
//   node reviews/lib/mint-id.mjs [--root <repoRoot>] scaffold-resolution <target>
//     --version N [--dry-run]
// --dry-run: prints what would happen, writes nothing.
// Exit: 0 done · 2 refusal (non-numeric counter, duplicate id, existing file, usage error,
// unrecognized template shape).
import { readFileSync, writeFileSync, existsSync, mkdirSync } from 'node:fs'
import { join } from 'node:path'
import { REVIEWS as REVIEWS_HOME, ID_COUNTER, TEMPLATES } from './paths.mjs'

const SEV = ['🔴', '🟠', '🟡', '⚪']
const todayIso = () => new Date().toISOString().slice(0, 10)
const fail = msg => { console.error(`ERROR ${msg}`); process.exit(2) }
const strip = l => l.replace(/\r$/, '')

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

// Builds a fresh ledger.md from templates/ledger.md: keeps its frontmatter (minus `closed:`,
// absent while the loop is open) and its Findings/Details headings + table header, drops the
// template's illustrative example row and detail block. Always LF — there is no pre-existing
// file whose bytes need preserving.
function freshLedger(target, templatesDir) {
  const lines = readFileSync(join(templatesDir, 'ledger.md'), 'utf8').split(/\r?\n/)
  const fmClose = lines.findIndex((l, i) => i > 0 && l === '---')
  const titleAt = lines.findIndex((l, i) => i > fmClose && /^# /.test(l))
  const findingsAt = lines.indexOf('## Findings')
  const headerAt = lines.findIndex((l, i) => i > findingsAt && /^\|/.test(l))
  const detailsAt = lines.indexOf('## Details')
  if ([fmClose, titleAt, findingsAt, headerAt, detailsAt].some(i => i === -1))
    fail('templates/ledger.md is missing an expected heading or table — template shape changed')

  const fm = lines.slice(1, fmClose).filter(l => !/^closed:/.test(l)).map(l =>
    /^target:/.test(l) ? `target: ${target}` : /^updated:/.test(l) ? `updated: ${todayIso()}` : l)

  return [
    '---', ...fm, '---', '',
    lines[titleAt].replace('<target>', target), '',
    '## Findings', '',
    lines[headerAt], lines[headerAt + 1], '',
    '## Details', '',
  ].join('\n')
}

function scaffoldLedger(reviewsDir, target, opts, templatesDir) {
  const id = opts.id
  if (!id || !/^PPW-\d+$/.test(id)) fail('--id must look like PPW-<n>')
  if (!SEV.includes(opts.sev)) fail(`--sev must be one of ${SEV.join(' ')}`)
  if (!opts.title) fail('--title is required')
  if (!opts.file) fail('--file is required')
  if (!opts.pass) fail('--pass is required')

  const dir = join(reviewsDir, target)
  const ledgerPath = join(dir, 'ledger.md')
  const existing = existsSync(ledgerPath)
  const raw = existing ? readFileSync(ledgerPath, 'utf8') : freshLedger(target, templatesDir)
  if (existing && new RegExp(`\\b${id}\\b`).test(raw)) fail(`${id} already exists in ${ledgerPath}`)

  // A Windows checkout keeps real ledgers CRLF: splitting on plain '\n' leaves the '\r' on
  // every existing line untouched, so only the lines we insert need one added to match their
  // neighbours (render-records.mjs's crOf pattern) — every pre-existing byte stays as-is.
  const lines = raw.split('\n')
  const cr = lines.some(l => l.endsWith('\r')) ? '\r' : ''
  const detailsAt = lines.findIndex(l => strip(l) === '## Details')
  if (detailsAt === -1) fail(`${ledgerPath} has no "## Details" heading — not a valid ledger`)

  const row = `| ${id} | ${opts.sev} | ${opts.pass} | ${opts.title} | \`${opts.file}\` | open | |${cr}`
  const rowAt = detailsAt > 0 && strip(lines[detailsAt - 1]) === '' ? detailsAt - 1 : detailsAt
  lines.splice(rowAt, 0, row)

  const block = [
    '', `### ${id} — ${opts.title}`, '',
    '- **What:** <fill in>', '- **Evidence:** <fill in>', '- **Suggested fix:** <fill in>',
    '- **History:** <append-only, one line per event>', `  - ${opts.pass}: found by <fill in>`,
  ].map(l => l + cr)
  const tailAt = lines.length && lines[lines.length - 1] === '' ? lines.length - 1 : lines.length
  lines.splice(tailAt, 0, ...block)

  const updatedAt = lines.findIndex(l => /^updated:/.test(strip(l)))
  if (updatedAt !== -1) lines[updatedAt] = `updated: ${todayIso()}${cr}`

  if (opts.dryRun) { console.log(`(dry-run) would ${existing ? 'append' : 'create'} ${ledgerPath} with ${id}`); return }
  mkdirSync(dir, { recursive: true })
  writeFileSync(ledgerPath, lines.join('\n'))
  console.log(`MINT-ID: ${existing ? 'appended' : 'created'} ${ledgerPath} (${id})`)
}

// Reads templates/resolution.md at runtime: fills the frontmatter scalars (drops `closed:`),
// replaces the Findings table's one illustrative row with one row per id, and keeps Scope and
// Decisions — including the template's "### Protocol — <label>" block — verbatim, since
// neither is named as filled by the caller.
function resolutionSkeleton(target, N, ids, templatesDir) {
  const lines = readFileSync(join(templatesDir, 'resolution.md'), 'utf8').split(/\r?\n/)
  const fmClose = lines.findIndex((l, i) => i > 0 && l === '---')
  const titleAt = lines.findIndex((l, i) => i > fmClose && /^# /.test(l))
  const findingsAt = lines.indexOf('## Findings')
  const headerAt = lines.findIndex((l, i) => i > findingsAt && /^\|/.test(l))
  const scopeAt = lines.indexOf('## Scope')
  const decisionsAt = lines.indexOf('## Decisions')
  if ([fmClose, titleAt, findingsAt, headerAt, scopeAt, decisionsAt].some(i => i === -1))
    fail('templates/resolution.md is missing an expected heading or table — template shape changed')

  const fm = lines.slice(1, fmClose).filter(l => !/^closed:/.test(l)).map(l => {
    if (/^target:/.test(l)) return `target: ${target}`
    if (/^version:/.test(l)) return `version: ${N}`
    if (/^answers:/.test(l)) return `answers: review-v${N}.md`
    if (/^status:/.test(l)) return 'status: open'
    if (/^fixed_commit:/.test(l)) return 'fixed_commit: <fill in>'
    return l
  })

  return [
    '---', ...fm, '---', '',
    lines[titleAt].replace('<n>', N).replace('<target>', target), '',
    '## Findings', '',
    lines[headerAt], lines[headerAt + 1],
    ...ids.map(id => `| ${id} | open | — | — |`), '',
    ...lines.slice(scopeAt, decisionsAt),
    ...lines.slice(decisionsAt),
  ].join('\n')
}

function scaffoldResolution(reviewsDir, target, opts, templatesDir) {
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

  const content = resolutionSkeleton(target, N, ids, templatesDir)
  if (opts.dryRun) { console.log(`(dry-run) would create ${resolutionPath} with ${ids.length} finding(s)`); return }
  mkdirSync(dir, { recursive: true })
  writeFileSync(resolutionPath, content)
  console.log(`MINT-ID: created ${resolutionPath} (${ids.length} finding(s))`)
}

function main() {
  const { root, rest, opts } = parseArgs(process.argv.slice(2))
  const reviewsDir = root ? join(root, 'reviews') : REVIEWS_HOME
  const templatesDir = root ? join(reviewsDir, 'templates') : TEMPLATES
  const [cmd, target] = rest
  if (cmd === 'mint') {
    const counterPath = root ? join(reviewsDir, 'state', 'id-counter') : ID_COUNTER
    return mint(counterPath, opts)
  }
  if (cmd === 'scaffold-ledger') {
    if (!target) fail('scaffold-ledger requires a target')
    return scaffoldLedger(reviewsDir, target, opts, templatesDir)
  }
  if (cmd === 'scaffold-resolution') {
    if (!target) fail('scaffold-resolution requires a target')
    return scaffoldResolution(reviewsDir, target, opts, templatesDir)
  }
  console.error('usage: node reviews/lib/mint-id.mjs [--root <repoRoot>] mint --count N [--dry-run]')
  console.error('       node reviews/lib/mint-id.mjs [--root <repoRoot>] scaffold-ledger <target> --id PPW-<n> --sev <emoji> --title "<t>" --file "<path:line>" --pass v<p> [--dry-run]')
  console.error('       node reviews/lib/mint-id.mjs [--root <repoRoot>] scaffold-resolution <target> --version N [--dry-run]')
  process.exit(2)
}

try {
  main()
  process.exit(0)
} catch (e) {
  console.error(`ERROR ${e.message}`)
  process.exit(2)
}
