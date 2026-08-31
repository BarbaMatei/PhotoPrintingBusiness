// Unit tests for the derived-fact modules, imported in-process (no child process): the one
// target-folder lookup and version listing, and the CLI root/--root handling. Their own file
// rather than records.test.mjs, because model/ answers questions ABOUT records (which folder,
// which version) where records/ only reads files — and the runner then reports the two layers
// separately. The consumers' test files still pin each command's behaviour end to end.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only model
import { check } from './lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'
import { pathToFileURL } from 'node:url'
import { newest, resolveDir, targetDirs, versions } from '../model/target.mjs'
import { repoRoot, takeRoot } from '../cli/args.mjs'
import { REPO, REVIEWS } from '../records/schema.mjs'

// ---------- model/target.mjs: the lookup ----------
{
  const T = mkdtempSync(join(tmpdir(), 'model-target-'))
  const reviews = join(T, 'reviews')
  const live = join(reviews, '980-live')
  const archived = join(reviews, 'archive', '981-archived')
  const both = join(reviews, '982-both')
  mkdirSync(live, { recursive: true })
  mkdirSync(archived, { recursive: true })
  mkdirSync(both, { recursive: true })
  mkdirSync(join(reviews, 'archive', '982-both'), { recursive: true })

  check('a live target resolves to reviews/<name>', resolveDir(reviews, '980-live') === live, String(resolveDir(reviews, '980-live')))
  check('an archived target resolves to reviews/archive/<name>',
    resolveDir(reviews, '981-archived') === archived, String(resolveDir(reviews, '981-archived')))
  check('live wins when a name exists in both places',
    resolveDir(reviews, '982-both') === both, String(resolveDir(reviews, '982-both')))
  check('a target that exists nowhere resolves to null, never to a path that is not there',
    resolveDir(reviews, '999-nowhere') === null, String(resolveDir(reviews, '999-nowhere')))
  check('targetDirs hands back both candidates in lookup order, whether or not they exist',
    JSON.stringify(targetDirs(reviews, '999-nowhere')) === JSON.stringify([join(reviews, '999-nowhere'), join(reviews, 'archive', '999-nowhere')]),
    JSON.stringify(targetDirs(reviews, '999-nowhere')))

  // ---------- model/target.mjs: versions and newest ----------
  for (const f of ['review-v1.md', 'review-v2.md', 'review-v10.md', 'resolution-v2.md', 'resolution-v11.md',
    'resolution-v3.md.bak', 'summary-v2.md', 'ledger.md']) writeFileSync(join(live, f), 'x\n')

  check('versions sorts numerically, so v10 comes after v2 and not between v1 and v2',
    JSON.stringify(versions(live, 'review')) === '[1,2,10]', JSON.stringify(versions(live, 'review')))
  check('versions of the other kind reads only that kind',
    JSON.stringify(versions(live, 'resolution')) === '[2,11]', JSON.stringify(versions(live, 'resolution')))
  check('a name that only looks like a record file is not a version',
    !versions(live, 'resolution').includes(3), JSON.stringify(versions(live, 'resolution')))
  check('newest is the highest version, read numerically',
    newest(live, 'review') === 10 && newest(live, 'resolution') === 11, `${newest(live, 'review')}/${newest(live, 'resolution')}`)
  check('a target with none of a kind has no versions and a newest of 0 — round numbers start at 1',
    versions(archived, 'review').length === 0 && newest(archived, 'review') === 0, String(newest(archived, 'review')))

  let threw = null
  try { versions(live, 'summary') } catch (e) { threw = e }
  check('an unknown record kind throws instead of quietly listing nothing',
    threw !== null && /resolution\|review/.test(threw.message), String(threw?.message))
  rmSync(T, { recursive: true, force: true })
}

// ---------- cli/args.mjs ----------
{
  const scriptUrl = pathToFileURL(join(REVIEWS, 'lib', 'some-command.mjs')).href
  check('with no override, a lib script reads the repo as two levels above itself',
    repoRoot(scriptUrl) === REPO, repoRoot(scriptUrl))
  check('an explicit --root wins over the derived root',
    repoRoot(scriptUrl, '/tmp/fixture-repo') === '/tmp/fixture-repo', repoRoot(scriptUrl, '/tmp/fixture-repo'))
  check('an empty --root value falls back to the derived root rather than resolving against the cwd',
    repoRoot(scriptUrl, '') === REPO, repoRoot(scriptUrl, ''))

  const a = takeRoot(['--root', '/r', '038-039', '--day', '2026-08-21'])
  check('takeRoot lifts out --root and its value and keeps everything else in order',
    a.root === '/r' && JSON.stringify(a.rest) === JSON.stringify(['038-039', '--day', '2026-08-21']), JSON.stringify(a))
  const b = takeRoot(['038-039'])
  check('no --root reads as no root, so the caller derives one', b.root === null && b.rest.length === 1, JSON.stringify(b))
  const c = takeRoot(['038-039', '--root'])
  check('a trailing --root with no value reads as no root, and swallows nothing',
    c.root === null && JSON.stringify(c.rest) === JSON.stringify(['038-039']), JSON.stringify(c))
}
