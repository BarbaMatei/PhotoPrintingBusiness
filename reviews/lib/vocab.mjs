// Machine copies of vocabulary the gates share. Prose authorities: the lens manifest
// table in runbooks/runbook-discovery.md, the area table in rules/doc-contracts.md.
// A change there changes this file in the same commit (the descriptive-standards rule).

export const MANIFEST_LENSES = [
  'correctness', 'security', 'requirements', 'quality', 'tests-coverage',
  'completeness-critic', 'db-parity', 'input-validation',
  'observability', 'race', 'frontend-ux',
]

export const AREAS = ['payments', 'orders', 'shipping', 'uploads', 'gallery', 'auth',
  'edge', 'observability', 'jobs', 'data', 'tests', 'records']

// Fix-round lines and the runtime split exist only from this date; earlier rounds never get one.
export const V3_CUTOFF = '2026-08-03'

// Rules from the accepted fix-round audit apply to rounds closed on/after this date;
// every earlier record is grandfathered (doc-contracts.md, "Grandfathering cut-offs").
export const V4_CUTOFF = '2026-08-28'
