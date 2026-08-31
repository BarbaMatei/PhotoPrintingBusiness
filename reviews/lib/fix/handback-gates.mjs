// The hand-back gates of the accepted fix-round audit (2026-08-28): a resolution may not stand
// `resolved` without the round's recorded evidence — a consumed pre-check verdict or a dispatched
// check for every trigger-classified fix (R2), a round-scope composition review for every code
// round (R3), a test-meaning audit whenever new tests ran red (R4), and a protocol block written
// before the fixes of any cluster whose fix briefs overlap on the same files (R1). Applies only to
// rounds closed on/after V4_CUTOFF; history is grandfathered.
//
// These enforce the fixer's contract, not the shape of a record, which is why they live under
// fix/ and the records validator calls them through the auditor rather than owning them. The
// events arrive already void-filtered: a mis-stamp repaired with a void must not gate hand-back.
import { readFileSync, readdirSync, existsSync } from 'node:fs'
import { join } from 'node:path'
import { V4_CUTOFF } from '../records/schema.mjs'
import { blocks as ledgerBlocks, severityOf } from '../records/ledger.mjs'
import { meta, fixedRows } from '../records/resolution.mjs'

export function auditHandBackGates(t, tag, roundDates, events, strictTier) {
  for (const f of readdirSync(t.dir)) {
    const m = /^resolution-v(\d+)\.md$/.exec(f)
    if (!m) continue
    const round = Number(m[1])
    const text = readFileSync(join(t.dir, f), 'utf8')
    const { status, closed } = meta(text, { acrossLines: true })
    if (status !== 'resolved') continue
    const lineDate = roundDates.get(round) ?? null
    if (!((closed && closed >= V4_CUTOFF) || (lineDate && lineDate >= V4_CUTOFF))) continue
    const at = `${tag} resolution-v${round}.md`
    if (!closed) strictTier(`${at}: resolved without a closed: date — required since 2026-08-28; the hand-back gates key on it`)

    const startIdx = events.findIndex(e => e.ev === 'round-start' && e.round === round)
    if (startIdx === -1) { strictTier(`${at}: no round-start worklog event for round ${round} — the hand-back gates have nothing to check against`); continue }
    let endIdx = -1
    for (let i = events.length - 1; i >= 0; i--) if (events[i].ev === 'round-end' && events[i].round === round) { endIdx = i; break }
    const slice = events.slice(startIdx, endIdx === -1 ? undefined : endIdx + 1)
    const by = ev => slice.filter(e => e.ev === ev)
    const idsOf = e => Array.isArray(e.ids) ? e.ids : []

    const fixed = fixedRows(text).map(r => ({ id: r.id, code: r.commits.length > 0 }))
    if (fixed.some(r => r.code) && (!by('round-review-dispatched').length || !by('round-review-returned').length))
      strictTier(`${at}: code was fixed but the round has no round-review-dispatched/returned pair — one composition review over the whole round's diff gates hand-back (audit R3)`)
    if (slice.some(e => e.ev === 'test-run' && e.kind === 'red') && !by('test-audit-returned').length)
      strictTier(`${at}: regression tests ran red but no test-audit-returned event exists — the test-meaning check gates hand-back (audit R4)`)

    const ledgerPath = join(t.dir, 'ledger.md')
    const ledgerText = existsSync(ledgerPath) ? readFileSync(ledgerPath, 'utf8') : ''
    const blocks = ledgerBlocks(ledgerText)
    const sevOf = severityOf(ledgerText)

    const dispatched = by('check-dispatched')
    for (const row of fixed) {
      const block = blocks.get(row.id)
      if (!block || !/Trigger-list-shaped:\*{0,2}\s*yes/i.test(block)) continue
      const preChecked = /Approach pre-check:\s*(cleared|revised)/i.test(block)
      const named = dispatched.some(e => idsOf(e).includes(row.id))
      if (!preChecked && !named)
        strictTier(`${at}: ${row.id} is trigger-classified by its fix brief but no pre-check verdict was consumed and no check-dispatched event names it — "not needed" is not a writable value (audit R2)`)
    }

    const briefFiles = id => {
      const set = new Set()
      for (const hit of (blocks.get(id) ?? '').matchAll(/[A-Za-z0-9_./\\-]+\.(?:cs|ts|tsx|mjs|js|html|scss|css)\b/g)) {
        const p = hit[0].replace(/\\/g, '/')
        if (/\.spec\.ts$/.test(p) || /PhotoPrint\.Tests/.test(p) || /Tests\.cs$/.test(p)) continue
        set.add(p.split('/').pop().toLowerCase())
      }
      return set
    }
    const serious = fixed.filter(r => r.code && ['🔴', '🟠'].includes(sevOf.get(r.id)))
    const groups = []
    for (const r of serious) {
      const mine = briefFiles(r.id)
      if (!mine.size) continue
      let g = groups.find(gr => [...gr.files].some(fb => mine.has(fb)))
      if (!g) { g = { ids: [], files: new Set() }; groups.push(g) }
      g.ids.push(r.id)
      for (const fb of mine) g.files.add(fb)
    }
    const findingEvents = by('finding')
    const protoEvents = by('protocol-written')
    for (const g of groups.filter(x => x.ids.length >= 2)) {
      const proto = protoEvents.find(e => idsOf(e).some(id => g.ids.includes(id)))
      const firstFix = findingEvents.find(e => g.ids.includes(e.id))
      if (!proto)
        strictTier(`${at}: ${g.ids.join(', ')} share a stateful surface (fix briefs overlap on ${[...g.files][0]}) but no protocol-written event covers them — the protocol block is the cluster's first artifact (audit R1)`)
      else if (firstFix && Date.parse(proto.t) > Date.parse(firstFix.t))
        strictTier(`${at}: the protocol-written event for ${g.ids.join(', ')} is timestamped after the cluster's first finding event — a protocol paraphrasing the diff is spec-theatre (audit R1)`)
    }
  }
}
