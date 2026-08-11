---
type: review
target: 043-cloud-storage-provider
version: 1
supersedes: null
commit: 5706580
branch: feat/bolt-043-cloud-storage-provider
pass-type: discovery
date: 2026-07-14
lenses: [correctness, security, race, tests-coverage, completeness-critic]
lenses-not-run: [db-parity, observability, input-validation, requirements, frontend-ux]
verdict: request-changes
blockers: [D1]
findings: { high: 1, medium: 6, low: 11, cleanup: 0, refuted: 1 }
tests: { dotnet: "661/661 (+7 skipped MinIO)", frontend: "423/423" }
---

# Review v1 — 043-cloud-storage-provider

## Findings

| F# | D# | Sev | Title | File | Fix now? |
|---|---|---|---|---|---|
| F1 | D1 | 🔴 | Admin ZIP fulfilment download reads promoted originals from the local tier only | `Services/AdminOrderService.cs:168` | yes |
| F2 | D2 | 🟠 | Cleanup job deletes Cloud uploads against the local tier and never deletes the large preview | `BackgroundJobs/UploadCleanupJob.cs:67` | yes |
| F3 | D3 | 🟠 | Missing cloud original throws `AmazonS3Exception`, not `FileNotFoundException` → preview 500 | `Services/S3StorageService.cs:91` | yes |
| F4 | D4 | 🟠 | Purge on Shipped fires once and skips an in-flight promotion → original never purged until reboot | `Services/AdminOrderService.cs:136` | yes |
| F5 | D5 | 🟠 | Presigned-URL lifetime and the hardcoded `Cache-Control` max-age diverge → expired images | `Controllers/UploadsController.cs:185` | yes |
| F6 | D6 | 🟠 | Promotion worker disposes the concurrency semaphore while tasks are still in flight | `BackgroundJobs/OrderPhotoPromotionWorker.cs:108` | yes |
| F7 | D7 | 🟠 | Migration dropping the `FilePath` NOT NULL constraint is unverified by any test | `Migrations/…MakeUploadFilePathNullable.cs` | yes |
| F8 | D8 | 🟡 | Preview read races promotion deleting the local thumbnail → 500 instead of 404 | `Controllers/UploadsController.cs:190` | no |
| F9 | D9 | 🟡 | Duplicate payment webhooks race `Order.Status`; there is no concurrency token | `Controllers/WebhooksController.cs:218` | no |
| F10 | D10 | 🟡 | 403 rather than 404 for another user's order tells an attacker which order ids exist | `Services/OrderService.cs:468` | no |
| F11 | D11 | 🟡 | `/photos` returns presigned URLs with no `Cache-Control: private` | `Controllers/OrdersController.cs:82` | no |
| F12 | D12 | 🟡 | Guest-placed orders cannot reach the new `/photos` endpoint | `Controllers/OrdersController.cs:10` | no |
| F13 | D13 | 🟡 | Empty-state copy collapses four causes into one permanent "no longer available", with no retry | `UI/…/order-detail-page.ts:103` | no |
| F14 | D14 | 🟡 | Cloud preview regeneration branch never runs — every test presets the thumbnail path | `Tests/…/CloudPreviewIntegrationTests.cs:225` | no |
| F15 | D15 | 🟡 | `GET /orders/{id}/photos` has no integration test, so its auth pipeline is unproven | `Controllers/OrdersController.cs:73` | no |
| F16 | D16 | 🟡 | Promoter row-update-failure and preview-generation-failure branches untested | `Services/OrderPhotoPromoter.cs:198` | no |
| F17 | D17 | 🟡 | Paid-then-cancelled originals: purge behaviour undecided and untested | `BackgroundJobs/OriginalPurgeRecoveryScanner.cs:54` | no |
| F18 | D18 | 🟡 | `BackfillCommand` and `S3BucketVerifier` have no tests at all | `Cli/BackfillCommand.cs:42` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The cloud storage adapter's coverage hinges on a local MinIO container, so regressions are invisible | The continuous-integration workflow runs on every pull request and non-main push, starts MinIO, health-checks it and sets the four storage test variables, so every skippable cloud test executes there. The local skip is deliberate and backed by that run. One residue stands and is tracked under D3 and D16: transient-failure classification and multipart upload are unproven even with MinIO up, because nothing injects faults and the payloads are tiny. |

## Notes for the fixer

- This was a lean pass by owner choice. Five lenses ran; database parity, observability, input validation, requirements and frontend behaviour did not, so their surface is under-reviewed and this pass cannot claim the search is complete.
- Fix D1 before merge. D1, D2 and D8 are one root cause: collaborators still resolve the default local storage service instead of routing by the upload's recorded tier. Fix all three the same way, and add D3's exception translation while you are in the upload service.
- D5 has a second half the lean pass cannot settle: the lightbox large URL is signed at page load and can expire before the user opens it. It needs the frontend lens that did not run. It is on the ledger as D5b.
- D12 and D17 are decisions before they are fixes. Confirm whether guest order history is in scope, and whether a cancelled order's original should be kept or purged.
- D4 and D7 have a retention and database-parity flavour and pair naturally with the parity lens this pass skipped.
- D9 overlaps the payment-idempotency work. Check whether that work already covers it before writing anything here.
- Every fix needs a regression test that fails when the fix is reverted.
- Security came back strong: the new photos endpoint is ownership-scoped, signed URLs cover only thumbnail and preview keys, and no credential or signed URL reaches the logs or responses.
- This target owes a full-manifest discovery pass and a certification pass before closure. A lean pass is a sample, not a sweep, and the absence of more High findings is not evidence of cleanliness.
