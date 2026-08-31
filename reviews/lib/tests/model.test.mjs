// Unit tests for the derived-fact modules, imported in-process (no child process): the one
// target-folder lookup and version listing, the two span-pairing strategies, and the CLI
// root/--root handling. Their own file rather than records.test.mjs, because model/ answers
// questions ABOUT records (which folder, which version, which span) where records/ only reads
// files — and the runner then reports the two layers separately. The consumers' test files still
// pin each command's behaviour end to end: render-records' 35 and speed-report's 82 are the
// contract these strategies must keep, and the shapes below are the seams they share.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only model
import { check } from './lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'
import { pathToFileURL } from 'node:url'
import { newest, resolveDir, targetDirs, versions } from '../model/target.mjs'
import { covers, lenientSpans, REFUSED, short, sliceSpans, spanOf, strictSpans } from '../model/spans.mjs'
import { convergenceCheck } from '../model/convergence.mjs'
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

// ---------- model/spans.mjs: the strict strategy (the renderer's) ----------
{
  const ev = (t, ev, round) => ({ t: `2019-05-01T${t}:00+03:00`, ev, round })
  // fail must not return, so the test's version throws and the assertion reads the message.
  const boom = m => { throw new Error(m) }
  const opts = (round, extra = {}) => ({
    startEv: 'round-start', endEv: 'round-end', belongs: e => e.round === round, fail: boom,
    onSecondStart: (o, e) => `second start: ${o.t} then ${e.t}`,
    onForeign: (o, e) => `foreign: open ${o.t}, ${e.ev} ${e.round} at ${e.t}`,
    onStrayEnd: (e, done) => `stray end at ${e.t} after ${done.length} span(s)`,
    ...extra,
  })
  const caught = fn => { try { fn(); return null } catch (e) { return e.message } }

  const two = [ev('09:00', 'round-start', 1), ev('09:30', 'note', 1), ev('10:00', 'round-end', 1),
    ev('11:00', 'round-start', 1), ev('11:30', 'round-end', 1)]
  const paired = strictSpans(two, opts(1))
  check('a resumed round pairs into one span per start/end couple, as index ranges',
    JSON.stringify(paired.spans) === '[{"from":0,"to":2},{"from":3,"to":4}]', JSON.stringify(paired.spans))
  check('a fully paired walk leaves nothing open', paired.open === null, JSON.stringify(paired.open))

  const other = strictSpans(two, opts(2))
  check("another unit's stamps pair no spans and raise nothing while nothing is open",
    other.spans.length === 0 && other.open === null, JSON.stringify(other))

  const open = strictSpans([ev('09:00', 'round-start', 1), ev('09:30', 'note', 1)], opts(1))
  check('a start with no end comes back as the open span, with its index',
    open.spans.length === 0 && open.open.i === 0, JSON.stringify(open.open?.i))

  check('a second start while one is open aborts, naming both stamps',
    caught(() => strictSpans([ev('09:00', 'round-start', 1), ev('10:00', 'round-start', 1)], opts(1)))
      === 'second start: 2019-05-01T09:00:00+03:00 then 2019-05-01T10:00:00+03:00',
    String(caught(() => strictSpans([ev('09:00', 'round-start', 1), ev('10:00', 'round-start', 1)], opts(1)))))
  check("another unit's stamp arriving while a start is open aborts as a missing end",
    caught(() => strictSpans([ev('09:00', 'round-start', 1), ev('10:00', 'round-start', 2)], opts(1)))
      === 'foreign: open 2019-05-01T09:00:00+03:00, round-start 2 at 2019-05-01T10:00:00+03:00',
    String(caught(() => strictSpans([ev('09:00', 'round-start', 1), ev('10:00', 'round-start', 2)], opts(1)))))
  check('an end that closes nothing aborts, and is told how many spans already closed',
    caught(() => strictSpans([ev('09:00', 'round-start', 1), ev('09:30', 'round-end', 1), ev('10:00', 'round-end', 1)], opts(1)))
      === 'stray end at 2019-05-01T10:00:00+03:00 after 1 span(s)',
    String(caught(() => strictSpans([ev('09:00', 'round-start', 1), ev('09:30', 'round-end', 1), ev('10:00', 'round-end', 1)], opts(1)))))

  const sliced = sliceSpans(two, paired.spans, { fail: boom })
  check('sliceSpans hands back one sequence per span plus the flat list',
    sliced.seqs.length === 2 && sliced.seqs[0].length === 3 && sliced.flat.length === 5, JSON.stringify(sliced.seqs.map(s => s.length)))
  check('a stamp whose timestamp will not parse stops the measurement',
    /unparseable timestamp/.test(String(caught(() => sliceSpans([{ t: 'the other day', ev: 'round-start', round: 1 }], [{ from: 0, to: 0 }], { fail: boom })))),
    String(caught(() => sliceSpans([{ t: 'the other day', ev: 'round-start', round: 1 }], [{ from: 0, to: 0 }], { fail: boom }))))
}

// ---------- model/spans.mjs: the lenient strategy (the speed report's) ----------
{
  const ev = (t, ev, round) => ({ t: `2019-06-01T${t}:00+03:00`, ev, round })
  const walk = (events, resume) => {
    const notes = []
    const spans = lenientSpans(events, { startEv: 'round-start', endEv: 'round-end', key: 'round', resume, note: m => notes.push(m) })
    return { spans, notes }
  }

  const restamp = walk([ev('09:00', 'round-start', 5), ev('09:00', 'round-start', 6), ev('10:00', 'round-end', 6)])
  check('two starts on one instant are one restamp: the later key wins and the earlier costs no time',
    JSON.stringify(restamp.spans) === '[{"key":6,"from":1,"to":2}]', JSON.stringify(restamp.spans))
  check('the restamp is reported', /restamps the round 5 opened at the same instant/.test(restamp.notes.join('\n')), JSON.stringify(restamp.notes))

  const dup = walk([ev('09:00', 'round-start', 1), ev('09:30', 'round-start', 2), ev('10:00', 'round-end', 2)])
  check('a later second start is a duplicate: the first span stands and takes the end',
    JSON.stringify(dup.spans) === '[{"key":1,"from":0,"to":2}]', JSON.stringify(dup.spans))
  check('the ignored duplicate and the mislabelled end are both reported',
    /the duplicate is ignored/.test(dup.notes.join('\n')) && /carries round 2 but closes the span opened at/.test(dup.notes.join('\n')),
    JSON.stringify(dup.notes))

  const stray = walk([ev('09:00', 'round-end', 1)])
  check('an end that closes nothing is dropped, never aborted', stray.spans.length === 0, JSON.stringify(stray.spans))
  check('the dropped end is reported', /closes nothing and no resumption stamp precedes it — ignored/.test(stray.notes.join('\n')), JSON.stringify(stray.notes))

  const resumed = walk([ev('09:00', 'triage-done', 1), ev('10:00', 'round-end', 1)], () => 0)
  check('a resumption opens the span where the caller\'s rule points',
    JSON.stringify(resumed.spans) === '[{"key":1,"from":0,"to":1}]', JSON.stringify(resumed.spans))
  check('the resumption is reported with the stamp it opened at',
    /closes a resumed round 1 whose round-start went unstamped — opened at 2019-06-01T09:00:00\+03:00/.test(resumed.notes.join('\n')),
    JSON.stringify(resumed.notes))
  const refused = walk([ev('09:00', 'triage-done', 1), ev('10:00', 'round-end', 1)], () => REFUSED)
  check('a refused resumption charges nothing and says nothing here — the rule reports it itself',
    refused.spans.length === 0 && refused.notes.length === 0, JSON.stringify(refused))

  const unclosed = walk([ev('09:00', 'round-start', 1), ev('09:30', 'note', 1), ev('11:00', 'note', 1)])
  check('a start with no end is measured to the last event',
    JSON.stringify(unclosed.spans) === '[{"key":1,"from":0,"to":2}]', JSON.stringify(unclosed.spans))
  check('measuring to the last event is reported', /has no round-end — measured to the last event/.test(unclosed.notes.join('\n')), JSON.stringify(unclosed.notes))

  const spans = [{ key: 1, from: 2, to: 5 }]
  check('covers is about the gap starting at an index, so the span\'s own end index is not covered',
    covers(spans, 2) && covers(spans, 4) && !covers(spans, 5) && !covers(spans, 1), 'covers')
  check('spanOf is about the event at an index, so the end index belongs to its span',
    spanOf(spans, 5) === spans[0] && spanOf(spans, 6) === undefined, 'spanOf')
  check('a gate reason longer than 60 chars is quoted by its head only',
    short('x'.repeat(61)) === `${'x'.repeat(57)}…` && short('short one') === 'short one', short('x'.repeat(61)))
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

// ---------- model/convergence.mjs: the brake's edges ----------
// Every fix-round answer in both drivers consults the brake, so its decision edges are asserted
// here directly rather than only through whichever fixture happens to exercise one of them.
{
  const fix = (round, fixed, notes = '') => ({ type: 'fix-round', round, findings: { fixed }, tests: { invocations: 1 }, notes })
  const blind = findings => ({ type: 'delta-discovery', lenses: ['correctness'], findings })
  const seed = (round, area) => ({ f: 'F1', d: 'PPW-1', new: true, sev: 'high', seed_round: round, ...(area ? { area } : {}) })

  check('one substantive round is not two, so the brake has nothing to say',
    convergenceCheck([fix(1, 3), blind([seed(1, 'payments')])]) === null, 'expected null')
  check('a round that fixed nothing is not substantive, so it cannot be one of the two',
    convergenceCheck([{ type: 'fix-round', round: 1, findings: { fixed: 0 }, tests: { invocations: 4 } }, fix(2, 1)]) === null, 'expected null')

  // A seeded finding with no area word cannot be named in a design pass, so it never brakes one.
  const unstated = convergenceCheck([fix(1, 1), blind([seed(1)]), fix(2, 1), blind([seed(2)])])
  check('a seed with no area never brakes, however high the rate',
    unstated.nonConvergent === false && unstated.area === null && unstated.s1 === 1 && unstated.s2 === 1, JSON.stringify(unstated))

  const capped = convergenceCheck([fix(1, 1), blind([seed(1, 'payments')]), fix(2, 1, 'design-pass:payments'), blind([seed(2, 'payments')])])
  check('the component stays non-convergent once its one design pass has run — the cap only lifts the gate',
    capped.nonConvergent === true && capped.area === 'payments' && capped.capped === true, JSON.stringify(capped))
  const otherArea = convergenceCheck([fix(1, 1), blind([seed(1, 'payments')]), fix(2, 1, 'design-pass:uploads'), blind([seed(2, 'payments')])])
  check('a design pass recorded for another component does not cap this one',
    otherArea.nonConvergent === true && otherArea.capped === false, JSON.stringify(otherArea))

  // The shrink warning and the rate gate are separate readings of the same pair.
  const growing = convergenceCheck([fix(1, 2), fix(2, 3)])
  check('rounds that are not shrinking are reported even when nothing seeded — s = 0 on both',
    growing.notShrinking === true && growing.nonConvergent === false && growing.s1 === 0 && growing.s2 === 0, JSON.stringify(growing))
  const shrinking = convergenceCheck([fix(1, 3), blind([seed(1, 'payments')]), fix(2, 2), blind([seed(2, 'payments')])])
  check('a shrinking pair still brakes when both rates clear the threshold',
    shrinking.notShrinking === false && shrinking.nonConvergent === true, JSON.stringify(shrinking))
}
