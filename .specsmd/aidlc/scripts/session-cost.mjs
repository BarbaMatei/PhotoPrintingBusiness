#!/usr/bin/env node
import { existsSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { homedir } from 'node:os';

const USAGE = `Usage: node .specsmd/aidlc/scripts/session-cost.mjs [<worktree>] [--since <ISO>] [--bolt <id>]
         [--stage <name>] [--append <construction-log.md>] [--transcript <file.jsonl>]

Measures what a Claude Code session cost, from its transcript on disk.

  <worktree>           the worktree the session ran in (default: the current directory). Its
                       transcript is the newest .jsonl under %USERPROFILE%\\.claude\\projects\\<slug>,
                       where <slug> is the absolute worktree path with every non-alphanumeric
                       character replaced by "-" (D:\\worktrees\\bolt-054 -> D--worktrees-bolt-054).
  --since <ISO>        count only assistant turns after this timestamp (a stage boundary)
  --bolt <id>          bolt id written into the output row (default "-")
  --stage <name>       stage name written into the output row (default "-")
  --append <path>      append the row to this construction-log.md, creating its "## Session cost"
                       table when the log has none yet; the report is still printed
  --transcript <file>  read this .jsonl instead of the newest one for the worktree

Reports turns (assistant transcript lines), tool calls, subagent launches (Agent tool calls),
fresh input (input + cache creation), cache read, output, cache misses (turns creating more
than 50k cache tokens) and gaps over five minutes between turns.
Row: | <ISO date> | <bolt> | <stage> | <turns> | <tools> | <fresh> | <cache-read> | <output> | <misses> |
Exit: 0 measured (and appended when asked) · 1 no transcript or no construction log · 2 usage error`;

const CACHE_MISS_TOKENS = 50000;
const GAP_MS = 5 * 60 * 1000;
const TABLE_HEADER = [
    '| Date | Bolt | Stage | Turns | Tools | Fresh | Cache read | Output | Misses |',
    '|---|---|---|---|---|---|---|---|---|'
];

function fail(message, code = 1) {
    console.error(`session-cost: ${message}`);
    if (code === 2) console.error(`\n${USAGE}`);
    process.exit(code);
}

function parseArgs(argv) {
    const args = { worktree: process.cwd(), since: null, bolt: '-', stage: '-', append: null, transcript: null };
    for (let i = 0; i < argv.length; i++) {
        const a = argv[i];
        if (a === '--help' || a === '-h') {
            console.log(USAGE);
            process.exit(0);
        } else if (a === '--since') args.since = argv[++i];
        else if (a === '--bolt') args.bolt = argv[++i];
        else if (a === '--stage') args.stage = argv[++i];
        else if (a === '--append') args.append = argv[++i];
        else if (a === '--transcript') args.transcript = argv[++i];
        else if (a.startsWith('--')) fail(`unknown option ${a}`, 2);
        else args.worktree = a;
    }
    for (const key of ['since', 'bolt', 'stage', 'append', 'transcript']) {
        if (args[key] === undefined) fail(`--${key} needs a value`, 2);
    }
    if (args.since !== null && Number.isNaN(Date.parse(args.since))) fail(`--since is not an ISO timestamp: ${args.since}`, 2);
    return args;
}

const slugOf = worktree => resolve(worktree).replace(/[^a-zA-Z0-9]/g, '-');

function newestTranscript(worktree) {
    const dir = join(homedir(), '.claude', 'projects', slugOf(worktree));
    if (!existsSync(dir)) fail(`no transcript folder for ${resolve(worktree)}: ${dir}`);
    const files = readdirSync(dir).filter(f => f.endsWith('.jsonl')).map(f => join(dir, f));
    if (files.length === 0) fail(`no .jsonl transcript under ${dir}`);
    return files.sort((a, b) => statSync(b).mtimeMs - statSync(a).mtimeMs)[0];
}

function measure(text, sinceMs) {
    const totals = { turns: 0, responses: 0, tools: 0, agents: 0, fresh: 0, cacheRead: 0, output: 0, misses: 0, gaps: 0, last: null };
    const responseIds = new Set();
    let previous = null;
    for (const line of text.split('\n')) {
        if (!line.trim()) continue;
        let entry;
        try { entry = JSON.parse(line); } catch { continue; }
        if (entry.type !== 'assistant' || !entry.message?.usage) continue;
        const at = Date.parse(entry.timestamp);
        if (sinceMs !== null && !(at > sinceMs)) continue;
        const usage = entry.message.usage;
        totals.turns++;
        responseIds.add(entry.message.id ?? entry.uuid);
        totals.fresh += (usage.input_tokens || 0) + (usage.cache_creation_input_tokens || 0);
        totals.cacheRead += usage.cache_read_input_tokens || 0;
        totals.output += usage.output_tokens || 0;
        if ((usage.cache_creation_input_tokens || 0) > CACHE_MISS_TOKENS) totals.misses++;
        if (previous !== null && at - previous > GAP_MS) totals.gaps++;
        if (!Number.isNaN(at)) {
            previous = at;
            totals.last = at;
        }
        for (const item of entry.message.content || []) {
            if (item.type !== 'tool_use') continue;
            totals.tools++;
            if (item.name === 'Agent') totals.agents++;
        }
    }
    totals.responses = responseIds.size;
    return totals;
}

const millions = n => `${(n / 1e6).toFixed(1)}M`;
const isoSeconds = ms => new Date(ms).toISOString().replace(/\.\d{3}Z$/, 'Z');

function formatRow(totals, bolt, stage) {
    const date = isoSeconds(totals.last ?? Date.now());
    return `| ${date} | ${bolt} | ${stage} | ${totals.turns} | ${totals.tools} | ${millions(totals.fresh)} | ${millions(totals.cacheRead)} | ${millions(totals.output)} | ${totals.misses} |`;
}

function printReport(transcript, since, totals, row) {
    console.log(`transcript: ${transcript}${since ? ` (turns after ${since})` : ''}`);
    console.log(`turns ${totals.turns} (${totals.responses} distinct responses) · tool calls ${totals.tools} · subagent launches ${totals.agents}`);
    console.log(`fresh input ${millions(totals.fresh)} · cache read ${millions(totals.cacheRead)} · output ${millions(totals.output)}`);
    console.log(`cache misses ${totals.misses} · gaps over 5 min ${totals.gaps}`);
    console.log(row);
}

function appendRow(logPath, row) {
    if (!existsSync(logPath)) fail(`construction log not found, row not appended: ${logPath}`);
    const original = readFileSync(logPath, 'utf8');
    const eol = original.includes('\r\n') ? '\r\n' : '\n';
    const lines = original.split(/\r?\n/);
    while (lines.length && lines[lines.length - 1] === '') lines.pop();
    const heading = lines.findIndex(l => /^## Session cost\s*$/.test(l));
    if (heading === -1) {
        if (lines.length) lines.push('');
        lines.push('## Session cost', '', ...TABLE_HEADER, row);
    } else {
        let end = lines.findIndex((l, i) => i > heading && /^## /.test(l));
        if (end === -1) end = lines.length;
        let lastTableLine = -1;
        for (let i = heading + 1; i < end; i++) if (lines[i].startsWith('|')) lastTableLine = i;
        if (lastTableLine === -1) lines.splice(heading + 1, 0, '', ...TABLE_HEADER, row);
        else lines.splice(lastTableLine + 1, 0, row);
    }
    writeFileSync(logPath, lines.join(eol) + eol);
    console.log(`appended to ${logPath}`);
}

function main() {
    const args = parseArgs(process.argv.slice(2));
    const transcript = args.transcript ?? newestTranscript(args.worktree);
    if (!existsSync(transcript)) fail(`transcript not found: ${transcript}`);
    const sinceMs = args.since === null ? null : Date.parse(args.since);
    const totals = measure(readFileSync(transcript, 'utf8'), sinceMs);
    const row = formatRow(totals, args.bolt, args.stage);
    printReport(transcript, args.since, totals, row);
    if (totals.turns === 0) console.error('session-cost: no assistant turns counted');
    if (args.append) appendRow(args.append, row);
}

main();
