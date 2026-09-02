// Flow: verify-fixes driving revert-and-rerun against a throwaway git repo — the held path,
// worklog events via wl.mjs's appendEvent, --no-events, multi-commit rows, env-missing, the
// dirty-tree refusal, and a leaked git env. The red-leg classification lives next door in
// verify-fixes-red-leg.test.mjs.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only verify-fixes
import { check, run, fixtureGit } from '../lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'
import { spawnSync } from 'node:child_process'
import { REVIEWS, REPO } from '../../records/schema.mjs'

// ---------- verify-fixes: revert-and-rerun against a throwaway repo ----------
{
  const T = mkdtempSync(join(tmpdir(), 'verify-fixes-'))
  const g = fixtureGit(T)
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
  // Prints a failing-test line: since SF39 a red leg counts only when one is named.
  const redGreen = `node -e "if(require('fs').readFileSync('src/app/calc.txt','utf8').includes('buggy')){console.log('Failed CalcTests.Fixture');process.exit(1)}"`

  const wlRel = 'reviews/950-verify-target/worklog.jsonl'
  const wlPath = join(T, wlRel)
  const wlLines = () => existsSync(wlPath) ? readFileSync(wlPath, 'utf8').split(/\r?\n/).filter(Boolean).map(l => JSON.parse(l)) : []
  const statusLines = () => g('status', '--porcelain').stdout.trim().split(/\r?\n/).filter(Boolean)
  const commitWorklog = msg => { g('add', wlRel); g('commit', '-qm', msg) }

  const dry = run('verify/verify-fixes.mjs', ['--root', T, '950-verify-target', '--dry-run'])
  check('verify-fixes dry-run derives the plan', dry.code === 0 && dry.out.includes('calc.txt') && dry.out.includes('PhotoPrint.Tests.Unit.CalcTests'), dry.out.trim())
  check('verify-fixes --dry-run appends no worklog event', wlLines().length === 0, JSON.stringify(wlLines()))

  const live = run('verify/verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen])
  check('verify-fixes proves red-then-green and reports held', live.code === 0 && live.out.includes('"verdict":"held"') && live.out.includes('SUMMARY: 1/1 held'), live.out.trim())
  check("verify-fixes warns when HEAD has moved past the resolution's fixed_commit",
    live.out.includes(`warning: HEAD is not the resolution's fixed_commit ${sha}`), live.out.trim())
  {
    const verifyResults = wlLines().filter(e => e.ev === 'verify-result')
    check('verify-fixes appends exactly one verify-result event for the held row', verifyResults.length === 1, JSON.stringify(verifyResults))
    check('the verify-result event names PPW-9501 held', verifyResults[0]?.id === 'PPW-9501' && verifyResults[0]?.verdict === 'held', JSON.stringify(verifyResults))
  }
  check('verify-fixes leaves the tree clean except the worklog it just wrote',
    statusLines().length === 1 && statusLines()[0].endsWith(wlRel), g('status', '--porcelain').stdout)
  commitWorklog('worklog')

  const liveNoEvents = run('verify/verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen, '--no-events'])
  check('verify-fixes --no-events reports held without appending', liveNoEvents.code === 0 && liveNoEvents.out.includes('"verdict":"held"'), liveNoEvents.out.trim())
  check('verify-fixes --no-events appends no additional worklog event', wlLines().filter(e => e.ev === 'verify-result').length === 1, JSON.stringify(wlLines()))

  {
    const decoyT = mkdtempSync(join(tmpdir(), 'decoy-'))
    const dg = fixtureGit(decoyT)
    dg('init', '-q', '-b', 'main')
    dg('config', 'user.email', 'decoy@test'); dg('config', 'user.name', 'decoy')
    writeFileSync(join(decoyT, 'marker.txt'), 'untouched\n')
    dg('add', '.'); dg('commit', '-qm', 'decoy base')
    const decoyHead = dg('rev-parse', 'HEAD').stdout.trim()
    const leakedEnv = { ...process.env, GIT_DIR: join(decoyT, '.git'), GIT_WORK_TREE: decoyT, GIT_COMMON_DIR: join(decoyT, '.git') }
    const leaked = spawnSync(process.execPath, [join(REVIEWS, 'lib', 'verify-fixes.mjs'), '--root', T, '950-verify-target', '--test-cmd-api', redGreen, '--no-events'], { encoding: 'utf8', cwd: REPO, env: leakedEnv })
    const leakedOut = `${leaked.stdout ?? ''}${leaked.stderr ?? ''}`
    check('verify-fixes ignores a leaked GIT_DIR/GIT_WORK_TREE/GIT_COMMON_DIR and still verifies the real target',
      leaked.status === 0 && leakedOut.includes('"verdict":"held"'), leakedOut.trim())
    check('verify-fixes leaves the real fixture tree clean despite the leaked env', g('status', '--porcelain').stdout.trim() === '', g('status', '--porcelain').stdout)
    check('verify-fixes never touches the decoy repo the leaked env pointed at',
      dg('rev-parse', 'HEAD').stdout.trim() === decoyHead && dg('status', '--porcelain').stdout.trim() === '',
      `decoy HEAD now ${dg('rev-parse', 'HEAD').stdout.trim()} (was ${decoyHead}); status: ${dg('status', '--porcelain').stdout.trim()}`)
    check('the decoy marker file is untouched', readFileSync(join(decoyT, 'marker.txt'), 'utf8') === 'untouched\n', readFileSync(join(decoyT, 'marker.txt'), 'utf8'))
    rmSync(decoyT, { recursive: true, force: true })
  }

  const neverRed = run('verify/verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', 'node -e "process.exit(0)"', '--no-events'])
  check('verify-fixes reopens a fix whose test never goes red', neverRed.code === 1 && neverRed.out.includes('"verdict":"test-never-red"'), neverRed.out.trim())

  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'dirty\n')
  const dirty = run('verify/verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen])
  check('verify-fixes refuses a dirty tree', dirty.code === 2, `exit ${dirty.code}: ${dirty.out.trim()}`)
  g('checkout', '--', '.')

  // A follow-up commit is listed too: reverting only the first leaves the test green for the wrong reason.
  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'fixed and polished\n')
  g('add', '.'); g('commit', '-qm', 'follow-up')
  const sha2 = g('rev-parse', '--short', 'HEAD').stdout.trim()
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${sha2}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9501 | fixed | \`${sha}\`, \`${sha2}\` | fixture fix with a follow-up |\n| PPW-9502 | fixed | — | fixture row whose cell names no commit |\n`)
  g('add', '.'); g('commit', '-qm', 'resolution with two commits')
  const multi = run('verify/verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen, '--no-events'])
  check('verify-fixes covers a row whose Commit cell lists two commits', multi.out.includes('"id":"PPW-9501"') && multi.out.includes('"verdict":"held"'), multi.out.trim())
  check('verify-fixes never skips a fixed row it cannot parse', multi.out.includes('"id":"PPW-9502"') && multi.out.includes('"verdict":"unparsable-commit"'), multi.out.trim())
  check('verify-fixes counts both rows in its summary', multi.out.includes('SUMMARY: 1/2 held') && multi.code === 1, `exit ${multi.code}: ${multi.out.trim()}`)
  check('verify-fixes leaves the tree clean after a multi-commit revert', g('status', '--porcelain').stdout.trim() === '', g('status', '--porcelain').stdout)

  // Two rows each completing a full revert -> red -> restore -> green cycle in one run.
  mkdirSync(join(T, 'src', 'app2'), { recursive: true })
  writeFileSync(join(T, 'src', 'app2', 'a.txt'), 'buggyA\n')
  writeFileSync(join(T, 'src', 'app2', 'b.txt'), 'buggyB\n')
  g('add', '.'); g('commit', '-qm', 'two-row base')
  writeFileSync(join(T, 'src', 'app2', 'a.txt'), 'fixedA\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit', 'ATests.cs'), 'test a\n')
  g('add', '.'); g('commit', '-qm', 'fix a')
  const shaA = g('rev-parse', '--short', 'HEAD').stdout.trim()
  writeFileSync(join(T, 'src', 'app2', 'b.txt'), 'fixedB\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit', 'BTests.cs'), 'test b\n')
  g('add', '.'); g('commit', '-qm', 'fix b')
  const shaB = g('rev-parse', '--short', 'HEAD').stdout.trim()
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${shaB}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9510 | fixed | \`${shaA}\` | two-row fixture: row A |\n| PPW-9511 | fixed | \`${shaB}\` | two-row fixture: row B |\n`)
  g('add', '.'); g('commit', '-qm', 'two-row resolution')
  const twoRowTpl = `node -e "const fs=require('fs'); const f=process.argv[1].indexOf('ATests')>=0?'src/app2/a.txt':'src/app2/b.txt'; if(fs.readFileSync(f,'utf8').indexOf('buggy')>=0){console.log('Failed '+process.argv[1]);process.exit(1)}" {filter}`
  const twoRow = run('verify/verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', twoRowTpl])
  check('verify-fixes holds both rows of the two-row run', twoRow.code === 0 && twoRow.out.includes('SUMMARY: 2/2 held'), twoRow.out.trim())
  {
    const twoRowResults = wlLines().filter(e => e.ev === 'verify-result' && (e.id === 'PPW-9510' || e.id === 'PPW-9511'))
    check('both rows of the multi-row run land their own held verify-result event',
      twoRowResults.length === 2 && twoRowResults.every(e => e.verdict === 'held'), JSON.stringify(wlLines()))
  }
  check('verify-fixes leaves the tree clean except the worklog after the two-row run',
    statusLines().length === 1 && statusLines()[0].endsWith(wlRel), g('status', '--porcelain').stdout)
  commitWorklog('worklog after two-row run')

  // A fix commit can itself touch worklog.jsonl, and revert/restore must not corrupt that history.
  writeFileSync(join(T, 'src', 'app2', 'c.txt'), 'buggyC\n')
  g('add', '.'); g('commit', '-qm', 'c base')
  writeFileSync(join(T, 'src', 'app2', 'c.txt'), 'fixedC\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit', 'CTests.cs'), 'test c\n')
  writeFileSync(wlPath, readFileSync(wlPath, 'utf8') + JSON.stringify({ t: '2020-01-01T00:00:00+00:00', ev: 'note', text: 'fixer note committed with the fix' }) + '\n')
  g('add', '.'); g('commit', '-qm', 'fix c (also touches worklog.jsonl)')
  const shaC = g('rev-parse', '--short', 'HEAD').stdout.trim()
  const worklogAtHead = wlLines()
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${shaC}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9520 | fixed | \`${shaC}\` | fixture: fix commit also touches worklog.jsonl |\n`)
  g('add', '.'); g('commit', '-qm', 'worklog-in-fix resolution')
  const worklogInFixTpl = `node -e "if(require('fs').readFileSync('src/app2/c.txt','utf8').includes('buggy')){console.log('Failed WorklogTests.Fixture');process.exit(1)}"`
  const wlFix = run('verify/verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', worklogInFixTpl])
  check('verify-fixes holds a row whose fix commit also touches worklog.jsonl', wlFix.code === 0 && wlFix.out.includes('"verdict":"held"'), wlFix.out.trim())
  const worklogAfterFix = wlLines()
  check("the fix commit's committed worklog history survives the revert/restore intact",
    worklogAfterFix.length === worklogAtHead.length + 1 &&
    worklogAtHead.every((e, i) => JSON.stringify(e) === JSON.stringify(worklogAfterFix[i])),
    JSON.stringify({ before: worklogAtHead, after: worklogAfterFix }))
  const lastLine = worklogAfterFix[worklogAfterFix.length - 1]
  check('the run appended its own verify-result on top of, not instead of, that history',
    lastLine?.id === 'PPW-9520' && lastLine?.verdict === 'held', JSON.stringify(worklogAfterFix))
  commitWorklog('worklog after worklog-in-fix run')

  // A frontend spec with no installed dependencies fails to start in both legs — a red for the wrong reason.
  mkdirSync(join(T, 'src', 'PhotoPrint.UI', 'src'), { recursive: true })
  writeFileSync(join(T, 'src', 'app', 'widget.txt'), 'buggy\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.UI', 'src', 'widget.spec.ts'), 'spec body\n')
  g('add', '.'); g('commit', '-qm', 'ui fix')
  const uiSha = g('rev-parse', '--short', 'HEAD').stdout.trim()
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${uiSha}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9503 | fixed | \`${uiSha}\` | fixture fix carrying a frontend spec |\n`)
  g('add', '.'); g('commit', '-qm', 'ui resolution')
  const ui = run('verify/verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen, '--no-events'])
  check('verify-fixes refuses a frontend row with no installed dependencies', ui.out.includes('"verdict":"env-missing"') && ui.out.includes('node_modules'), ui.out.trim())
  check('verify-fixes ran no test for the refused frontend row', ui.out.includes('"red_exits":[]'), ui.out.trim())
  rmSync(T, { recursive: true, force: true })
}
