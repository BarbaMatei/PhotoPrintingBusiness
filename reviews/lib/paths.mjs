// Compatibility re-export: records/schema.mjs is the home of the path constants. Nothing imports
// this path any more (the ten exports nothing used — README, CONTRACTS, METRICS_SCHEMA, both
// RUNBOOK_*, RATIONALE, LOOP_DESIGN, ID_MAP, ARCHIVE, targetDir — are gone; a target folder is
// resolved by model/target.mjs, and a prose path belongs in the markdown link that uses it), so
// unlike vocab.mjs this file is a delete candidate for the next session, not a permanent shim.
export * from './records/schema.mjs'
