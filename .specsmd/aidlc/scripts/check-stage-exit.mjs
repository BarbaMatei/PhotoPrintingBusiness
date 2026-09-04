#!/usr/bin/env node
import { existsSync, readFileSync, readdirSync } from 'node:fs'
import { join, resolve } from 'node:path'

const USAGE = `Usage: node .specsmd/aidlc/scripts/check-stage-exit.mjs <bolt-id> <stage> [--root <repo>]

Answers whether a bolt may leave a stage, from the files in memory-bank/bolts/<bolt-id>/:

  failure-modes.jsonl   one JSON object per failure mode. Fields: id (FM-<n>), source
                        (bolt | class | invariant | attack), fails, expected, test (a fragment
                        usable verbatim as the wrapper's --filter, or --include when "ui": true),
                        log, disposition (accepted | rejected), reason.
                        {"none": true, "reason": "..."} says the bolt has no failure modes.
                        {"source": "attack", "none": true, "reason": "..."} says the adversarial
                        design check ran and found nothing to add.
  test-stamps.jsonl     written by reviews/lib/run-scoped-tests.mjs --log <this file>.

Conditions by stage (ddd: technical-design, implement, test · simple: plan, implement, test):
  design / plan   every accepted row names a test; every rejected row has a reason; every attack
                  row has a disposition; at least one attack row exists.
  implement       the above, and per accepted test: a red stamp (exit != 0) earlier than the first
                  green stamp (exit 0); a revert-and-rerun stamp with exit != 0 and a mutate record
                  naming a non-test file; a green stamp after that mutation.
  test            the above, and the test artifact (ddd-03-test-report.md / test-walkthrough.md)
                  has an "Adversarial design check" and a "Fresh-eyes micro-review" heading.
Spike bolts and stages with no conditions exit 0 with a note.

Exit: 0 conditions met (one "ok:" line) · 1 unmet (one "- " line per unmet condition) · 2 usage.`

const TYPES = {
  'ddd-construction-bolt': {
    stages: { 'domain-model': [], 'technical-design': ['design'], 'adr-analysis': [], implement: ['design', 'implement'], test: ['design', 'implement', 'test'] },
    testArtifact: 'ddd-03-test-report.md',
  },
  'simple-construction-bolt': {
    stages: { plan: ['design'], implement: ['design', 'implement'], test: ['design', 'implement', 'test'] },
    testArtifact: 'test-walkthrough.md',
  },
  'spike-bolt': { exempt: true },
}
const TEST_PATH_RE = /(^|[\\/])PhotoPrint\.Tests[\\/]|\.spec\.ts$|(^|[\\/])tests?[\\/]/
const GATE_HEADINGS = [/adversarial design check/i, /fresh-eyes micro-review/i]

function usage(message) {
  console.error(`usage: ${message}\n\n${USAGE}`)
  process.exit(2)
}

function parseArgs(argv) {
  const args = { root: process.cwd(), positional: [] }
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--help' || argv[i] === '-h') { console.log(USAGE); process.exit(0) }
    else if (argv[i] === '--root') args.root = resolve(argv[++i])
    else args.positional.push(argv[i])
  }
  if (args.positional.length !== 2) usage('wants exactly <bolt-id> <stage>')
  return args
}

function findBoltDir(root, id) {
  const bolts = join(root, 'memory-bank', 'bolts')
  const direct = join(bolts, id)
  if (existsSync(join(direct, 'bolt.md'))) return direct
  if (!existsSync(bolts)) return null
  const match = readdirSync(bolts).find(name => name.startsWith(`${id}-`) || name.endsWith(`-${id}`))
  return match && existsSync(join(bolts, match, 'bolt.md')) ? join(bolts, match) : null
}

function boltType(boltDir) {
  const text = readFileSync(join(boltDir, 'bolt.md'), 'utf8')
  const fm = /^---\r?\n([\s\S]*?)\r?\n---/.exec(text)
  const m = fm && /^type:[ \t]*["']?([\w-]+)/m.exec(fm[1])
  return m ? m[1] : null
}

function readJsonl(path) {
  const rows = [], errors = []
  readFileSync(path, 'utf8').split(/\r?\n/).forEach((line, i) => {
    if (!line.trim()) return
    try { rows.push({ ...JSON.parse(line), _line: i + 1 }) } catch (e) { errors.push(`${path.split(/[\\/]/).pop()} line ${i + 1} is not JSON: ${e.message}`) }
  })
  return { rows, errors }
}

function loadFailureModes(boltDir) {
  const path = join(boltDir, 'failure-modes.jsonl')
  if (!existsSync(path)) return { missing: true, rows: [], errors: [] }
  return { missing: false, ...readJsonl(path) }
}

function rowLabel(row) {
  return typeof row.id === 'string' && row.id ? row.id : `line ${row._line}`
}

function checkDesign(fm) {
  if (fm.missing) return ['failure-modes.jsonl is missing: list every failure mode before the stage exits (see --help for the row shape)']
  const unmet = [...fm.errors]
  const noneRow = fm.rows.find(r => r.none && r.source !== 'attack')
  if (noneRow && !(typeof noneRow.reason === 'string' && noneRow.reason.trim())) unmet.push(`${rowLabel(noneRow)}: a "none" row needs a reason why this bolt has no failure modes`)
  if (!fm.rows.some(r => r.source === 'attack')) unmet.push('no attack row: the adversarial design check has not run (append its rows with "source": "attack", or {"source": "attack", "none": true, "reason": "..."})')
  const seen = new Set()
  for (const row of fm.rows) {
    if (row.none) continue
    const label = rowLabel(row)
    if (typeof row.id === 'string') {
      if (seen.has(row.id)) unmet.push(`${label}: duplicate id`)
      seen.add(row.id)
    }
    if (row.source === 'attack' && row.disposition === undefined) { unmet.push(`${label}: attack row has no disposition (accepted with a test, or rejected with a reason)`); continue }
    const disposition = row.disposition ?? 'accepted'
    if (disposition === 'rejected') {
      if (!(typeof row.reason === 'string' && row.reason.trim())) unmet.push(`${label}: rejected row has no reason`)
      continue
    }
    if (disposition !== 'accepted') { unmet.push(`${label}: disposition must be accepted or rejected, not "${disposition}"`); continue }
    if (!noneRow && !(typeof row.test === 'string' && row.test.trim())) unmet.push(`${label}: accepted row names no test (a fragment usable verbatim as --filter / --include)`)
    if (!(typeof row.fails === 'string' && row.fails.trim()) || !(typeof row.expected === 'string' && row.expected.trim())) unmet.push(`${label}: row needs both "fails" and "expected"`)
  }
  return unmet
}

function acceptedTests(fm) {
  if (fm.missing || fm.rows.some(r => r.none && r.source !== 'attack')) return []
  return fm.rows
    .filter(r => !r.none && (r.disposition ?? 'accepted') === 'accepted' && typeof r.test === 'string' && r.test.trim())
    .map(r => ({ label: rowLabel(r), test: r.test.trim() }))
}

function checkImplement(boltDir, fm) {
  const tests = acceptedTests(fm)
  if (tests.length === 0) return []
  const path = join(boltDir, 'test-stamps.jsonl')
  if (!existsSync(path)) return ['test-stamps.jsonl is missing: run every failure-mode test through reviews/lib/run-scoped-tests.mjs with --log memory-bank/bolts/<id>/test-stamps.jsonl']
  const { rows: stamps, errors } = readJsonl(path)
  const unmet = [...errors]
  const time = s => Date.parse(s.t) || 0
  const sorted = [...stamps].sort((a, b) => time(a) - time(b))
  for (const { label, test } of tests) {
    const mine = sorted.filter(s => {
      const name = s.filter ?? s.include
      return typeof name === 'string' && name && (test.includes(name) || name.includes(test))
    })
    const first = condition => { unmet.push(`${label} (${test}): ${condition}`) }
    if (mine.length === 0) { first('no stamp matches this test'); continue }
    const greens = mine.filter(s => s.kind === 'green' && s.exit === 0)
    if (greens.length === 0) { first('no green stamp (exit 0)'); continue }
    const firstGreen = time(greens[0])
    if (!mine.some(s => s.kind === 'red' && s.exit !== 0 && time(s) < firstGreen)) { first('no red stamp (exit != 0) before the first green: the test was not seen failing before the code'); continue }
    const reverts = mine.filter(s => s.kind === 'revert-and-rerun')
    if (reverts.length === 0) { first('no revert-and-rerun stamp: run it with --mutate <file>:<line> after the green'); continue }
    const revert = reverts[reverts.length - 1]
    if (revert.exit === 0) { first('the revert-and-rerun stayed green (exit 0): the mutation did not turn the test red, so the test does not protect that line'); continue }
    if (!revert.mutate || typeof revert.mutate.file !== 'string') { first('the revert-and-rerun has no mutate record: let the wrapper break the line with --mutate <file>:<line>'); continue }
    if (TEST_PATH_RE.test(revert.mutate.file)) { first(`the revert-and-rerun mutated a test file (${revert.mutate.file}): mutate the production line the test protects`); continue }
    if (!greens.some(s => time(s) > time(revert))) first('no green stamp after the mutation: the restore is not proven')
  }
  return unmet
}

function checkTest(boltDir, type) {
  const artifact = TYPES[type].testArtifact
  const path = join(boltDir, artifact)
  if (!existsSync(path)) return [`${artifact} is missing: the test stage artifact records both gates`]
  const headings = readFileSync(path, 'utf8').split(/\r?\n/).filter(l => /^#{1,6}\s/.test(l))
  const unmet = []
  for (const [re, name] of [[GATE_HEADINGS[0], 'Adversarial design check'], [GATE_HEADINGS[1], 'Fresh-eyes micro-review']]) {
    if (!headings.some(h => re.test(h))) unmet.push(`${artifact}: no "${name}" heading — record that gate's run and what it found`)
  }
  return unmet
}

function main() {
  const { root, positional: [id, stage] } = parseArgs(process.argv.slice(2))
  const boltDir = findBoltDir(root, id)
  if (!boltDir) usage(`no bolt "${id}" under ${join(root, 'memory-bank', 'bolts')}`)
  const bolt = boltDir.split(/[\\/]/).pop()
  const type = boltType(boltDir)
  if (!type || !TYPES[type]) usage(`bolt ${bolt} has type "${type}", not one of ${Object.keys(TYPES).join(', ')}`)
  if (TYPES[type].exempt) { console.log(`ok: ${bolt} is a ${type} — exempt from stage exit conditions`); return }
  const checks = TYPES[type].stages[stage]
  if (!checks) usage(`stage "${stage}" is not a ${type} stage (${Object.keys(TYPES[type].stages).join(', ')})`)
  if (checks.length === 0) { console.log(`ok: ${bolt} ${stage} — no exit conditions for this stage`); return }

  const fm = loadFailureModes(boltDir)
  const unmet = []
  if (checks.includes('design')) unmet.push(...checkDesign(fm))
  if (checks.includes('implement')) unmet.push(...checkImplement(boltDir, fm))
  if (checks.includes('test')) unmet.push(...checkTest(boltDir, type))

  if (unmet.length === 0) {
    const stampsPath = join(boltDir, 'test-stamps.jsonl')
    const stamps = existsSync(stampsPath) ? readJsonl(stampsPath).rows.length : 0
    console.log(`ok: ${bolt} ${stage} — ${fm.rows.length} failure-mode row(s), ${stamps} stamp(s)`)
    return
  }
  console.log(`${bolt} ${stage}: ${unmet.length} unmet exit condition(s)`)
  for (const line of unmet) console.log(`- ${line}`)
  process.exit(1)
}

main()
