// Tests for ledger-miner.mjs: the defect-class sidecar is the only input; rank aggregates it per
// class (weighted 5/3/1/0.5, later line for the same target+id wins), --area filters, --as-rows
// prints failure-modes.jsonl rows, --write renders between the definition-of-done markers, and
// feed appends one validated line.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only ledger-miner
import { check, run } from '../lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'

const T = mkdtempSync(join(tmpdir(), 'ledger-miner-'))
const SIDECAR = join(T, 'reviews', 'state', 'defect-classes.jsonl')
const DOD = join(T, 'memory-bank', 'standards', 'definition-of-done.md')
mkdirSync(join(T, 'reviews', 'state'), { recursive: true })
mkdirSync(join(T, 'memory-bank', 'standards'), { recursive: true })
const miner = (...args) => run('ledger-miner.mjs', ['--root', T, ...args])
const lines = (out, re) => out.split(/\r?\n/).filter(l => re.test(l))

{
  const r = miner('rank')
  check('rank without a sidecar exits 2 and names the file', r.code === 2 && r.out.includes('defect-classes.jsonl'), `exit ${r.code}: ${r.out.trim()}`)
}

const rows = [
  { target: '042-thumbnail-cache', id: 'PPW-1', sev: 'high', class: 'test-vacuity', area: 'storage', title: 'ImageProcessor mocked everywhere' },
  { target: '042-thumbnail-cache', id: 'PPW-2', sev: 'medium', class: 'test-vacuity', area: 'storage', title: 'green suite proves nothing about resize' },
  { target: '043-cloud-storage-provider', id: 'PPW-3', sev: 'low', class: 'caller-sweep', area: 'storage', title: 'second IStorageService caller missed' },
  { target: '015-sameday-shipping', id: 'PPW-4', sev: 'high', class: 'caller-sweep', area: 'sameday', title: 'AWB contract change left the locker path stale' },
  { target: '015-sameday-shipping', d: 'D7', sev: 'cleanup', class: 'doc-sync', area: 'docs', title: 'runbook names the old flag' },
  { target: '042-thumbnail-cache', id: 'PPW-1', sev: 'low', class: 'test-vacuity', area: 'storage', title: 'ImageProcessor mocked everywhere (downgraded)' },
]
writeFileSync(SIDECAR, rows.map(r => JSON.stringify(r)).join('\n') + '\n')

{
  const r = miner('rank')
  check('rank prints a ranked table and exits 0', r.code === 0 && /\|\s*#\s*\|\s*class\s*\|/i.test(r.out), `exit ${r.code}: ${r.out.trim()}`)
  const order = lines(r.out, /^\|\s*\d+\s*\|/).map(l => l.split('|')[2].trim())
  check('classes are ordered by weighted score (caller-sweep 6 > test-vacuity 4 > doc-sync 0.5)',
    JSON.stringify(order) === JSON.stringify(['caller-sweep', 'test-vacuity', 'doc-sync']), JSON.stringify(order))
  const vacuity = lines(r.out, /test-vacuity/)[0] ?? ''
  check('a later line for the same target+id supersedes the earlier one (PPW-1 counts as low, not high)',
    /\|\s*4\s*\|/.test(vacuity) && /\|\s*0\s*\|\s*1\s*\|\s*1\s*\|\s*0\s*\|/.test(vacuity), vacuity)
  check('a legacy "d" key is accepted as the id', /doc-sync/.test(r.out) && /D7/.test(r.out), r.out)
  check('the table lists the targets a class was seen in', /015-sameday-shipping/.test(lines(r.out, /caller-sweep/)[0] ?? ''), lines(r.out, /caller-sweep/)[0])
}
{
  const r = miner('rank', '--area', 'storage')
  const order = lines(r.out, /^\|\s*\d+\s*\|/).map(l => l.split('|')[2].trim())
  check('--area keeps only that area and re-ranks (test-vacuity 4 > caller-sweep 1)',
    r.code === 0 && JSON.stringify(order) === JSON.stringify(['test-vacuity', 'caller-sweep']), `exit ${r.code}: ${JSON.stringify(order)}`)
  check('--area drops classes with no lines in the area', !/doc-sync/.test(r.out), r.out)
}
{
  const r = miner('rank', '--area', 'storage', '--top', '1', '--as-rows')
  const out = r.out.trim().split(/\r?\n/)
  check('--as-rows prints exactly --top JSON lines', r.code === 0 && out.length === 1, `exit ${r.code}: ${r.out.trim()}`)
  let row = null
  try { row = JSON.parse(out[0]) } catch { row = null }
  check('an --as-rows line is a failure-modes.jsonl row with source class, the slug, example titles in fails, and blank expected/test',
    !!row && row.source === 'class' && row.class === 'test-vacuity' && /^FM-/.test(row.id) &&
    row.fails.includes('test-vacuity') && row.fails.includes('green suite proves nothing') && row.expected === '' && row.test === '', JSON.stringify(row))
}
{
  writeFileSync(DOD, '# DoD\n\nintro\n\n<!-- miner:ranked-classes:start -->\nold\n<!-- miner:ranked-classes:end -->\n\ntail\n')
  const r = miner('rank', '--write')
  const dod = readFileSync(DOD, 'utf8')
  check('--write replaces the block between the markers and keeps everything else', r.code === 0 && dod.startsWith('# DoD\n\nintro\n\n<!-- miner:ranked-classes:start -->\n') && dod.endsWith('<!-- miner:ranked-classes:end -->\n\ntail\n') && !dod.includes('\nold\n') && dod.includes('caller-sweep'), dod)
  miner('rank', '--write')
  check('--write is idempotent', readFileSync(DOD, 'utf8') === dod, 'second write changed the file')
  writeFileSync(DOD, '# DoD without markers\n')
  const r2 = miner('rank', '--write')
  check('--write without markers exits 1 and names them', r2.code === 1 && r2.out.includes('miner:ranked-classes:start'), `exit ${r2.code}: ${r2.out.trim()}`)
}
{
  const r = miner('feed', '044-045-observability', '--id', 'PPW-9', '--sev', 'medium', '--class', 'observability-floor', '--area', 'observability', '--title', 'scrubber bypassed')
  const last = readFileSync(SIDECAR, 'utf8').trim().split(/\r?\n/).pop()
  check('feed appends one line with target, id, sev, class, area, title', r.code === 0 && last === JSON.stringify({ target: '044-045-observability', id: 'PPW-9', sev: 'medium', class: 'observability-floor', area: 'observability', title: 'scrubber bypassed' }), `exit ${r.code}: ${last}`)
  const bad = miner('feed', '044-045-observability', '--id', 'PPW-10', '--sev', 'medium', '--class', 'made-up-class', '--area', 'observability', '--title', 'x')
  check('feed refuses an unknown class slug (exit 2) and lists the valid ones', bad.code === 2 && bad.out.includes('test-vacuity'), `exit ${bad.code}: ${bad.out.trim()}`)
  const badSev = miner('feed', '044-045-observability', '--id', 'PPW-10', '--sev', 'critical', '--class', 'other', '--area', 'observability', '--title', 'x')
  check('feed refuses an unknown severity (exit 2)', badSev.code === 2, `exit ${badSev.code}`)
  const badArea = miner('feed', '044-045-observability', '--id', 'PPW-10', '--sev', 'low', '--class', 'other', '--area', 'kitchen', '--title', 'x')
  check('feed refuses an unknown area (exit 2)', badArea.code === 2, `exit ${badArea.code}`)
  check('a refused feed appends nothing', readFileSync(SIDECAR, 'utf8').trim().split(/\r?\n/).length === rows.length + 1, 'sidecar grew')
}
{
  writeFileSync(SIDECAR, rows.map(r => JSON.stringify(r)).join('\n') + '\n{"target":"x","id":"PPW-99","sev":"low","class":"not-a-class","area":"storage","title":"t"}\n{broken\n')
  const r = miner('rank')
  check('a malformed line fails the run with its line number', r.code === 1 && /line 8/.test(r.out), `exit ${r.code}: ${r.out.trim()}`)
  writeFileSync(SIDECAR, rows.map(r => JSON.stringify(r)).join('\n') + '\n{"target":"x","id":"PPW-99","sev":"low","class":"not-a-class","area":"storage","title":"t"}\n')
  const r2 = miner('rank')
  check('an unknown class slug is counted under "other" with a warning', r2.code === 0 && /other/.test(r2.out) && /not-a-class/.test(r2.out), `exit ${r2.code}: ${r2.out.trim()}`)
}
{
  const r = miner('--help')
  check('--help lists the class and area slugs', r.code === 0 && r.out.includes('caller-sweep') && r.out.includes('sameday'), `exit ${r.code}`)
}
