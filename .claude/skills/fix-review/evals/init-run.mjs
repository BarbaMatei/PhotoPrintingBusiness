#!/usr/bin/env node
// Copies an eval fixture to evals/.run/<name> and makes it a git repository holding the whole
// fixture in one commit, so a fix round can commit into a tree of its own and a grader can diff
// any file against that base. The copy stays inside this repository because the records auditor
// resolves record shas against the enclosing one.
//
// Usage: node .claude/skills/fix-review/evals/init-run.mjs [<fixture name>]
// Prints the run root and the base commit. Exit 0 built · 1 the fixture or a git step failed.
import { cpSync, existsSync, mkdirSync, rmSync } from 'node:fs'
import { spawnSync } from 'node:child_process'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const HERE = dirname(fileURLToPath(import.meta.url))
const name = process.argv[2] ?? 'discount-module'
const from = join(HERE, 'fixtures', name)
const to = join(HERE, '.run', name)

if (!existsSync(from)) {
  console.error(`ERROR   no fixture at ${from}`)
  process.exit(1)
}

// An inherited GIT_* variable would point these commits at another repository's index.
const env = { ...process.env }
for (const k of Object.keys(env)) if (k.startsWith('GIT_')) delete env[k]

const git = (...args) => {
  const r = spawnSync('git', ['-C', to, ...args], { encoding: 'utf8', env })
  if (r.error || r.status !== 0) {
    console.error(`ERROR   git ${args.join(' ')} in ${to}: ${(r.stderr || r.stdout || r.error?.message || 'no output').trim()}`)
    process.exit(1)
  }
  return r.stdout.trim()
}

rmSync(to, { recursive: true, force: true })
mkdirSync(dirname(to), { recursive: true })
cpSync(from, to, { recursive: true })

git('init', '--quiet', '--initial-branch=fixture')
git('add', '-A')
// hooksPath is emptied rather than the commit skipping verification: these hooks are this repository's.
git('-c', 'user.name=eval fixture', '-c', 'user.email=eval@fixture.local', '-c', 'core.hooksPath=',
  'commit', '--quiet', '-m', `fixture base: ${name}`)

console.log(`RUN_ROOT: ${resolve(to)}`)
console.log(`BASE_COMMIT: ${git('rev-parse', 'HEAD')}`)
