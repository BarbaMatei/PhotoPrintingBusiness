// The one frontmatter reader for reviews/ records, and the one "## <heading>" section splitter.
// Two block shapes exist because the records are read two ways: the strict block a router-grade
// reader needs (`---` with a newline after it, a closing `---`, CRLF or LF, no block = no
// frontmatter), and the lenient read the auditor and the renderer do, where the opener needs no
// newline and the opener's own newline may close an empty block. `fm` is null when the text
// carries no block at all, so a caller can tell "absent" from "present but empty".
// No I/O, no dependencies.

// { fm, body }. Lenient mode keeps fm's own line endings verbatim (leading newline, trailing \r
// included); strict mode trims them, so a CRLF file and an LF file read the same.
export function parse(text, { lenient = false } = {}) {
  const src = typeof text === 'string' ? text : String(text ?? '')
  const none = { fm: null, body: src }
  if (!src.startsWith('---')) return none
  const open = lenient ? null : /^---\r?\n/.exec(src)
  if (!lenient && !open) return none
  const end = src.indexOf('\n---', lenient ? 3 : open[0].length)
  if (end === -1) return none
  const raw = src.slice(3, end)
  const after = src.slice(end + 4)
  return {
    fm: lenient ? raw : raw.replace(/^\r?\n/, '').replace(/\r$/, ''),
    body: after.startsWith('\n') ? after.slice(1) : after,
  }
}

// Rest of the key's line, trimmed. The `\s*` after the colon can reach the next line, which is
// what every caller of this shape does today.
export const value = (fm, key) =>
  new RegExp(`^${key}:\\s*(.+?)\\s*$`, 'm').exec(fm ?? '')?.[1] ?? null

// First whitespace-free token of the key's value. `acrossLines` allows the gap after the colon to
// cross a newline, so a key with an empty value reads the next line's first token.
export const word = (fm, key, { acrossLines = false } = {}) =>
  new RegExp(`^${key}:${acrossLines ? '\\s' : '[ \\t]'}*(\\S+)`, 'm').exec(fm ?? '')?.[1] ?? null

// An ISO date value: a value of another shape reads as absent, never as a comparable string.
export const dateValue = (fm, key, { acrossLines = false } = {}) =>
  new RegExp(`^${key}:${acrossLines ? '\\s' : '[ \\t]'}*(\\d{4}-\\d{2}-\\d{2})`, 'm').exec(fm ?? '')?.[1] ?? null

// The named "## " section of a body, heading line included minus its "## ", or '' when absent.
export const section = (text, name) =>
  (text ?? '').split(/^## /m).find(s => s.startsWith(name)) ?? ''
