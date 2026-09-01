// Flow: the pre-commit hook itself over a throwaway repo — a staged comment line is blocked, and
// an override leaves a parsable trace in reviews/state/overrides.jsonl.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only hook-override
import { check, scrubbedGitEnv } from '../lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'
import { spawnSync } from 'node:child_process'
import { REVIEWS } from '../../records/schema.mjs'

// ---------- pre-commit hook: gate overrides leave a trace in the override log ----------
{
  const sh = spawnSync('sh', ['-c', 'true'], { encoding: 'utf8' })
  if (sh.error || sh.status !== 0) {
    console.log('note: sh unavailable — the pre-commit override-log checks were skipped')
  } else {
    const T = mkdtempSync(join(tmpdir(), 'hook-override-'))
    // A git hook's own GIT_DIR/GIT_INDEX_FILE would override -C and land these commits
    // on the real repository, so the throwaway repo gets a scrubbed environment.
    const g = (...a) => spawnSync('git', ['-C', T, ...a], { encoding: 'utf8', env: scrubbedGitEnv() })
    g('init', '-q', '-b', 'main')
    g('config', 'user.email', 'fixture@test'); g('config', 'user.name', 'fixture')
    mkdirSync(join(T, 'src'), { recursive: true })
    writeFileSync(join(T, 'src', 'Fixture.cs'), 'var x = 1; // narrating comment\n')
    g('add', '.')
    const hook = join(REVIEWS, '..', '.githooks', 'pre-commit')
    const blocked = spawnSync('sh', [hook], { cwd: T, encoding: 'utf8', env: { ...scrubbedGitEnv(), COMMENTS_OK: '', DOCGATE_OK: '' } })
    check('hook blocks a staged comment line without an override', blocked.status === 1 && !existsSync(join(T, 'reviews', 'state', 'overrides.jsonl')), `exit ${blocked.status}: ${(blocked.stderr ?? '').trim().slice(0, 200)}`)
    const overridden = spawnSync('sh', [hook], { cwd: T, encoding: 'utf8', env: { ...scrubbedGitEnv(), COMMENTS_OK: '1', DOCGATE_OK: '' } })
    const logPath = join(T, 'reviews', 'state', 'overrides.jsonl')
    const logged = existsSync(logPath) ? readFileSync(logPath, 'utf8') : ''
    check('hook passes with COMMENTS_OK=1 but logs the override', overridden.status === 0 && logged.includes('"var":"COMMENTS_OK"') && logged.includes('src/Fixture.cs'), `exit ${overridden.status}: log=${logged.trim() || '(missing)'}`)
    check('the override-log line parses as JSON with a timestamp', (() => { try { const o = JSON.parse(logged.trim().split('\n')[0]); return Number.isFinite(Date.parse(o.t)) } catch { return false } })(), logged.trim())
    rmSync(T, { recursive: true, force: true })
  }
}
