// Two derived facts about a target's open work that the router and the policy must answer the
// same way, and used to answer twice: whether the ledger may be read for open work at all
// (standsDown), and whether the loop is sitting at its close (atLoopClose).
//
// standsDown — a fix round and its verification are one reviewed unit whose records render once,
// after the verification. While a resolved round waits for that render, the ledger rows still
// read `open` and describe work the round already answered, so no reader may arm on them. The
// mechanical test is the newest resolution reading `resolved` with no fix-round line for its
// round — except for a round closed before V3_CUTOFF, when fix-round lines did not exist yet and
// its records will never come, so the window would be permanent.
//
// atLoopClose — the certification passed on the latest pass and no post-cert fix round is
// pending. Open 🟠 stand down there: they roll into the backlog at close and must not pre-empt
// the owner's close decision.
import { readFileSync } from 'node:fs'
import { join } from 'node:path'
import { dateValue, parse, value } from '../records/frontmatter.mjs'
import { readMetrics } from '../records/metrics.mjs'
import { V3_CUTOFF } from '../records/schema.mjs'
import { newest } from './target.mjs'

export function standsDown(dir) {
  const RN = newest(dir, 'resolution')
  if (!RN || RN < newest(dir, 'review')) return false
  const fm = parse(readFileSync(join(dir, `resolution-v${RN}.md`), 'utf8')).fm ?? ''
  if (value(fm, 'status') !== 'resolved') return false
  const closed = dateValue(fm, 'closed', { acrossLines: true })
  if (closed && closed < V3_CUTOFF) return false
  const metrics = readMetrics(dir)
  if (!metrics) return true
  return !metrics.lines.some(l => l.type === 'fix-round' && l.round === RN)
}

// `line` is the newest metrics line; N the newest review version, RN the newest resolution's.
export const atLoopClose = (line, N, RN) => line.outcome === 'certified' && line.pass === N && (!RN || RN < N)
