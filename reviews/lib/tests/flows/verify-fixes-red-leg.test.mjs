// Tests for verify-fixes.mjs' red-leg classification: a red leg counts only when the runner
// output names a failing TEST. A compile error, or a non-zero exit that attributes nothing, is
// `revert-broke-build` — never proof (audit R6, SF39).
//
// Everything else about verify-fixes — the held path, worklog events, --no-events, multi-commit
// rows, env-missing, dirty-tree refusal, leaked git env — lives in verify-fixes.test.mjs.
// These runs pass --no-events so the worklog cannot dirty the fixture between legs.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only verify-fixes-red-leg
import { check, run, scrubbedGitEnv } from '../lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'
import { spawnSync } from 'node:child_process'

{
  const T = mkdtempSync(join(tmpdir(), 'verify-red-'))
  // A git hook's own GIT_DIR/GIT_INDEX_FILE would override -C and land these commits on the
  // real repository, so the throwaway repo gets a scrubbed environment.
  const g = (...a) => spawnSync('git', ['-C', T, ...a], { encoding: 'utf8', env: scrubbedGitEnv() })
  g('init', '-q', '-b', 'main')
  g('config', 'user.email', 'fixture@test'); g('config', 'user.name', 'fixture')
  mkdirSync(join(T, 'src', 'app'), { recursive: true })
  mkdirSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit'), { recursive: true })
  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'buggy\n')
  g('add', '.'); g('commit', '-qm', 'base')
  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'fixed\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit', 'CalcTests.cs'), 'test body\n')
  g('add', '.'); g('commit', '-qm', 'fix')
  const sha = g('rev-parse', '--short', 'HEAD').stdout.trim()
  mkdirSync(join(T, 'reviews', '950-verify-target'), { recursive: true })
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${sha}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9501 | fixed | \`${sha}\` | fixture fix |\n`)
  g('add', '.'); g('commit', '-qm', 'resolution')

  const verify = tpl => run('verify/verify-fixes.mjs',
    ['--root', T, '950-verify-target', '--no-events', '--test-cmd-api', tpl])

  // A test that names its failure: the only shape that counts as red.
  const named = `node -e "if(require('fs').readFileSync('src/app/calc.txt','utf8').includes('buggy')){console.log('Failed CalcTests.Fixture');process.exit(1)}"`
  const live = verify(named)
  check('verify-fixes records which failing test made the red leg red',
    live.out.includes('"red_reasons":["test-failed"]') && live.out.includes('Failed CalcTests.Fixture'),
    live.out.trim())
  check('a named test failure still reports held', live.out.includes('"verdict":"held"'), live.out.trim())

  // Reverting the fix broke the build instead of failing a test — no proof either way.
  const buildFail = `node -e "if(require('fs').readFileSync('src/app/calc.txt','utf8').includes('buggy')){console.log('error CS1002: ; expected');console.log('Build FAILED.');process.exit(1)}"`
  const broke = verify(buildFail)
  check('verify-fixes reads a compile-error red leg as revert-broke-build, never red',
    broke.code === 1 && broke.out.includes('"verdict":"revert-broke-build"'), broke.out.trim())
  check('the build-broke leg records why it was refused',
    broke.out.includes('"red_reasons":["build-broke"]'), broke.out.trim())

  // Non-zero with nothing attributable: also not proof.
  const silentRed = `node -e "process.exit(require('fs').readFileSync('src/app/calc.txt','utf8').includes('buggy')?1:0)"`
  const silent = verify(silentRed)
  check('verify-fixes refuses a non-zero red leg that names no failing test',
    silent.code === 1 && silent.out.includes('"verdict":"revert-broke-build"'), silent.out.trim())
  check('the unattributed leg says so rather than claiming a test failed',
    silent.out.includes('"red_reasons":["unattributed"]'), silent.out.trim())

  check('the red-leg runs leave the fixture tree clean',
    g('status', '--porcelain').stdout.trim() === '', g('status', '--porcelain').stdout)

  rmSync(T, { recursive: true, force: true })
}
