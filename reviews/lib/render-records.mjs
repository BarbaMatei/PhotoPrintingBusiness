#!/usr/bin/env node
// Records renderer — turns a fix round's worklog + resolution frontmatter into the mechanical
// records, per metrics-schema.md v3: the resolution body's findings table (between
// <!-- rendered:findings-table:start/end --> markers), the round's fix-round metrics line
// (runtime split computed from worklog events), and a suggested index.md status cell (printed,
// never applied — index prose stays a human's). Judgment prose is never touched.
//
// Usage: node reviews/lib/render-records.mjs <target> [--round <n>] [--dry-run] [--root <repoRoot>]
// Exit 0 = rendered (or dry-run) · 1 = cannot render (missing records, duplicate line, bad worklog).
import { readFileSync, writeFileSync, appendFileSync, readdirSync, existsSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const argv = process.argv.slice(2)
let ROOT = null, ROUND = null, DRY = false
const rest = []
for (let i = 0; i < argv.length; i++) {
  if (argv[i] === '--root') ROOT = argv[++i]
  else if (argv[i] === '--round') ROUND = Number(argv[++i])
  else if (argv[i] === '--dry-run') DRY = true
  else rest.push(argv[i])
}
if (!ROOT) ROOT = join(dirname(fileURLToPath(import.meta.url)), '..', '..')
const target = rest[0]
if (!target) fail('usage: render-records.mjs <target> [--round <n>] [--dry-run]')
const dir = join(ROOT, 'reviews', target)
if (!existsSync(dir)) fail(`no reviews/${target}/`)

function fail(m) { console.error(`ERROR   ${m}`); process.exit(1) }
const note = m => console.log(`note    ${m}`)

// ---------- locate the round ----------
const resVersions = readdirSync(dir).map(f => /^resolution-v(\d+)\.md$/.exec(f)).filter(Boolean).map(m => Number(m[1]))
if (!resVersions.length) fail(`no resolution-v<n>.md in reviews/${target}/`)
const round = ROUND ?? Math.max(...resVersions)
const resPath = join(dir, `resolution-v${round}.md`)
if (!existsSync(resPath)) fail(`no resolution-v${round}.md`)
const resText = readFileSync(resPath, 'utf8')

// ---------- resolution frontmatter: findings map + status + fixed_commit ----------
const fmEnd = resText.indexOf('\n---', 3)
const fm = resText.startsWith('---') && fmEnd !== -1 ? resText.slice(3, fmEnd) : fail('resolution has no frontmatter')
const findings = new Map() // id -> {status, commit, note}
for (const m of fm.matchAll(/^ {2}([A-Za-z]+-?\d+(?:–[A-Za-z]*\d+)?):\s*\{\s*status:\s*([a-z-]+)([^\n]*)/gm) ?? []) {
  const restLine = m[3]
  const commit = (/commit:\s*"([^"]+)"/.exec(restLine) || /commit:\s*([0-9a-f]{7,40})/.exec(restLine))?.[1] ?? null
  const noteTxt = /note:\s*"((?:[^"\\]|\\.)*)"/.exec(restLine)?.[1] ?? null
  findings.set(m[1], { status: m[2], commit, note: noteTxt })
}
if (!findings.size) fail('resolution frontmatter has no findings map')
const resStatus = /^status:\s*(\S+)/m.exec(fm)?.[1] ?? 'open'
const fixedCommitRaw = /^fixed_commit:\s*(\S+)/m.exec(fm)?.[1] ?? 'null'
const fixedCommit = fixedCommitRaw === 'null' ? null : fixedCommitRaw.replace(/"/g, '')

const TALLY = { fixed: 'fixed', 'wont-fix': 'wont_fix', deferred: 'deferred', disputed: 'disputed', 'false-positive': 'false_positive' }
const tallies = { fixed: 0, wont_fix: 0, deferred: 0, disputed: 0, false_positive: 0, open: 0 }
for (const { status } of findings.values()) tallies[TALLY[status] ?? 'open']++

// ---------- severities + titles from findings-v<round>.md ----------
const sevTitle = new Map()
const fPath = join(dir, `findings-v${round}.md`)
if (existsSync(fPath)) {
  for (const m of readFileSync(fPath, 'utf8').matchAll(/^## (🔴|🟠|🟡|⚪) ([A-Za-z]+-?\d+)(?:\s*\/\s*D\d+)? — (.+)$/gm))
    sevTitle.set(m[2], { sev: m[1], title: m[3].trim() })
} else note(`no findings-v${round}.md — table gets no severity/title columns`)

// ---------- worklog slice for this round ----------
const wlPath = join(dir, 'worklog.jsonl')
if (!existsSync(wlPath)) fail(`no worklog.jsonl — the v2 fixer contract writes it as it works; nothing to compute runtime from`)
const events = readFileSync(wlPath, 'utf8').split(/\r?\n/).filter(l => l.trim()).map((l, i) => {
  try { return JSON.parse(l) } catch (e) { fail(`worklog line ${i + 1}: unparseable JSON (${e.message})`) }
})
const startIdx = events.findIndex(e => e.ev === 'round-start' && e.round === round)
if (startIdx === -1) fail(`worklog has no round-start for round ${round}`)
let endIdx = -1
for (let i = events.length - 1; i >= 0; i--) if (events[i].ev === 'round-end' && events[i].round === round) { endIdx = i; break }
const slice = events.slice(startIdx, endIdx === -1 ? undefined : endIdx + 1)
if (endIdx === -1) note('no round-end yet — treating the last event as the current end (in-progress render)')
const ts = e => Date.parse(e.t)
for (const e of slice) if (!Number.isFinite(ts(e))) fail(`worklog event with unparseable timestamp: ${JSON.stringify(e)}`)

// ---------- runtime: blocked (gates) / active (gaps <= 15 min) / idle ----------
const CAP_S = 30 * 60 // schema v3: gaps above this with no open gate = nobody at the wheel
const blocked = []
let open = null
for (const e of slice) {
  if (e.ev === 'gate-open' && !open) open = e
  else if (e.ev === 'gate-closed' && open) { blocked.push({ reason: open.reason ?? e.reason ?? 'unstated', s: Math.round((ts(e) - ts(open)) / 1000) }); open = null }
}
if (open) { blocked.push({ reason: `${open.reason ?? 'unstated'} (never closed)`, s: Math.round((ts(slice[slice.length - 1]) - ts(open)) / 1000) }); note('a gate-open has no gate-closed — counted to the end of the slice') }
const blockedS = blocked.reduce((a, b) => a + b.s, 0)
let activeS = 0
let inGate = false
for (let i = 0; i < slice.length - 1; i++) {
  if (slice[i].ev === 'gate-open') inGate = true
  if (slice[i].ev === 'gate-closed') inGate = false
  if (inGate) continue
  const gap = (ts(slice[i + 1]) - ts(slice[i])) / 1000
  if (gap >= 0 && gap <= CAP_S) activeS += gap
}
activeS = Math.round(activeS)
const started = slice[0].t, ended = slice[slice.length - 1].t
const idleS = Math.max(0, Math.round((ts(slice[slice.length - 1]) - ts(slice[0])) / 1000) - activeS - blockedS)

// ---------- counters from events ----------
const by = ev => slice.filter(e => e.ev === ev)
const testRuns = by('test-run')
const finals = testRuns.filter(e => e.kind === 'final')
const checksReturned = by('check-returned')
const checkTokens = checksReturned.reduce((a, e) => a + (Number.isFinite(e.tokens) ? e.tokens : NaN), 0)
const triage = by('triage-done')[0]
const microD = by('micro-review-dispatched').length
const microFound = by('micro-review-returned').reduce((a, e) => a + (Number.isFinite(e.found) ? e.found : 0), 0)

// ---------- base commit from the review frontmatter ----------
const revPath = join(dir, `review-v${round}.md`)
const baseCommit = existsSync(revPath) ? (/^commit:\s*([0-9a-f]{7,40})\b/m.exec(readFileSync(revPath, 'utf8').slice(0, 800))?.[1] ?? null) : null
if (!baseCommit) note(`review-v${round}.md commit not found — base_commit will be null`)

const line = {
  target, round, type: 'fix-round', date: (endIdx === -1 ? new Date(ts(slice[slice.length - 1])) : new Date(ts(slice[endIdx]))).toISOString().slice(0, 10),
  base_commit: baseCommit, fixed_commit: fixedCommit,
  findings: tallies,
  tests: { invocations: testRuns.length, red_runs: testRuns.filter(e => e.kind === 'red').length, green_runs: testRuns.filter(e => e.kind === 'green').length, final: finals.length ? { passed: finals[finals.length - 1].passed ?? null, failed: finals[finals.length - 1].failed ?? null } : null },
  approach_checks: { pre_cleared_consumed: Number.isFinite(triage?.pre_cleared) ? triage.pre_cleared : 0, run: by('check-dispatched').length, tokens: Number.isFinite(checkTokens) && checksReturned.length ? checkTokens : null },
  micro_reviews: { count: microD, follow_up_fixes: microFound },
  cost: { agents: by('check-dispatched').length + microD, tokens: null },
  runtime: { started, ended, active_s: activeS, blocked_s: blockedS, idle_s: idleS, blocked },
  notes: resStatus === 'resolved' ? '' : `resolution status: ${resStatus}`,
}

// ---------- findings table between markers ----------
const START = '<!-- rendered:findings-table:start -->', END = '<!-- rendered:findings-table:end -->'
let newResText = null
if (resText.includes(START) && resText.includes(END)) {
  const rows = [...findings.entries()].map(([id, f]) => {
    const st = sevTitle.get(id) ?? { sev: '', title: '' }
    const how = f.note ? (f.note.length > 110 ? f.note.slice(0, 107).trimEnd() + '…' : f.note) : '—'
    const commit = f.commit ? '`' + f.commit + '`' : '—'
    return `| ${id} | ${st.sev} | ${st.title} | ${f.status} | ${commit} | ${how} |`
  })
  const table = ['| ID | Sev | Title | Status | Commit | How |', '|---|---|---|---|---|---|', ...rows].join('\n')
  newResText = resText.slice(0, resText.indexOf(START) + START.length) + '\n' + table + '\n' + resText.slice(resText.indexOf(END))
} else note('resolution has no rendered:findings-table markers — table skipped (pre-v2 round or hand-kept table)')

// ---------- duplicate guard + write ----------
const metricsPath = join(dir, 'metrics.jsonl')
const already = existsSync(metricsPath) && readFileSync(metricsPath, 'utf8').split('\n').filter(l => l.trim()).some(l => {
  try { const o = JSON.parse(l); return o.type === 'fix-round' && o.round === round && !o.correction_for } catch { return false }
})

console.log(JSON.stringify(line, null, 2))
console.log(`\nindex.md status suggestion for "${target}": fix round v${round} ${resStatus} — ${tallies.fixed} fixed / ${tallies.open} open, active ${(activeS / 60).toFixed(0)} min, blocked ${(blockedS / 60).toFixed(0)} min`)
if (DRY) { note('dry-run: nothing written'); process.exit(0) }
if (already) fail(`metrics.jsonl already has a fix-round line for round ${round} — append a correction line instead (schema: Corrections)`)
if (newResText) writeFileSync(resPath, newResText)
const prev = existsSync(metricsPath) ? readFileSync(metricsPath, 'utf8') : ''
appendFileSync(metricsPath, (prev && !prev.endsWith('\n') ? '\n' : '') + JSON.stringify(line) + '\n')
note(`appended fix-round line for round ${round}${newResText ? ' and refreshed the findings table' : ''} — now run: node reviews/lib/records-auditor.mjs ${target}`)
