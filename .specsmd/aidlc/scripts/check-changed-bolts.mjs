#!/usr/bin/env node
import { spawnSync } from 'node:child_process'
import { join, dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const USAGE = `Usage: node .specsmd/aidlc/scripts/check-changed-bolts.mjs (--staged | --base <ref>) [--root <repo>] [--lint-strict]

The construction gate both the pre-commit hook and CI run. For every memory-bank/bolts/*/bolt.md
in the change set whose stages_completed grew, runs check-stage-exit.mjs for each new stage; for
every changed stage artifact (implementation-plan, implementation-walkthrough, test-walkthrough,
ddd-01/02/03) runs lint-claims.mjs. Files are read from the working tree.

  --staged       the change set is the index against HEAD (the pre-commit hook)
  --base <ref>   the change set is HEAD against the merge base with <ref> (CI on a pull request)
  --lint-strict  unsourced numbers in artifacts fail the lint instead of warning

Exit: 0 every check passed or nothing to check · 1 a check failed (their output is printed) · 2 usage.`

const HERE = dirname(fileURLToPath(import.meta.url))
const CHECK = join(HERE, 'check-stage-exit.mjs')
const LINT = join(HERE, 'lint-claims.mjs')
const BOLT_MD_RE = /^memory-bank\/bolts\/([^/]+)\/bolt\.md$/
const ARTIFACT_RE = /^memory-bank\/bolts\/[^/]+\/(?:implementation-plan|implementation-walkthrough|test-walkthrough|ddd-01-domain-model|ddd-02-technical-design|ddd-03-test-report)\.md$/

function usage(message) {
  console.error(`usage: ${message}\n\n${USAGE}`)
  process.exit(2)
}

function parseArgs(argv) {
  const args = { mode: null, ref: null, root: process.cwd(), strict: false }
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--help' || argv[i] === '-h') { console.log(USAGE); process.exit(0) }
    else if (argv[i] === '--staged') args.mode = 'staged'
    else if (argv[i] === '--base') { args.mode = 'base'; args.ref = argv[++i] }
    else if (argv[i] === '--root') args.root = resolve(argv[++i])
    else if (argv[i] === '--lint-strict') args.strict = true
    else usage(`unexpected argument ${argv[i]}`)
  }
  if (!args.mode || (args.mode === 'base' && !args.ref)) usage('wants --staged or --base <ref>')
  return args
}

function git(root, ...args) {
  const r = spawnSync('git', ['-C', root, ...args], { encoding: 'utf8' })
  return { ok: r.status === 0, out: r.stdout ?? '', err: r.stderr ?? '' }
}

function completedStages(text) {
  const block = /^stages_completed:[ \t]*\r?\n((?:[ \t]+.*(?:\r?\n|$))*)/m.exec(text)
  if (!block) return []
  return [...block[1].matchAll(/^[ \t]*-[ \t]*name:[ \t]*["']?([\w-]+)/gm)].map(m => m[1])
}

function changeSet(root, args) {
  if (args.mode === 'staged') {
    const diff = git(root, 'diff', '--cached', '--name-only', '--diff-filter=ACMR')
    if (!diff.ok) usage(`git diff --cached failed: ${diff.err.trim()}`)
    return { files: diff.out.split(/\r?\n/).filter(Boolean), oldRev: 'HEAD', newRev: '' }
  }
  const mb = git(root, 'merge-base', args.ref, 'HEAD')
  if (!mb.ok) usage(`git merge-base ${args.ref} HEAD failed: ${mb.err.trim()}`)
  const base = mb.out.trim()
  const diff = git(root, 'diff', '--name-only', '--diff-filter=ACMR', base, 'HEAD')
  if (!diff.ok) usage(`git diff failed: ${diff.err.trim()}`)
  return { files: diff.out.split(/\r?\n/).filter(Boolean), oldRev: base, newRev: 'HEAD' }
}

function show(root, rev, path) {
  const r = git(root, 'show', `${rev}:${path}`)
  return r.ok ? r.out : ''
}

function runNode(script, args) {
  const r = spawnSync(process.execPath, [script, ...args], { encoding: 'utf8' })
  process.stdout.write(`${r.stdout ?? ''}${r.stderr ?? ''}`)
  return r.status === 0
}

function main() {
  const args = parseArgs(process.argv.slice(2))
  const root = args.root
  const { files, oldRev, newRev } = changeSet(root, args)

  const stageChecks = []
  for (const file of files) {
    const m = BOLT_MD_RE.exec(file)
    if (!m) continue
    const before = completedStages(show(root, oldRev, file))
    const after = completedStages(show(root, newRev, file))
    for (const stage of after.filter(s => !before.includes(s))) stageChecks.push({ bolt: m[1], stage })
  }
  const artifacts = files.filter(f => ARTIFACT_RE.test(f))

  if (stageChecks.length === 0 && artifacts.length === 0) {
    console.log('construction gates: nothing to check (no stage completed, no stage artifact changed)')
    return
  }

  let failedChecks = 0, failedLints = 0
  for (const { bolt, stage } of stageChecks) {
    if (!runNode(CHECK, [bolt, stage, '--root', root])) failedChecks++
  }
  for (const artifact of artifacts) {
    if (!runNode(LINT, [join(root, artifact), ...(args.strict ? ['--strict'] : [])])) failedLints++
  }

  const plural = (n, word) => `${n} ${word}${n === 1 ? '' : 's'}`
  if (failedChecks === 0 && failedLints === 0) {
    console.log(`construction gates: ok (${plural(stageChecks.length, 'stage check')}, ${plural(artifacts.length, 'lint')})`)
    return
  }
  console.log(`construction gates: FAILED (${failedChecks} of ${plural(stageChecks.length, 'stage check')}, ${failedLints} of ${plural(artifacts.length, 'lint')})`)
  process.exit(1)
}

main()
