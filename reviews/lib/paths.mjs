// Compatibility re-export: records/schema.mjs is the home of the path constants; this path stays
// until every caller moves. The ten exports nothing imported (README, CONTRACTS, METRICS_SCHEMA,
// both RUNBOOK_*, RATIONALE, LOOP_DESIGN, ID_MAP, ARCHIVE, targetDir) are gone — a target folder
// is resolved by model/target.mjs, and a prose path belongs in the markdown link that uses it.
export * from './records/schema.mjs'
