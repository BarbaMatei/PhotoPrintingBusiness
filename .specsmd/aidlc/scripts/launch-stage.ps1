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
    return $m.Groups[1].Value.Trim('"', "'")
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

if ($PromptFile) {
    $prompt = Read-Utf8 $PromptFile
} else {
    $prompt = @"
You are running one stage of one bolt in this worktree: bolt ${Bolt}, stage ${Stage}.

Bolt file: ${relBolt} (status: ${status}; current_stage: ${currentStage}; stages completed: ${stagesText}).
Construction log: ${relLog}.

Last stage exit recorded in the construction log:

${exitText}

Do this and nothing more:
1. Invoke /specsmd-construction-agent --bolt-id="${Bolt}" --stage="${Stage}" (the construction agent runs its bolt-start skill for that id at that stage). The bolt type definition under .specsmd/aidlc/templates/construction/bolt-types/ dictates this stage's activities and artifacts; follow it exactly. memory-bank/standards/bolt-process.md is the lifecycle, memory-bank/standards/definition-of-done.md the hand-back checklist.
2. Work only stage ${Stage}. When its artifacts are written: update the bolt.md frontmatter (current_stage, stages_completed), append the stage-exit block for ${Bolt} / ${Stage} to ${relLog} in the shape your working rules give, commit, and end the turn. Do not start the next stage.
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
} finally {
    Pop-Location
}
exit $claudeExit
