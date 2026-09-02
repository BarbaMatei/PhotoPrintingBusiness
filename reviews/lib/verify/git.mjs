// The git environment a verifier — and any throwaway-repo fixture — must run in. One home,
// because verify-fixes and the test harness both shell out to git from inside a git hook.
import { spawnSync } from 'node:child_process'

const FALLBACK_GIT_ENV_VARS = ['GIT_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_OBJECT_DIRECTORY', 'GIT_ALTERNATE_OBJECT_DIRECTORIES', 'GIT_CEILING_DIRECTORIES', 'GIT_PREFIX']

// A git hook's own GIT_DIR/GIT_WORK_TREE/GIT_COMMON_DIR (etc.) would otherwise leak in and
// redirect these calls at the hook's repo; ask git for the authoritative list (git help
// githooks: "unset $(git rev-parse --local-env-vars)") rather than guessing.
// The list belongs to the git binary and is asked once, but process.env can change under us.
let gitEnvVars = null
export function scrubbedGitEnv() {
  if (gitEnvVars === null) {
    const localEnvVars = spawnSync('git', ['rev-parse', '--local-env-vars'], { encoding: 'utf8' })
    gitEnvVars = localEnvVars.status === 0 ? localEnvVars.stdout.split(/\r?\n/).filter(Boolean) : FALLBACK_GIT_ENV_VARS
  }
  const gitEnv = { ...process.env }
  for (const k of gitEnvVars) delete gitEnv[k]
  return gitEnv
}
