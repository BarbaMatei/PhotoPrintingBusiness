// Tests for wl.mjs: the validated worklog-event stamper, its shapes, voids, and in-process appendEvent.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only wl
import { check, run } from './lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'

// ---------- wl: the validated worklog stamper ----------
{
  const T = mkdtempSync(join(tmpdir(), 'wl-'))
  const target = '930-wl-target'
  mkdirSync(join(T, 'reviews', target), { recursive: true })
  writeFileSync(join(T, 'reviews', target, 'resolution-v1.md'), '---\nstatus: resolved\n---\n')
  const wlPath = join(T, 'reviews', target, 'worklog.jsonl')
  const lines = () => existsSync(wlPath) ? readFileSync(wlPath, 'utf8').split(/\r?\n/).filter(l => l.trim()) : []

  let r = run('wl.mjs', ['--root', T, target, 'pass-launch', '--pass', 'v1', '--type', 'full discovery'])
  check('wl appends a valid event and exits 0', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('wl prints exactly the appended line', lines().length === 1 && r.out.trim() === lines()[0], r.out.trim())
  let firstEvent = lines().length ? JSON.parse(lines()[0]) : null
  check('wl stamps a timestamp with the local UTC offset', !!firstEvent && /[+-]\d{2}:\d{2}$/.test(firstEvent.t), JSON.stringify(firstEvent))

  r = run('wl.mjs', ['--root', T, target, 'not-a-real-event'])
  check('wl refuses an unknown ev', r.code === 1 && r.out.includes('ERROR') && r.out.includes('unknown ev'), r.out.trim())
  check('wl appended nothing for the unknown ev', lines().length === 1, String(lines().length))

  const shapeCases = [
    ['round-start', [], 'round'],
    ['triage-done', ['--round', '1'], 'clusters'],
    ['gate-open', [], 'reason'],
    ['gate-parked', ['--kind', 'fixer-decision', '--default', 'deferred'], 'reason'],
    ['test-run', [], 'kind'],
    ['finding', ['--id', 'PPW-1'], 'status'],
    ['micro-review-dispatched', [], 'cluster'],
    ['pass-launch', ['--pass', 'v1'], 'type'],
    ['pass-records-done', [], 'pass'],
    ['verify-result', ['--id', 'PPW-1'], 'verdict'],
    ['void', [], 'of'],
  ]
  for (const [ev, args, field] of shapeCases) {
    const before = lines().length
    const rr = run('wl.mjs', ['--root', T, target, ev, ...args])
    check(`wl refuses ${ev} missing "${field}"`, rr.code === 1 && rr.out.includes('ERROR'), rr.out.trim())
    check(`wl appends nothing for ${ev} missing "${field}"`, lines().length === before, String(lines().length))
  }

  r = run('wl.mjs', ['--root', T, target, 'test-run', '--kind', 'bogus'])
  check('wl refuses a test-run kind outside the enum', r.code === 1 && r.out.includes('ERROR'), r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'finding', '--id', 'BUG-1', '--status', 'fixed'])
  check('wl refuses a finding id not shaped like PPW-<n>', r.code === 1 && r.out.includes('ERROR'), r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'run-start', '--t', '2020-01-01T00:00:00+00:00'])
  check('wl refuses a passed --t (the stamper owns time)', r.code === 1 && r.out.includes('ERROR'), r.out.trim())
  check('wl appends nothing when --t is passed', lines().length === 1, String(lines().length))

  r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '7'])
  check('wl refuses round-start when resolution-v7.md is missing', r.code === 1 && r.out.includes('ERROR') && r.out.includes('resolution-v7'), r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '1'])
  check('wl accepts round-start 1 (resolution-v1.md exists)', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '2'])
  check('wl refuses round-start 2 while round 1 is still open', r.code === 1 && r.out.includes('ERROR'), r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-end', '--round', '2'])
  check('wl refuses round-end 2 with no open round-start for 2', r.code === 1 && r.out.includes('ERROR'), r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-end', '--round', '1'])
  check('wl accepts round-end 1, closing the open round', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '1'])
  check('wl accepts a same-number restart after round-end (multi-part round)', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-end', '--round', '1'])
  check('wl closes the restarted round', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'void', '--json', '{"of":{"ev":"round-start","t":"1999-01-01T00:00:00+00:00"}}'])
  check('wl refuses a void with no matching event', r.code === 1 && r.out.includes('ERROR') && r.out.includes('closest timestamps'), r.out.trim())

  if (firstEvent) {
    r = run('wl.mjs', ['--root', T, target, 'void', '--json', JSON.stringify({ of: { ev: firstEvent.ev, t: firstEvent.t } })])
    check('wl accepts a void matching an existing event', r.code === 0, r.out.trim())
    check('wl worklog grew by exactly one line for the accepted void', lines().length === 6, String(lines().length))
  } else {
    check('wl accepts a void matching an existing event', false, 'no earlier event was captured to void')
  }

  rmSync(T, { recursive: true, force: true })
}
{
  const T = mkdtempSync(join(tmpdir(), 'wl-void-'))
  const target = '932-wl-void'
  mkdirSync(join(T, 'reviews', target), { recursive: true })
  for (const n of [1, 2]) writeFileSync(join(T, 'reviews', target, `resolution-v${n}.md`), '---\nstatus: resolved\n---\n')
  const wlPath = join(T, 'reviews', target, 'worklog.jsonl')
  const lines = () => existsSync(wlPath) ? readFileSync(wlPath, 'utf8').split(/\r?\n/).filter(l => l.trim()) : []

  let r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '1'])
  check('wl opens round 1', r.code === 0, r.out.trim())
  const opened = lines().length ? JSON.parse(lines()[0]) : null

  r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '1'])
  check('wl refuses a repeat round-start for the round already open', r.code === 1 && r.out.includes('ERROR') && r.out.includes('still open'), r.out.trim())
  check('wl appended nothing for the repeat round-start', lines().length === 1, String(lines().length))

  if (opened) {
    r = run('wl.mjs', ['--root', T, target, 'void', '--json', JSON.stringify({ of: { ev: 'round-start', t: opened.t, round: 1 } })])
    check('wl voids the round-start it just stamped', r.code === 0, r.out.trim())
    r = run('wl.mjs', ['--root', T, target, 'round-start', '--round', '2'])
    check('a voided round-start no longer holds the round open for the stamper', r.code === 0, r.out.trim())
  } else {
    check('a voided round-start no longer holds the round open for the stamper', false, 'no round-start was captured to void')
  }

  rmSync(T, { recursive: true, force: true })
}
{
  const T = mkdtempSync(join(tmpdir(), 'wl-inprocess-'))
  const target = '931-wl-inprocess'
  mkdirSync(join(T, 'reviews', target), { recursive: true })
  try {
    const { appendEvent } = await import('../wl.mjs')
    const stamped = appendEvent(T, target, { ev: 'note', text: 'in-process call' })
    check('appendEvent returns the stamped event with an offset timestamp',
      stamped.ev === 'note' && /[+-]\d{2}:\d{2}$/.test(stamped.t), JSON.stringify(stamped))
    const written = readFileSync(join(T, 'reviews', target, 'worklog.jsonl'), 'utf8').trim()
    check('appendEvent wrote exactly the stamped line to disk', written === JSON.stringify(stamped), written)
  } catch (e) {
    check('appendEvent is importable and usable in-process', false, String(e))
  }
  rmSync(T, { recursive: true, force: true })
}
{
  const T = mkdtempSync(join(tmpdir(), 'wl-archive-'))
  const target = '933-wl-archived'
  mkdirSync(join(T, 'reviews', 'archive', target), { recursive: true })
  writeFileSync(join(T, 'reviews', 'archive', target, 'resolution-v1.md'), '---\nstatus: resolved\n---\n')
  const r = run('wl.mjs', ['--root', T, target, 'note', '--text', 'archived target event'])
  check('wl appends for an archived target and exits 0', r.code === 0, r.out.trim())
  check('the event lands in the archive folder', existsSync(join(T, 'reviews', 'archive', target, 'worklog.jsonl')), 'reviews/archive/' + target + '/worklog.jsonl is missing')
  check('no stray reviews/<target>/ folder is created for an archived target', !existsSync(join(T, 'reviews', target)), 'reviews/' + target + ' should not exist')
  rmSync(T, { recursive: true, force: true })
}
{
  const T = mkdtempSync(join(tmpdir(), 'wl-audit-'))
  const target = '934-wl-audit-events'
  mkdirSync(join(T, 'reviews', target), { recursive: true })
  writeFileSync(join(T, 'reviews', target, 'resolution-v1.md'), '---\nstatus: resolved\n---\n')
  const wlPath = join(T, 'reviews', target, 'worklog.jsonl')
  const lines = () => existsSync(wlPath) ? readFileSync(wlPath, 'utf8').split(/\r?\n/).filter(l => l.trim()) : []

  let r = run('wl.mjs', ['--root', T, target, 'protocol-written', '--round', '1', '--cluster', 'c1', '--ids', 'PPW-1,PPW-2'])
  check('wl accepts protocol-written with round/cluster/ids', r.code === 0, r.out.trim())
  const written = lines().length ? JSON.parse(lines()[lines().length - 1]) : null
  check('wl parses --ids as an array via comma-split', !!written && Array.isArray(written.ids) && written.ids.length === 2 && written.ids[0] === 'PPW-1' && written.ids[1] === 'PPW-2', JSON.stringify(written))

  r = run('wl.mjs', ['--root', T, target, 'check-dispatched', '--round', '1', '--cluster', 'c1', '--json', '{"ids":["PPW-3"]}'])
  check('wl accepts check-dispatched with round/cluster/ids', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'check-returned', '--round', '1', '--cluster', 'c1', '--verdict', 'cleared'])
  check('wl accepts check-returned with round/cluster/verdict', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'check-returned', '--round', '1', '--cluster', 'c1', '--verdict', 'cleared', '--tokens', '1200'])
  check('wl still accepts check-returned with the optional tokens field', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-review-dispatched', '--round', '1'])
  check('wl accepts round-review-dispatched with round', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'round-review-returned', '--round', '1', '--found', '0'])
  check('wl accepts round-review-returned with round/found', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'test-audit-dispatched', '--round', '1'])
  check('wl accepts test-audit-dispatched with round', r.code === 0, r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'test-audit-returned', '--round', '1', '--verdict', 'pass'])
  check('wl accepts test-audit-returned with round/verdict', r.code === 0, r.out.trim())

  const auditShapeCases = [
    ['protocol-written', ['--round', '1', '--cluster', 'c1'], 'ids'],
    ['check-dispatched', ['--round', '1', '--cluster', 'c1'], 'ids'],
    ['check-returned', ['--round', '1', '--cluster', 'c1'], 'verdict'],
    ['round-review-dispatched', [], 'round'],
    ['round-review-returned', ['--round', '1'], 'found'],
    ['test-audit-dispatched', [], 'round'],
    ['test-audit-returned', ['--round', '1'], 'verdict'],
  ]
  for (const [ev, args, field] of auditShapeCases) {
    const before = lines().length
    const rr = run('wl.mjs', ['--root', T, target, ev, ...args])
    check(`wl refuses ${ev} missing "${field}"`, rr.code === 1 && rr.out.includes('ERROR'), rr.out.trim())
    check(`wl appends nothing for ${ev} missing "${field}"`, lines().length === before, String(lines().length))
  }

  r = run('wl.mjs', ['--root', T, target, 'protocol-written', '--round', '1', '--cluster', 'c1', '--ids', 'BUG-1'])
  check('wl refuses ids not shaped like PPW-<n>', r.code === 1 && r.out.includes('ERROR'), r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'protocol-written', '--round', '1', '--cluster', 'c1', '--json', '{"ids":[]}'])
  check('wl refuses an empty ids array', r.code === 1 && r.out.includes('ERROR'), r.out.trim())

  r = run('wl.mjs', ['--root', T, target, 'check-dispatched', '--round', '1', '--cluster', 'c1', '--json', '{"ids":"PPW-1"}'])
  check('wl refuses ids that is a string instead of an array', r.code === 1 && r.out.includes('ERROR'), r.out.trim())

  rmSync(T, { recursive: true, force: true })
}
