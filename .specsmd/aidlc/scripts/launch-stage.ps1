param(
    [Parameter(Mandatory = $true)][string]$Worktree,
    [Parameter(Mandatory = $true)][string]$Bolt,
    [Parameter(Mandatory = $true)][string]$Stage,
    [string]$Model = 'opus',
    [string]$Effort = 'xhigh',
    [int]$Autocompact = 120000,
    [string]$PromptFile,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$Utf8NoBom = New-Object System.Text.UTF8Encoding $false
$EmDash = [string][char]0x2014
$RulesPath = Join-Path $PSScriptRoot 'working-rules.md'
$CostScript = Join-Path $PSScriptRoot 'session-cost.mjs'

function Read-Utf8([string]$Path) {
    return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
}

function Find-Dir([string]$Parent, [string]$Name) {
    $direct = Join-Path $Parent $Name
    if (Test-Path $direct -PathType Container) { return (Get-Item $direct).FullName }
    $found = Get-ChildItem $Parent -Directory | Where-Object { $_.Name.EndsWith("-$Name") -or $_.Name.StartsWith("$Name-") } | Select-Object -First 1
    if ($null -eq $found) { throw "no folder named or ending in '$Name' under $Parent" }
    return $found.FullName
}

function Get-Frontmatter([string]$Text) {
    $m = [regex]::Match($Text, '(?s)\A---\r?\n(.*?)\r?\n---')
    if (-not $m.Success) { throw 'bolt.md has no frontmatter' }
    return $m.Groups[1].Value
}

function Get-FrontmatterValue([string]$Frontmatter, [string]$Key) {
    $m = [regex]::Match($Frontmatter, '(?m)^' + [regex]::Escape($Key) + ':[ \t]*(.*?)[ \t]*$')
    if (-not $m.Success) { return '' }
    return $m.Groups[1].Value.Trim().Trim('"', "'")
}

function Get-CompletedStages([string]$Frontmatter) {
    $block = [regex]::Match($Frontmatter, '(?ms)^stages_completed:[ \t]*\r?\n(.*?)(?=^\S|\z)')
    if (-not $block.Success) { return @() }
    $names = @()
    foreach ($m in [regex]::Matches($block.Groups[1].Value, '(?m)^[ \t]*-[ \t]*name:[ \t]*(\S+)')) { $names += $m.Groups[1].Value }
    return $names
}

function Get-LastStageExit([string]$LogText) {
    $found = [regex]::Matches($LogText, '(?m)^## Stage exit ' + $EmDash + ' ')
    if ($found.Count -eq 0) { return $null }
    $rest = $LogText.Substring($found[$found.Count - 1].Index)
    $next = [regex]::Match($rest.Substring(1), '(?m)^## ')
    if ($next.Success) { $rest = $rest.Substring(0, $next.Index + 1) }
    return $rest.Trim()
}

function Get-RelativePath([string]$Root, [string]$Path) {
    return $Path.Substring($Root.Length).TrimStart('\', '/').Replace('\', '/')
}

$Worktree = (Resolve-Path $Worktree).Path
$boltDir = Find-Dir (Join-Path $Worktree 'memory-bank\bolts') $Bolt
$Bolt = (Get-Item $boltDir).Name
$boltMd = Join-Path $boltDir 'bolt.md'
if (-not (Test-Path $boltMd)) { throw "bolt file not found: $boltMd" }
$frontmatter = Get-Frontmatter (Read-Utf8 $boltMd)
$status = Get-FrontmatterValue $frontmatter 'status'
$currentStage = Get-FrontmatterValue $frontmatter 'current_stage'
$intent = Get-FrontmatterValue $frontmatter 'intent'
$unit = Get-FrontmatterValue $frontmatter 'unit'
if (-not $intent -or -not $unit) { throw "bolt.md must name intent and unit: $boltMd" }
$completed = Get-CompletedStages $frontmatter

$unitDir = Find-Dir (Join-Path (Find-Dir (Join-Path $Worktree 'memory-bank\intents') $intent) 'units') $unit
$logPath = Join-Path $unitDir 'construction-log.md'
$lastExit = $null
if (Test-Path $logPath) { $lastExit = Get-LastStageExit (Read-Utf8 $logPath) }

$relBolt = Get-RelativePath $Worktree $boltMd
$relLog = Get-RelativePath $Worktree $logPath
$stagesText = 'none'
if ($completed.Count -gt 0) { $stagesText = $completed -join ', ' }
if (-not $currentStage) { $currentStage = 'null' }
$exitText = '(none recorded: this is the first stage session for this bolt)'
if ($null -ne $lastExit) { $exitText = $lastExit }

$CheckScript = Join-Path $PSScriptRoot 'check-stage-exit.mjs'
$boltType = Get-FrontmatterValue $frontmatter 'type'
$failureModes = "memory-bank/bolts/$Bolt/failure-modes.jsonl"
$stampsLog = "memory-bank/bolts/$Bolt/test-stamps.jsonl"
$testArtifact = 'test-walkthrough.md'
if ($boltType -eq 'ddd-construction-bolt') { $testArtifact = 'ddd-03-test-report.md' }
$checkCommand = "node .specsmd/aidlc/scripts/check-stage-exit.mjs $Bolt $Stage"
$conditions = "None are checked for this stage. The pre-commit hook still runs the claims lint over any stage artifact you commit."
if ($Stage -eq 'technical-design' -or $Stage -eq 'plan') {
    $conditions = @"
Checked by $checkCommand on the commit that marks the stage complete (pre-commit hook; CI repeats it on the pull request). The stage cannot exit until:
- $failureModes exists: one JSON object per failure mode: {"id":"FM-1","source":"bolt","fails":"<what can fail>","expected":"<what must happen>","test":"<test class or method name>","log":"<log line>"} ("ui": true when the test is a Vitest spec). "test" is the exact fragment you will pass to the test wrapper as --filter (API) or --include (UI). A bolt with genuinely no failure modes writes {"none":true,"reason":"..."} instead.
- Every row is filled from three sources: the bolt's own mechanisms and error paths ("bolt"); the definition-of-done classes the routing table in memory-bank/standards/bolt-process.md maps to what this bolt touches ("class"); the shared invariants the bolt touches ("invariant").
- The adversarial design check ran against the design AND this file and returned rows in the same shape with "source":"attack"; every one is appended with a "disposition": "accepted" (and a test) or "rejected" (and a "reason"). If it found nothing to add, append {"source":"attack","none":true,"reason":"..."}. No attack row = the check did not run = the stage cannot exit.
- The failure-mode table in the design artifact is rendered from this file, not written separately.
"@
} elseif ($Stage -eq 'implement') {
    $conditions = @"
Checked by $checkCommand on the commit that marks the stage complete (pre-commit hook; CI repeats it on the pull request). For every accepted row in $failureModes, in this order, always through the wrapper with --log so the stamps land in ${stampsLog}:
1. Write the test first and watch it fail: node reviews/lib/run-scoped-tests.mjs $Bolt --kind red --filter "<test>" --summary --log $stampsLog   (UI: --ui --include "<name>" instead of --filter). Red means the runner exited non-zero; a test that does not compile yet because the type does not exist is red.
2. Write the code and watch it pass: the same command with --kind green.
3. Prove the test protects the code: node reviews/lib/run-scoped-tests.mjs $Bolt --kind revert-and-rerun --filter "<test>" --summary --log $stampsLog --mutate <production file>:<line>. The wrapper breaks that one line, runs, restores the file and stamps what it did; the run must be red. Never point --mutate at a test file.
4. Run --kind green once more to prove the restore.
One class-level red run covers every row whose test name contains the filter. Stamps are written by the wrapper only, never by hand; keep --no-events for runs that are not failure-mode tests.
"@
} elseif ($Stage -eq 'test') {
    $conditions = @"
Checked by $checkCommand on the commit that marks the stage complete (pre-commit hook; CI repeats it on the pull request). The implement conditions must still hold, and memory-bank/bolts/$Bolt/$testArtifact must carry an "Adversarial design check" heading and a "Fresh-eyes micro-review" heading recording each gate's run and what it found. Every bullet under its Summary, Acceptance Criteria Validation, Test Files and Issues Found headings names a repository path, a file:line or the command that shows it (node .specsmd/aidlc/scripts/lint-claims.mjs <artifact> is the check; the hook runs it).
"@
}
if ($boltType -eq 'spike-bolt') { $conditions = 'Spike bolts are exempt from stage exit conditions.' }
$previousAttempt = ''
if ((Test-Path (Join-Path $boltDir 'failure-modes.jsonl')) -or (Test-Path (Join-Path $boltDir 'test-stamps.jsonl'))) {
    $checkLines = & node $CheckScript $Bolt $Stage --root $Worktree
    if ($LASTEXITCODE -ne 0) {
        $previousAttempt = "`nUnmet exit conditions left by the previous attempt at this stage; meet these first:`n" + ($checkLines -join "`n") + "`n"
    }
}

if ($PromptFile) {
    $prompt = Read-Utf8 $PromptFile
} else {
    $prompt = @"
You are running one stage of one bolt in this worktree: bolt ${Bolt}, stage ${Stage}.

Bolt file: ${relBolt} (status: ${status}; current_stage: ${currentStage}; stages completed: ${stagesText}).
Construction log: ${relLog}.

Last stage exit recorded in the construction log:

${exitText}

Exit conditions for stage ${Stage}:
${conditions}
${previousAttempt}
Do this and nothing more:
1. Read KICKOFF.md in this worktree first: it holds the group rules and the coordinator addendum. Then act as the construction agent: read .specsmd/aidlc/agents/construction-agent.md and execute its bolt-start skill for bolt ${Bolt} at stage ${Stage} (/specsmd-construction-agent is not a command in this harness). The bolt type definition under .specsmd/aidlc/templates/construction/bolt-types/ dictates this stage's activities and artifacts; follow it exactly. memory-bank/standards/bolt-process.md is the lifecycle, memory-bank/standards/definition-of-done.md the hand-back checklist.
2. Work only stage ${Stage}. When its artifacts are written and the exit conditions above are met: update the bolt.md frontmatter (current_stage, stages_completed), append the stage-exit block for ${Bolt} / ${Stage} to ${relLog} in the shape your working rules give, commit, push the branch, and end the turn. The pre-commit hook runs the stage exit check and the claims lint on that commit; a refused commit lists what is missing: meet it, never bypass the hook. Do not start the next stage. If this stage leaves the bolt at status review-pending, first send the hand-off report described in KICKOFF.md to the coordinator session photo-printing-website-68 by SendMessage.
"@
}

$promptPath = Join-Path $Worktree '.stage-prompt.md'
$commandText = "claude -p (Get-Content .stage-prompt.md -Raw -Encoding UTF8) --model $Model --effort $Effort --permission-mode auto --autocompact $Autocompact --exclude-dynamic-system-prompt-sections --append-system-prompt-file `"$RulesPath`""
$costText = "node `"$CostScript`" `"$Worktree`" --since <launch ISO> --bolt $Bolt --stage $Stage --append `"$logPath`""

if ($DryRun) {
    Write-Output "--- prompt (would be written to $promptPath) ---"
    Write-Output $prompt
    Write-Output "--- command (run from $Worktree, with CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0) ---"
    Write-Output $commandText
    Write-Output '--- after claude exits ---'
    Write-Output $costText
    exit 0
}

[System.IO.File]::WriteAllText($promptPath, $prompt, $Utf8NoBom)
Push-Location $Worktree
try {
    $env:CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS = '0'
    $launched = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Write-Host "[launch-stage] $launched  $Bolt / $Stage"
    Write-Host "[launch-stage] $commandText"
    $promptArg = (Get-Content .stage-prompt.md -Raw -Encoding UTF8) -replace '"', '\"'
    & claude -p $promptArg --model $Model --effort $Effort --permission-mode auto --autocompact $Autocompact --exclude-dynamic-system-prompt-sections --append-system-prompt-file $RulesPath
    $claudeExit = $LASTEXITCODE
    Write-Host "[launch-stage] claude exited $claudeExit; measuring the session"
    & node $CostScript $Worktree --since $launched --bolt $Bolt --stage $Stage --append $logPath
    Write-Host "[launch-stage] stage exit check ($checkCommand):"
    & node $CheckScript $Bolt $Stage --root $Worktree
    if ($LASTEXITCODE -ne 0) { Write-Host "[launch-stage] stage $Stage of $Bolt has unmet exit conditions; the next launch of this stage carries the list into its prompt" }
} finally {
    Pop-Location
}
exit $claudeExit
