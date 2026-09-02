// Span pairing: turning a worklog's start/end stamps into the index ranges every measurement is
// taken over — round-start/round-end for a fix round, pass-launch/pass-records-done for a pass,
// gate-open/gate-closed for an owner gate. Spans are index ranges, never timestamps, so two
// stamps on one instant stay apart. Reads nothing: the caller hands in the events.
//
// Two readers need the same walk with opposite answers to a broken log, and both answers are
// deliberate, so they are named strategies over one core rather than one algorithm with a flag
// that means two things:
//   strictSpans  — the renderer's. A stamp it cannot pair aborts the render, because a metrics
//                  line is written once and an over-counted runtime cannot be taken back. It
//                  reads one unit at a time (`belongs`), and another unit's stamp arriving while
//                  this unit's start is open is a missing end.
//   lenientSpans — the speed report's. It reads every unit in one walk (the span carries its own
//                  `key`), never aborts, and reports what it did with each odd stamp as a NOTE:
//                  a second start on the same instant is a restamp and the later one wins, a
//                  later one is a duplicate and is ignored, and an end that closes nothing is
//                  offered to the caller's `resume` rule before it is dropped.
// The shared core is the part that is genuinely one rule: the order events are read in, that only
// the two named events matter, that at most one span is open, and that a span is [from, to].
const ts = e => Date.parse(e.t)

// The stamp formatting the NOTEs and the resume rule both quote, kept with the walk that emits it.
export const at = (events, i) => events[i].t
export const stamp = (events, ev, i) => `${ev} at ${at(events, i)}`
// A gate span is keyed by its reason, which is a sentence — a NOTE quotes only its head.
export const short = v => { const t = String(v); return t.length > 60 ? `${t.slice(0, 57)}…` : t }

// What a lenient resume rule returns to say "this end is a stray stamp, charge nothing".
export const REFUSED = Symbol('refused')

// The core: read the events in order, hand every start/end stamp to the strategy, and carry the
// one open span it hands back. The strategy owns the shape of `open` and pushes its own spans.
function walkPairs(events, startEv, endEv, onStart, onEnd) {
  const spans = []
  let open = null
  for (let i = 0; i < events.length; i++) {
    const e = events[i]
    if (e.ev !== startEv && e.ev !== endEv) continue
    open = e.ev === startEv ? onStart(e, i, open, spans) : onEnd(e, i, open, spans)
  }
  return { spans, open }
}

// Fail-loud pairing for one unit. `fail` must not return (it exits); the message builders are the
// caller's, because they carry the repair command for the stamp that went wrong.
export function strictSpans(events, { startEv, endEv, belongs, fail, onSecondStart, onForeign, onStrayEnd }) {
  // An unclosed start followed by another unit's stamp is a missing end, not a live unit.
  const foreign = (e, open) => {
    if (belongs(e)) return false
    if (open) fail(onForeign(open.e, e))
    return true
  }
  return walkPairs(events, startEv, endEv,
    (e, i, open) => {
      if (foreign(e, open)) return open
      if (open) fail(onSecondStart(open.e, e))
      return { e, i }
    },
    (e, i, open, spans) => {
      if (foreign(e, open)) return open
      if (!open) fail(onStrayEnd(e, spans))
      spans.push({ from: open.i, to: i })
      return null
    })
}

// The events of each span and all of them flat, refusing a stamp whose timestamp will not parse.
export function sliceSpans(events, spans, { fail }) {
  const seqs = spans.map(s => events.slice(s.from, s.to + 1))
  const flat = seqs.flat()
  for (const e of flat) if (!Number.isFinite(ts(e))) fail(`worklog event with unparseable timestamp: ${JSON.stringify(e)}`)
  return { seqs, flat }
}

// Never-aborting pairing across every unit at once: each span carries the `key` field's value, and
// every odd stamp is reported through `note`. `resume` (optional) is asked what an end that closes
// nothing opens — an index, null for "nothing precedes it", or REFUSED to drop it silently.
export function lenientSpans(events, { startEv, endEv, key, resume, note }) {
  const { spans, open } = walkPairs(events, startEv, endEv,
    (e, i, open) => {
      // Two starts on the same instant are one stamp restamped: the later one is the correction.
      if (open && ts(e) === ts(events[open.from])) {
        note(`${stamp(events, startEv, i)} (${key} ${short(e[key])}) restamps the ${key} ${short(open.key)} opened at the same instant — the span reads as ${key} ${short(e[key])}`)
        return { key: e[key], from: i }
      }
      if (open) {
        note(`${stamp(events, startEv, i)} (${key} ${short(e[key])}) opens while ${key} ${short(open.key)} from ${at(events, open.from)} is still open — the duplicate is ignored`)
        return open
      }
      return { key: e[key], from: i }
    },
    (e, i, open, spans) => {
      if (open) {
        if (open.key !== e[key] && e[key] != null) note(`${stamp(events, endEv, i)} carries ${key} ${short(e[key])} but closes the span opened at ${at(events, open.from)} with ${key} ${short(open.key)} — read it as ${short(open.key)}'s end`)
        spans.push({ key: open.key, from: open.from, to: i })
        return null
      }
      const from = resume ? resume(e, i, spans) : null
      if (from === REFUSED) return null
      if (from == null) note(`${stamp(events, endEv, i)} (${key} ${short(e[key])}) closes nothing and no resumption stamp precedes it — ignored`)
      else {
        spans.push({ key: e[key], from, to: i })
        note(`${stamp(events, endEv, i)} closes a resumed ${key} ${e[key]} whose ${startEv} went unstamped — opened at ${at(events, from)}`)
      }
      return null
    })
  if (open) {
    spans.push({ key: open.key, from: open.from, to: events.length - 1 })
    note(`${stamp(events, startEv, open.from)} has no ${endEv} — measured to the last event`)
  }
  return spans
}

// Which span an index sits in: `covers` is about the gap that STARTS at i (so the span's own end
// index is not covered), `spanOf` about the event at i.
export const covers = (spans, i) => spans.some(s => i >= s.from && i < s.to)
export const spanOf = (spans, i) => spans.find(s => i >= s.from && i <= s.to)
