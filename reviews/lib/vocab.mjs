// Compatibility re-export: records/schema.mjs is the home. This path is permanent — schema.test.mjs
// pins that it still serves the same objects — so it stays even after every caller moves.
export * from './records/schema.mjs'
