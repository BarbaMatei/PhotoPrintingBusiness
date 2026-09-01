// Pins the shared vocabulary in records/schema.mjs: counts and spot values, so an accidental
// edit to a list every gate reads fails here instead of silently changing what a gate accepts.
// Behaviour that uses these values is pinned by each consumer's own test file.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only schema
import { check } from '../lib.mjs'
import {
  AREAS, CAPS, EVENTS, MANIFEST_LENSES, OPEN_STATUSES, SEVERITIES, SHA_RE, SHA_SCAN_RE,
  STATUSES, TARGETLESS, V3_CUTOFF, V4_CUTOFF,
} from '../../records/schema.mjs'
import * as schema from '../../records/schema.mjs'
import * as shim from '../../vocab.mjs'

{
  check('the lens manifest is the 11 keys', MANIFEST_LENSES.length === 11, String(MANIFEST_LENSES.length))
  check('the lens manifest opens with correctness and ends with frontend-ux',
    MANIFEST_LENSES[0] === 'correctness' && MANIFEST_LENSES[10] === 'frontend-ux', MANIFEST_LENSES.join(','))

  check('there are twelve areas', AREAS.length === 12, String(AREAS.length))
  check('the areas include payments and records', AREAS.includes('payments') && AREAS.includes('records'), AREAS.join(','))

  check('the severity set is the four emoji in order',
    SEVERITIES.join('') === '🔴🟠🟡⚪', SEVERITIES.join(''))

  check('the open statuses are open and in-progress',
    OPEN_STATUSES.join(',') === 'open,in-progress', OPEN_STATUSES.join(','))
  check('the status vocabulary is nine words', STATUSES.length === 9, String(STATUSES.length))
  check('the status vocabulary opens with the open ones and carries backlog',
    STATUSES[0] === 'open' && STATUSES[1] === 'in-progress' && STATUSES[8] === 'backlog', STATUSES.join(','))

  const events = Object.keys(EVENTS)
  check('the worklog vocabulary is 25 events', events.length === 25, String(events.length))
  check('round-start requires its round number',
    EVENTS['round-start'].required.join(',') === 'round', JSON.stringify(EVENTS['round-start']))
  check('gate-parked requires kind, default and reason',
    EVENTS['gate-parked'].required.join(',') === 'kind,default,reason', JSON.stringify(EVENTS['gate-parked']))
  check('void requires what it erases', EVENTS.void.required.join(',') === 'of', JSON.stringify(EVENTS.void))
  check('the ids rule covers protocol-written and check-dispatched only',
    events.filter(e => EVENTS[e].ids).join(',') === 'protocol-written,check-dispatched',
    events.filter(e => EVENTS[e].ids).join(','))
  check('every event declares a required-field list',
    events.every(e => Array.isArray(EVENTS[e].required)), events.filter(e => !Array.isArray(EVENTS[e].required)).join(','))

  check('nine folders under reviews/ are not targets', TARGETLESS.size === 9, String(TARGETLESS.size))
  check('lib, archive, state and system are not targets',
    ['lib', 'archive', 'state', 'system'].every(n => TARGETLESS.has(n)), [...TARGETLESS].join(','))
  check('a numbered target folder is not excluded', !TARGETLESS.has('901-good-target'), [...TARGETLESS].join(','))

  check('the doc-gate caps are the eight the contracts name', Object.keys(CAPS).length === 8, Object.keys(CAPS).join(','))
  check('the body caps are 120 review, 60 summary, 200 resolution',
    CAPS.reviewBodyLines === 120 && CAPS.summaryBodyLines === 60 && CAPS.resolutionBodyLines === 200,
    JSON.stringify(CAPS))
  check('the cell caps are 240 note chars, 15 decision lines, 20 ledger block lines',
    CAPS.resolutionNoteChars === 240 && CAPS.decisionLines === 15 && CAPS.ledgerBlockLines === 20,
    JSON.stringify(CAPS))
  check('the index caps are 50 description words and 5 glance lines',
    CAPS.passDescriptionWords === 50 && CAPS.glanceStateLines === 5, JSON.stringify(CAPS))

  check('SHA_RE matches a whole short sha and nothing longer than 40',
    SHA_RE.test('eeeee15') && SHA_RE.test('a'.repeat(40)) && !SHA_RE.test('a'.repeat(41)) && !SHA_RE.test('abc'),
    SHA_RE.source)
  check('SHA_RE is anchored, so it refuses a sha inside prose',
    !SHA_RE.test('commit eeeee15 here'), SHA_RE.source)
  check('SHA_SCAN_RE finds shas inside prose and is global',
    SHA_SCAN_RE.global && [...'at eeeee15 and ddddd17.'.matchAll(SHA_SCAN_RE)].length === 2, SHA_SCAN_RE.source)

  // The V2 cut-off went with the archive tolerance it gated: only lines no live target can hold
  // were ever read leniently, so a live record has one tier and the constant has no reader.
  check('the cut-offs are V3 2026-08-03 and V4 2026-08-28, with no V2 tier left',
    V3_CUTOFF === '2026-08-03' && V4_CUTOFF === '2026-08-28' && schema.V2_CUTOFF === undefined,
    [V3_CUTOFF, V4_CUTOFF, String(schema.V2_CUTOFF)].join(' '))

  check('the old vocab.mjs path still serves the same objects',
    shim.AREAS === AREAS && shim.MANIFEST_LENSES === MANIFEST_LENSES && shim.V4_CUTOFF === V4_CUTOFF,
    Object.keys(shim).join(','))
}
