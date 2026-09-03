#!/bin/sh
# Re-runs every adapted brief-test behind the verdicts in ../test-walkthrough.md and
# ../../086-phase-1-skeleton-agents/test-walkthrough.md. Read-only on the repo: the only writes
# go to a throwaway tree under $TMPDIR, removed on exit. Run from the repo root:
#   sh memory-bank/bolts/085-phase-1-skeleton-core/evidence/rerun-evidence.sh
# Its output is captured in evidence-log.txt beside this file.
set -u
ROOT=$(pwd)
WORK=$(mktemp -d 2>/dev/null || mktemp -d -t reverify)
trap 'rm -rf "$WORK"' EXIT
say() { printf '\n===== %s =====\n' "$1"; }
run() { printf '$ %s\n' "$*"; "$@" 2>&1; printf '[exit %s]\n' "$?"; }

say "A1 — a fresh ledger, two defects, the human view (Prompt 1 test a)"
mkdir -p "$WORK/wtA/reviews/state"
cp -r "$ROOT/reviews/templates" "$WORK/wtA/reviews/"
echo 9000 > "$WORK/wtA/reviews/state/id-counter"
run node reviews/lib/mint-id.mjs --root "$WORK/wtA" scaffold-ledger 900-alpha --id PPW-9000 --sev 🔴 --title "a defect in alpha" --file "Alpha.cs:10" --pass v1
run node reviews/lib/mint-id.mjs --root "$WORK/wtA" scaffold-ledger 900-alpha --id PPW-9001 --sev 🟠 --title "a second defect in alpha" --file "Alpha.cs:44" --pass v1
cat "$WORK/wtA/reviews/900-alpha/ledger.md"

say "A2 — the same id twice in one target is refused"
run node reviews/lib/mint-id.mjs --root "$WORK/wtA" scaffold-ledger 900-alpha --id PPW-9001 --sev 🟠 --title "same id again" --file "Alpha.cs:44" --pass v2

say "A3 — the coverage read: what has and has not been hunted (Prompt 1 test c)"
run node reviews/lib/drive/route-next-pass.mjs --root reviews/lib/tests/fixtures/drive-states 913-loop-quiet

say "A4 — six concurrent mints against one counter, same checkout"
echo 9000 > "$WORK/wtA/reviews/state/id-counter"
for _ in 1 2 3 4 5 6; do node reviews/lib/mint-id.mjs --root "$WORK/wtA" mint --count 2 & done
wait
printf 'counter after 6 x --count 2 (12 ids expected): '; cat "$WORK/wtA/reviews/state/id-counter"

say "A5 — THE GAP: two worktrees mint the same id into different targets"
for w in wt1 wt2; do
  mkdir -p "$WORK/$w/reviews/state"
  cp -r "$ROOT/reviews/templates" "$WORK/$w/reviews/"
  echo 9000 > "$WORK/$w/reviews/state/id-counter"
done
run node reviews/lib/mint-id.mjs --root "$WORK/wt1" mint --count 1
run node reviews/lib/mint-id.mjs --root "$WORK/wt2" mint --count 1
run node reviews/lib/mint-id.mjs --root "$WORK/wt1" scaffold-ledger 900-alpha --id PPW-9000 --sev 🔴 --title "a defect in alpha" --file "Alpha.cs:10" --pass v1
run node reviews/lib/mint-id.mjs --root "$WORK/wt2" scaffold-ledger 901-beta --id PPW-9000 --sev 🟠 --title "an unrelated defect in beta" --file "Beta.cs:20" --pass v1
echo "(exit 0 on the second scaffold = the duplicate was NOT caught)"

say "A6 — an out-of-band edit to a published record is refused"
cp -r "$ROOT/reviews/lib/tests/fixtures/repo" "$WORK/fx"
(cd "$WORK/fx" && git init -q . && git -c user.email=t@t -c user.name=t add -A >/dev/null 2>&1 && git -c user.email=t@t -c user.name=t commit -qm baseline >/dev/null 2>&1)
run node reviews/lib/records/doc-gate.mjs --root "$WORK/fx" 901-good-target 1
sed -i '20s/.*/- **What:** Rewritten after the fact — an out-of-band edit./' "$WORK/fx/reviews/901-good-target/ledger.md"
run node reviews/lib/records/doc-gate.mjs --root "$WORK/fx" 901-good-target 1

say "A7 — a dismissal with no reason is refused (Prompt 5 test c)"
cp -r "$ROOT/reviews/lib/tests/fixtures/repo" "$WORK/fx2"
R="$WORK/fx2/reviews/901-good-target/resolution-v1.md"
sed -i 's/^| PPW-9002 | backlog | — | .* |$/| PPW-9002 | wont-fix | — | Dismissed. |/' "$R"
awk '/^### Send the retry-count gap to the queue \(PPW-9002\)$/{skip=1} skip&&/^$/{next} /^Low severity, no user impact/{next} {if(!skip)print}' "$R" > "$R.tmp" && mv "$R.tmp" "$R"
run node reviews/lib/records/doc-gate.mjs --root "$WORK/fx2" 901-good-target 1

say "A8 — an owner summary over 60 lines is refused (the guide's v3.7 test for Prompt 4)"
cp -r "$ROOT/reviews/lib/tests/fixtures/repo" "$WORK/fx3"
S="$WORK/fx3/reviews/901-good-target/summary-v1.md"
i=1; while [ "$i" -le 60 ]; do echo "- padding line $i to push the body past the cap" >> "$S"; i=$((i+1)); done
run node reviews/lib/records/doc-gate.mjs --root "$WORK/fx3" 901-good-target 1

say "A9 — the clean baseline the A6-A8 refusals are measured against"
run node reviews/lib/doc-gate.mjs state
run node reviews/lib/records/doc-gate.mjs --root reviews/lib/tests/fixtures/repo 901-good-target 1

say "A10 — the loop-driver eval suite's read-only verify lines (bolt 086, story 007)"
F=".claude/skills/loop-driver/evals/fixtures/loop-states"
for t in 094-quiet 091-fixround 096-rotten 095-postcert 097-mockpass; do
  run node reviews/lib/records-auditor.mjs --root "$F" "$t"
done
run node reviews/lib/drive/route-next-pass.mjs --root "$F" 094-quiet

say "A11 — the machine-wide test lock refuses a second test process (bolt 086, story 007)"
mkdir -p "$WORK/lk/reviews/900-lock"
node reviews/lib/fix/run-scoped-tests.mjs --root "$WORK/lk" 900-lock --kind baseline --filter X \
  --no-events --cmd "node -e \"setTimeout(()=>console.log('# pass 1'),2500)\"" > "$WORK/lk/first.txt" 2>&1 &
sleep 1
run node reviews/lib/fix/run-scoped-tests.mjs --root "$WORK/lk" 900-lock --kind baseline --filter Y \
  --no-events --cmd "node -e \"console.log('# pass 1')\""
echo "(exit 3 = the lock held; the first process still had it)"
wait

say "T1-T5 — the fixture suite, whole and per seam"
run node reviews/lib/tests/run-tests.mjs
for only in route-next-pass doc-gate mint-id records; do
  run node reviews/lib/tests/run-tests.mjs --only "$only"
done
