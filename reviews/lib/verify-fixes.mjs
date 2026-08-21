#!/usr/bin/env node
// Mechanical half of the verification runbook: for every `fixed` row of the latest
// resolution, revert the fix commit's source files (tests kept), prove the regression tests
// go red, restore with `git reset --hard HEAD`, prove green. One test process at a time,
// strictly sequential — the machine rule. Judgment items (wont-fix/deferred/disputed
// affirmations) stay with the verifier: one cheap diff each, not worth scripting.
// Test commands are injectable so the fixture suite can drive a throwaway repo.
//
// Usage: node reviews/lib/verify-fixes.mjs [--root <repoRoot>] <target>
//          [--only PPW-1,PPW-2] [--dry-run]
//          [--test-cmd-api "<tpl with {filter}>"] [--test-cmd-ui "<tpl with {name}>"]
// Output: one JSON line per row {id, verdict, commit, filters, red_exits, green_exits},
// then "SUMMARY: <held>/<total> held". Verdicts: held · test-never-red · no-test ·
// test-only · unreachable-commit · unparsable-commit · revert-failed · green-failed ·
// rename-in-fix · env-missing · dry-run. A row's Commit cell may list several commits; all of
// them are reverted together, back to the parent of the first. A row whose tests include a
// frontend spec needs src/PhotoPrint.UI/node_modules in this checkout, or it is not run at all.
// Exit: 0 all held · 1 any other verdict · 2 dirty tree or usage error.
import { readFileSync, readdirSync, existsSync, rmSync } from 'node:fs'
import { spawnSync } from 'node:child_process'
import { join, dirname, basename } from 'node:path'
import { fileURLToPath } from 'node:url'

const argv = process.argv.slice(2)
let root = null, only = null, dryRun = false
let tplApi = 'dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~{filter}"'
let tplUi = 'npm --prefix src/PhotoPrint.UI test -- --watch=false --include=**/{name}*.spec.ts'
const rest = []
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === '--root') root = argv[++i]
  else if (argv[i] === '--only') only = new Set(argv[++i].split(','))
  else if (argv[i] === '--dry-run') dryRun = true
  else if (argv[i] === '--test-cmd-api') tplApi = argv[++i]
  else if (argv[i] === '--test-cmd-ui') tplUi = argv[++i]
  else rest.push(argv[i])
}
const target = rest[0]
const REPO = root ?? join(dirname(fileURLToPath(import.meta.url)), '..', '..')
if (!target) { console.error('usage: node reviews/lib/verify-fixes.mjs [--root <repoRoot>] <target> [--only ids] [--dry-run] [--test-cmd-api tpl] [--test-cmd-ui tpl]'); process.exit(2) }
const dir = [join(REPO, 'reviews', target), join(REPO, 'reviews', 'archive', target)].find(existsSync)
if (!dir) { console.error(`no reviews folder for "${target}"`); process.exit(2) }

const git = (...a) => spawnSync('git', a, { cwd: REPO, encoding: 'utf8' })
const runCmd = c => spawnSync(c, { cwd: REPO, encoding: 'utf8', shell: true, timeout: 600000 })

const versions = readdirSync(dir).map(f => /^resolution-v(\d+)\.md$/.exec(f)).filter(Boolean).map(m => Number(m[1]))
if (!versions.length) { console.error(`no resolution file in ${dir}`); process.exit(2) }
const N = Math.max(...versions)
const resolutionText = readFileSync(join(dir, `resolution-v${N}.md`), 'utf8')
// A fix may span several commits (the micro-review follow-up is the common case), so the Commit
// cell can list more than one. Take every sha in the cell and never skip a `fixed` row silently:
// a row whose cell holds no sha is reported, because under-coverage in a verifier reads as a pass.
const rows = [...resolutionText.matchAll(/^\|\s*(PPW-\d+)\s*\|\s*fixed\s*\|([^|]*)\|/gm)]
  .map(m => ({ id: m[1], commits: m[2].match(/[0-9a-f]{7,40}/g) ?? [] }))
  .map(r => ({ ...r, commit: r.commits.join(', ') }))
  .filter(r => !only || only.has(r.id))
if (!rows.length) { console.error(`resolution-v${N}.md has no matching fixed rows`); process.exit(2) }

const resolutionFm = /^---\r?\n([\s\S]*?)\r?\n---/.exec(resolutionText)?.[1] ?? ''
const fixedCommit = /^fixed_commit:\s*(.+?)\s*$/m.exec(resolutionFm)?.[1]
if (fixedCommit) {
  const fc = git('rev-parse', fixedCommit)
  const head = git('rev-parse', 'HEAD')
  if (fc.status === 0 && head.status === 0 && fc.stdout.trim() !== head.stdout.trim()) {
    console.error(`warning: HEAD is not the resolution's fixed_commit ${fixedCommit} — reverts run against current HEAD; red-run attribution may be weaker`)
  }
}

if (git('status', '--porcelain').stdout.trim() !== '') {
  console.error('the tree is dirty — verification reverts files and restores with reset --hard; commit or stash first')
  process.exit(2)
}

const isTest = p => /^src\/PhotoPrint\.Tests\//.test(p) || /\.spec\.ts$/.test(p)
const restoreOrDie = id => {
  const r = git('reset', '--hard', 'HEAD')
  if (r.status !== 0 || git('status', '--porcelain').stdout.trim() !== '') {
    console.error(`FATAL: restore failed after reverting ${id} — check the tree by hand`)
    process.exit(2)
  }
}
const results = []
for (const row of rows) {
  const res = { id: row.id, verdict: null, commit: row.commit, filters: [], red_exits: [], green_exits: [] }
  results.push(res)
  if (!row.commits.length) { res.verdict = 'unparsable-commit'; continue }
  if (row.commits.some(c => git('cat-file', '-e', c).status !== 0 ||
      git('merge-base', '--is-ancestor', c, 'HEAD').status !== 0)) { res.verdict = 'unreachable-commit'; continue }
  const entries = row.commits.flatMap(c => git('show', '--name-status', '--format=', c).stdout
    .split('\n').filter(l => l.trim()).map(l => l.split('\t')))
  if (entries.some(e => e[0].startsWith('R'))) { res.verdict = 'rename-in-fix'; continue }
  const base = `${row.commits[0]}^`
  const uniq = new Map()
  for (const e of entries) uniq.set(e[1], e[0])
  const tests = [...uniq.keys()].filter(isTest)
  const source = [...uniq.entries()].filter(([f]) => !isTest(f)).map(([f, st]) => [st, f])
  if (!tests.length) { res.verdict = 'no-test'; continue }
  if (!source.length) { res.verdict = 'test-only'; continue }
  // A frontend suite cannot run without installed dependencies, and a runner that fails to start
  // exits non-zero in BOTH legs — reading as a red that reddened for the wrong reason. Refuse.
  if (tests.some(p => p.endsWith('.spec.ts')) && !existsSync(join(REPO, 'src', 'PhotoPrint.UI', 'node_modules'))) {
    res.verdict = 'env-missing'
    res.note = 'src/PhotoPrint.UI/node_modules is absent in this checkout, so the frontend suites cannot run'
    continue
  }
  const cmds = tests.map(p => p.endsWith('.cs')
    ? tplApi.replace('{filter}', 'PhotoPrint.Tests.' + p.replace(/^src\/PhotoPrint\.Tests\//, '').replace(/\.cs$/, '').split('/').join('.'))
    : tplUi.replace('{name}', basename(p).replace(/\.spec\.ts$/, '')))
  res.filters = cmds
  if (dryRun) { res.verdict = 'dry-run'; res.plan = { source: source.map(e => e[1]), tests }; continue }

  let reverted = true
  for (const [, f] of source) {
    const existedBefore = git('cat-file', '-e', `${base}:${f}`).status === 0
    if (!existedBefore) { try { rmSync(join(REPO, f)) } catch { reverted = false } }
    else if (git('checkout', base, '--', f).status !== 0) reverted = false
  }
  if (!reverted) {
    restoreOrDie(row.id)
    res.verdict = 'revert-failed'
    continue
  }
  for (const c of cmds) res.red_exits.push(runCmd(c).status ?? -1)
  restoreOrDie(row.id)
  if (!res.red_exits.some(x => x !== 0)) { res.verdict = 'test-never-red'; continue }
  for (const c of cmds) res.green_exits.push(runCmd(c).status ?? -1)
  res.verdict = res.green_exits.every(x => x === 0) ? 'held' : 'green-failed'
}

for (const r of results) console.log(JSON.stringify(r))
const held = results.filter(r => r.verdict === 'held' || r.verdict === 'dry-run').length
console.log(`SUMMARY: ${held}/${results.length} held`)
process.exit(held === results.length ? 0 : 1)
