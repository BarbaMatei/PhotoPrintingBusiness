// Pins the flat reviews/lib/<name>.mjs command paths against the module each one forwards to.
// These paths are the permanent command surface: the pre-commit hook, all four skills and three
// runbooks name only them, so a forward that breaks stays invisible until a live pass fails.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only shims
import { check, run, firstLine, BAD_STATE_ROOT } from '../lib.mjs'
import { existsSync, readFileSync } from 'node:fs'
import { join } from 'node:path'
import { REVIEWS } from '../../records/schema.mjs'
import * as wlHome from '../../records/wl.mjs'

// No args, so each command answers its own usage line — ~100ms apiece. records-auditor is the
// exception: with no args it audits every live target (30s), so its pin is scoped to a fixture root.
const SHIMS = [
  ['wl.mjs', 'records/wl.mjs', []],
  ['doc-gate.mjs', 'records/doc-gate.mjs', []],
  ['records-auditor.mjs', 'records/records-auditor.mjs', ['--root', BAD_STATE_ROOT]],
  ['render-records.mjs', 'records/render-records.mjs', []],
  ['route-next-pass.mjs', 'drive/route-next-pass.mjs', []],
  ['autonomy-policy.mjs', 'drive/autonomy-policy.mjs', []],
  ['verify-fixes.mjs', 'verify/verify-fixes.mjs', []],
  ['run-scoped-tests.mjs', 'fix/run-scoped-tests.mjs', []],
  ['mint-id.mjs', 'review/mint-id.mjs', []],
  ['summary-data.mjs', 'review/summary-data.mjs', []],
  ['speed-report.mjs', 'measure/speed-report.mjs', []],
]

for (const [command, home, args] of SHIMS) {
  const shim = run(command, args)
  const direct = run(home, args)
  check(`reviews/lib/${command} answers exactly as ${home} does`,
    shim.code === direct.code && firstLine(shim.out) === firstLine(direct.out),
    `${command} exit ${shim.code} "${firstLine(shim.out)}" vs ${home} exit ${direct.code} "${firstLine(direct.out)}"`)
}

// wl.mjs is the one entry point that calls a named export rather than importing for side effects,
// so the export it calls is pinned separately.
check('records/wl.mjs exports the main() that the wl.mjs entry point calls',
  typeof wlHome.main === 'function', `main is ${typeof wlHome.main}`)

// The discovery workflow is not a module — the Workflow harness wraps its source, which carries a
// top-level return — so it cannot take a re-export entry point. It stays at the scriptPath the
// runbook names, and that path is pinned here instead.
{
  const at = join(REVIEWS, 'lib', 'discovery-review.wf.js')
  const text = existsSync(at) ? readFileSync(at, 'utf8') : ''
  check('discovery-review.wf.js still sits at the scriptPath runbook-discovery.md names, meta first',
    existsSync(at) && /^export const meta = \{/m.test(text) && text.includes("name: 'discovery-review'"),
    existsSync(at) ? firstLine(text) : `${at} is missing`)
  const runbook = join(REVIEWS, 'runbooks', 'runbook-discovery.md')
  check('runbook-discovery.md still names that exact scriptPath',
    readFileSync(runbook, 'utf8').includes("scriptPath: 'reviews/lib/discovery-review.wf.js'"),
    'the runbook no longer names reviews/lib/discovery-review.wf.js')
}
