// The one target-folder lookup and the one record-version listing. A target lives in
// reviews/<name> until it is archived, and then in reviews/archive/<name> — every reader looks in
// that order, so the lookup lives here once and a caller that must also handle "neither exists"
// (the stamper creating a folder, a reader falling back to the archive path) picks the candidate
// it wants from targetDirs. `versions` and `newest` answer "which resolution/review files exist"
// and "which is the newest", the question ten call sites asked with their own readdir + regex.
// No fuzzy matching here: route-next-pass's findDir matches a partial name and reports an
// ambiguity, which is a different question and stays its own.
import { existsSync, readdirSync } from 'node:fs'
import { join } from 'node:path'

const KINDS = { resolution: /^resolution-v(\d+)\.md$/, review: /^review-v(\d+)\.md$/ }

// Both candidates in lookup order, whether or not they exist: [live, archive].
export const targetDirs = (reviewsDir, name) => [join(reviewsDir, name), join(reviewsDir, 'archive', name)]

// The folder a target's records are in, or null when neither candidate exists.
export const resolveDir = (reviewsDir, name) => targetDirs(reviewsDir, name).find(existsSync) ?? null

// The version numbers of a target's resolution-v<n>.md or review-v<n>.md files, ascending.
export function versions(dir, kind) {
  const re = KINDS[kind]
  if (!re) throw new Error(`unknown record kind "${kind}" — resolution|review`)
  return readdirSync(dir).map(f => re.exec(f)).filter(Boolean).map(m => Number(m[1])).sort((a, b) => a - b)
}

// The highest version, or 0 when the target has none — round and pass numbers start at 1.
export function newest(dir, kind) {
  const ns = versions(dir, kind)
  return ns.length ? ns[ns.length - 1] : 0
}
