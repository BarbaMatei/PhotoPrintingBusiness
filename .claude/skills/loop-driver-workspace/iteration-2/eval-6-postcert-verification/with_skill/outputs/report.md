Next pass for 095-postcert is verification. I stopped at the announcement as asked — nothing was executed.

Records are clean first: the auditor found 0 errors and 1 warning (`reviews/index.md` is missing from the fixture root, so index pairing was skipped).

State the router read: latest review v3; pass v3 was a single-lens certification that came back approve-with-followups, outcome certified, carrying 1 new serious finding; resolution v3 is resolved at commit a030db7.

Router verdict (exit 0, row 3): resolution-v3 is resolved but not yet re-reviewed, so the next pass is verification, at roughly 60–250k agent tokens plus main-agent revert-and-rerun work.

Worth flagging: "certified" is not "closed" here. The certification pass itself raised the serious finding, the fix for it has never been independently re-reviewed, and a loop only closes on your word — so verification is the correct next step, not closure.

Say go and I'll run the verification pass.
