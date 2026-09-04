import { test } from 'node:test'
import assert from 'node:assert/strict'
import { mkdtempSync, writeFileSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { tmpdir } from 'node:os'
import { spawnSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'

const SCRIPT = join(dirname(fileURLToPath(import.meta.url)), '..', 'lint-claims.mjs')

function lint(content, ...args) {
  const dir = mkdtempSync(join(tmpdir(), 'lint-claims-'))
  const file = join(dir, 'implementation-walkthrough.md')
  writeFileSync(file, content)
  const r = spawnSync(process.execPath, [SCRIPT, file, ...args], { encoding: 'utf8' })
  const lines = `${r.stdout}${r.stderr}`.split(/\r?\n/).filter(Boolean)
  const at = level => lines.filter(l => l.includes(`: ${level}: `)).map(l => Number(/:(\d+): /.exec(l)[1]))
  return { code: r.status, out: lines.join('\n'), errors: at('error'), warnings: at('warning') }
}

const ARTIFACT = [
  '---',                                                                                  // 1
  'stage: implement',                                                                     // 2
  '---',                                                                                  // 3
  '## Context',                                                                           // 4
  '- prose with no evidence is fine outside the checked headings, even with 42 numbers', // 5
  '',                                                                                     // 6
  '## Completed Work',                                                                    // 7
  '- Added `src/PhotoPrint.API/Orders/RefundService.cs` with the refund path',            // 8
  '- Fixed the off-by-one in RefundService.cs:42',                                        // 9
  '- Ran `dotnet test src/PhotoPrint.Tests --filter Orders` before hand-back',            // 10
  '- Refactored the service for clarity',                                                 // 11
  '- 12 tests added',                                                                     // 12
  '- 12 tests added per `dotnet test src/PhotoPrint.Tests --filter Orders` output',       // 13
  '- Wired the new endpoint',                                                             // 14
  '  into the router at src/PhotoPrint.API/Program.cs',                                   // 15
  '- Bolt 054 done on 2026-09-04T10:00:00Z for PPW-123 at :42, v3, version 1.2.3, see #18 and 3 cases', // 16
  '- Reduced the payload by 40% and cut 250 ms of latency, measured with `curl -w`',      // 17
  '- Reduced the payload by 40% and cut 250 ms of latency',                               // 18
  '1. Numbered items count as bullets too',                                               // 19
  '',                                                                                     // 20
  '### Verification',                                                                     // 21
  '- All green',                                                                          // 22
  '',                                                                                     // 23
  '## Notes',                                                                             // 24
  '- 500 unrelated things, unchecked heading',                                            // 25
  '',                                                                                     // 26
  '## Done',                                                                              // 27
  '- see [the plan](memory-bank/bolts/054-x/implementation-plan.md)',                      // 28
].join('\n') + '\n'

test('--help prints usage and exits 0', () => {
  const r = spawnSync(process.execPath, [SCRIPT, '--help'], { encoding: 'utf8' })
  assert.equal(r.status, 0)
  assert.match(`${r.stdout}${r.stderr}`, /lint-claims\.mjs <artifact\.md>/)
})

test('a missing file is a usage error (exit 2)', () => {
  const r = spawnSync(process.execPath, [SCRIPT, join(tmpdir(), 'does-not-exist.md')], { encoding: 'utf8' })
  assert.equal(r.status, 2)
})

test('bullets under checked headings without a path, file:line or command are errors', () => {
  const r = lint(ARTIFACT)
  assert.equal(r.code, 1, r.out)
  assert.deepEqual(r.errors, [11, 12, 16, 18, 19, 22], r.out)
})

test('a bullet that continues on an indented line is judged as one bullet (the path on line 15 evidences line 14)', () => {
  const r = lint(ARTIFACT)
  assert.equal(r.errors.includes(14), false, r.out)
  assert.equal(r.errors.includes(15), false, r.out)
})

test('unevidenced numbers over 9 are warnings, and ids, dates, versions and line refs are not numbers', () => {
  const r = lint(ARTIFACT)
  assert.deepEqual(r.warnings, [12, 18], r.out)
  assert.match(r.out, /:12: warning: .*12/)
})

test('an evidenced number ("per", "measured", "from", "via" or a path in the bullet) is not a warning', () => {
  const r = lint(ARTIFACT)
  assert.equal(r.warnings.includes(13), false, r.out)
  assert.equal(r.warnings.includes(17), false, r.out)
})

test('--strict turns the warnings into errors', () => {
  const r = lint(ARTIFACT, '--strict')
  assert.deepEqual(r.errors, [11, 12, 12, 16, 18, 18, 19, 22], r.out)
  assert.equal(r.warnings.length, 0)
  const onlyNumbers = '## Done\n- 12 tests pass according to `dotnet test src/PhotoPrint.Tests`\n'
  const lenient = lint(onlyNumbers)
  assert.equal(lenient.code, 0, lenient.out)
  assert.deepEqual(lenient.warnings, [2])
  assert.equal(lint(onlyNumbers, '--strict').code, 1)
  const pathAfterNumber = lint('## Done\n- 12 tests pass in `src/PhotoPrint.Tests/Orders/RefundTests.cs`\n', '--strict')
  assert.equal(pathAfterNumber.code, 0, pathAfterNumber.out)
})

test('--headings replaces the checked heading list', () => {
  const r = lint(ARTIFACT, '--headings', 'Notes')
  assert.deepEqual(r.errors, [25], r.out)
  assert.deepEqual(r.warnings, [25], r.out)
})

test('a clean artifact prints nothing and exits 0', () => {
  const r = lint('## Completed Work\n- Added `src/PhotoPrint.API/Orders/RefundService.cs`\n- Ran `dotnet test src/PhotoPrint.Tests --filter Orders`\n\n## Done\n- everything above, see docs/testing/invariants.md\n')
  assert.equal(r.code, 0, r.out)
  assert.equal(r.out, '')
})

test('the default heading list covers the shapes real test reports and walkthroughs use', () => {
  const report = '## Test Report: x\n\n### Summary\n\n- **Scoped tests**: 48/48 passed (1s)\n\n### Acceptance Criteria Validation\n\n- ✅ the builder returns UTF-8 without BOM\n\n### Test Files\n\n- src/PhotoPrint.Tests/Unit/RefundTests.cs\n\n### Issues Found\n\n- none worth noting\n'
  const r = lint(report)
  assert.deepEqual(r.errors, [5, 9, 17], r.out)
  assert.deepEqual(r.warnings, [5], r.out)
  assert.match(r.out, /:5: warning: numbers without a source: 48 —/)
})

test('a checked heading ends at the next heading of the same or a higher level, not at a deeper one', () => {
  const r = lint('## Completed Work\n- no evidence here\n### Details\n- nor here\n## Other\n- but this is outside\n')
  assert.deepEqual(r.errors, [2, 4], r.out)
})
