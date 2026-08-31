// The two pieces of argv handling every CLI in reviews/lib shares: where the repo is, and
// stripping `--root <dir>` out of the argument list. Everything else stays with each command,
// because no two of these CLIs read their flags the same way — one validates that a flag was
// given a value, another turns unknown `--<key> <value>` pairs into worklog fields, a third lets
// the last positional win. A shared flag parser would have to reproduce all three, so it does not
// exist: takeRoot runs first and each command's own loop reads what is left.
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

// A lib script lives at <repo>/reviews/lib/<name>.mjs, so the repo root is two levels up from it;
// an explicit --root wins. Pass import.meta.url from the script itself, never from a module.
export const repoRoot = (importMetaUrl, overrideRoot) =>
  overrideRoot || join(dirname(fileURLToPath(importMetaUrl)), '..', '..')

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
