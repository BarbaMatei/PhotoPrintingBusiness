import { readdirSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { spawnSync } from 'node:child_process'

const dir = dirname(fileURLToPath(import.meta.url))
const files = readdirSync(dir).filter(f => f.endsWith('.test.mjs')).sort().map(f => join(dir, f))
const r = spawnSync(process.execPath, ['--test', ...files], { stdio: 'inherit' })
process.exit(r.status ?? 1)
