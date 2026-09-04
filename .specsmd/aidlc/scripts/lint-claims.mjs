#!/usr/bin/env node
import { existsSync, readFileSync } from 'node:fs'

const USAGE = `Usage: node .specsmd/aidlc/scripts/lint-claims.mjs <artifact.md> [--headings "A,B,C"] [--strict]

Claims carry evidence. Under every heading whose text starts with one of the checked names
(default: Done, What changed, Completed Work, Verification, Results — case-insensitive; a checked
section runs to the next heading of the same or a higher level), every bullet or numbered item must
contain at least one of: a repository path (something.cs, .ts, .md, .json, ...), a file:line, a
markdown #L<n> anchor, or an inline command (\`dotnet ...\`, \`node ...\`, \`npm ...\`, \`git ...\`, ...).
A bullet without one is an error.

An integer over 9 in such a bullet that is not followed, in the same bullet, by "measured", "from",
"via", "per" or a path is a warning (an error with --strict). Years, ISO dates and times, PPW-<n>,
zero-padded bolt ids, :<line>, #<n>, v<n> and dotted versions are not counted.

Output: <file>:<line>: error|warning: <what is missing>
Exit: 0 no errors · 1 errors (or warnings under --strict) · 2 usage.`

const DEFAULT_HEADINGS = ['Done', 'What changed', 'Completed Work', 'Verification', 'Results']
const PATH_RE = /(?:^|[\s(`'"\[<])[\w.\\/-]*[\w-]+\.(?:cs|ts|tsx|js|mjs|cjs|md|json|jsonl|yml|yaml|ps1|cshtml|scss|css|html|csproj|props|sln|editorconfig|sql|xml|txt|razor)\b/i
const PATH_RE_G = new RegExp(PATH_RE.source, 'gi')
const FILE_LINE_RE = /\.\w+:\d+/
const ANCHOR_RE = /#L\d+/
const COMMAND_RE = /^(?:node|dotnet|npm|npx|git|pwsh|powershell|curl|bash|sh|ls|grep|rg|docker|psql|gh)\b/
const CODE_SPAN_RE = /`([^`]*)`/g
const NOT_NUMBERS = [
  /\d{4}-\d{2}-\d{2}(?:T\d{2}:\d{2}(?::\d{2})?(?:\.\d+)?Z?)?/g,
  /PPW-\d+/g, /#\d+/g, /\bv\d+\b/g, /\b\d+(?:\.\d+)+\b/g, /:\d+\b/g, /\b0\d{2}\b/g, /\b\d{3}-[A-Za-z]/g, /\b(?:19|20)\d{2}\b/g,
]
const EVIDENCE_WORD_RE = /\b(?:measured|from|via|per)\b/i
const BULLET_RE = /^(\s*)(?:[-*+]|\d+[.)])\s+(.*)$/
const HEADING_RE = /^(#{1,6})\s+(.*?)\s*#*\s*$/

function usage(message) {
  console.error(`usage: ${message}\n\n${USAGE}`)
  process.exit(2)
}

function parseArgs(argv) {
  const args = { file: null, headings: DEFAULT_HEADINGS, strict: false }
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--help' || argv[i] === '-h') { console.log(USAGE); process.exit(0) }
    else if (argv[i] === '--headings') args.headings = String(argv[++i] ?? '').split(',').map(s => s.trim()).filter(Boolean)
    else if (argv[i] === '--strict') args.strict = true
    else if (args.file === null) args.file = argv[i]
    else usage(`unexpected argument ${argv[i]}`)
  }
  if (!args.file) usage('wants an artifact path')
  if (!existsSync(args.file)) usage(`no such file ${args.file}`)
  return args
}

function collectBullets(lines, headings) {
  const wanted = headings.map(h => h.toLowerCase())
  const bullets = []
  let active = null
  let current = null
  const close = () => { if (current) bullets.push(current); current = null }
  lines.forEach((line, i) => {
    const heading = HEADING_RE.exec(line)
    if (heading) {
      close()
      const level = heading[1].length
      if (active && level <= active) active = null
      const text = heading[2].trim().toLowerCase()
      if (wanted.some(w => text === w || text.startsWith(`${w} `) || text.startsWith(`${w}:`))) active = level
      return
    }
    if (!active) { close(); return }
    const bullet = BULLET_RE.exec(line)
    if (bullet) { close(); current = { line: i + 1, text: bullet[2] }; return }
    if (current && /^\s+\S/.test(line)) { current.text += ` ${line.trim()}`; return }
    close()
  })
  close()
  return bullets
}

function hasEvidence(text) {
  if (PATH_RE.test(text) || FILE_LINE_RE.test(text) || ANCHOR_RE.test(text)) return true
  for (const m of text.matchAll(CODE_SPAN_RE)) if (COMMAND_RE.test(m[1].trim())) return true
  return false
}

function unsourcedNumbers(text) {
  let scrubbed = text.replace(CODE_SPAN_RE, m => ' '.repeat(m.length)).replace(PATH_RE_G, m => ' '.repeat(m.length))
  for (const re of NOT_NUMBERS) scrubbed = scrubbed.replace(re, m => ' '.repeat(m.length))
  const found = []
  for (const m of scrubbed.matchAll(/\b\d+\b/g)) {
    if (Number(m[0]) <= 9) continue
    const after = text.slice(m.index + m[0].length)
    if (EVIDENCE_WORD_RE.test(after) || PATH_RE.test(after)) continue
    found.push(m[0])
  }
  return found
}

function main() {
  const { file, headings, strict } = parseArgs(process.argv.slice(2))
  const lines = readFileSync(file, 'utf8').split(/\r?\n/)
  let errors = 0
  for (const bullet of collectBullets(lines, headings)) {
    if (!hasEvidence(bullet.text)) {
      console.log(`${file}:${bullet.line}: error: no evidence — add a repository path, a file:line, or the command that shows it`)
      errors++
    }
    const numbers = unsourcedNumbers(bullet.text)
    if (numbers.length) {
      const level = strict ? 'error' : 'warning'
      console.log(`${file}:${bullet.line}: ${level}: numbers without a source: ${numbers.join(', ')} — follow them with "measured", "from", "via", "per" or the path they came from`)
      if (strict) errors++
    }
  }
  process.exit(errors ? 1 : 0)
}

main()
