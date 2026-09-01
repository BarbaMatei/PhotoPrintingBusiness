// The two pieces of argv handling every CLI in reviews/lib shares: where the repo is, and
// stripping `--root <dir>` out of the argument list. Everything else stays with each command,
// because no two of these CLIs read their flags the same way — one validates that a flag was
// given a value, another turns unknown `--<key> <value>` pairs into worklog fields, a third lets
// the last positional win. A shared flag parser would have to reproduce all three, so it does not
// exist: takeRoot runs first and each command's own loop reads what is left.
import { join, dirname, sep } from 'node:path'
import { fileURLToPath } from 'node:url'

// A script lives somewhere under <repo>/reviews/lib/, at whatever depth its system folder puts it,
// so the root is the path above that marker rather than a fixed number of levels up; an explicit
// --root wins. Pass import.meta.url from the script itself, never from a module.
const MARKER = `${sep}reviews${sep}lib`
export const repoRoot = (importMetaUrl, overrideRoot) => {
  if (overrideRoot) return overrideRoot
  const dir = dirname(fileURLToPath(importMetaUrl))
  const at = dir.lastIndexOf(MARKER)
  return at === -1 ? join(dir, '..', '..') : dir.slice(0, at)
}

// { root, rest }: `--root` and its value removed, every other argument kept in order. A trailing
// `--root` with no value reads as no root, so the caller falls back to the derived one.
export function takeRoot(argv) {
  let root = null
  const rest = []
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--root') root = argv[++i] ?? null
    else rest.push(argv[i])
  }
  return { root, rest }
}
