---
stage: test
bolt: 040-containers-and-pipelines
created: 2026-05-27T10:15:00Z
---

## Test Report: 001-containers-and-pipelines

### Summary

- **Unit + integration tests**: 457/457 passed, 0 failed, 0 skipped (`dotnet test PhotoPrint.sln`, ~7 s).
- **API build**: succeeds, 0 errors (pre-existing Stripe NU1603 + EF1002/CS1998 warnings only).
- **YAML**: well-formed by static check (no tab indentation; expected top-level keys present).
- **Scope note (D5)**: container build, CI/CD green runs, and the live HTTPS check are **operator-verified** — Docker is not installed on this machine and GitHub Actions / a live site cannot run locally.

### What this stage actually verifies

This is an ops/config bolt; most artefacts (Dockerfile, Compose, Caddy, workflows) are only
truly exercised by infrastructure that doesn't exist yet. So Stage 3 here proves the **code
change is safe** and the **config is well-formed**, and defers the rest to a documented runbook.

### Tests / checks run

- [x] `dotnet test PhotoPrint.sln` — full suite green, confirming the merged bolt-035 idempotency code AND the three `Program.cs` changes (SPA serving, Postgres boot-migrate, payment `ValidateOnStart`) introduce **no regression**. The InMemory test host is unaffected because the migrate path is `IsNpgsql()`-gated and payment validation is Production-gated.
- [x] `dotnet build src/PhotoPrint.API` — clean compile of the new `Program.cs` pipeline + options wiring.
- [x] YAML static sanity for `ci.yml`, `deploy.yml`, `docker-compose.yml`, `docker-compose.prod.yml` — no tabs, correct `name/on/jobs` and `name/services/volumes` structure. (No offline YAML parser / Docker available for a full schema validation.)

### Acceptance Criteria Validation

- ✅ **Dockerfile** present: multi-stage, non-root, serves SPA + API, `/health` HEALTHCHECK. *(build executed on a Docker host — operator)*
- ✅ **dev compose**: API + Postgres 16 (healthcheck-gated) + MailHog; named volumes. *(`up` — operator)*
- ✅ **prod compose + Caddy**: TLS edge → API, API not host-exposed, HSTS. *(LE issuance — operator)*
- ✅ **ci.yml / deploy.yml**: PR/push CI; CD builds+pushes GHCR and deploys on `main` (SSH step self-skips with no host). *(green run — operator)*
- ✅ **.env.example + boot validation**: every var documented; missing required Production secret fails boot with the field named (`OptionsValidationException`). *(verified by code + test; live boot — operator)*
- ✅ **README + runbook**: "Run with Docker" + `docs/DEPLOYMENT.md` (proposals, provisioning, secrets, deploy, rollback).

### Issues Found

- **Migration provider inconsistency** (pre-existing, surfaced by the merge): `20260527075359_AddOrderIdempotencyKey` is SQLite-flavoured (`TEXT`, plain unique index). `TEXT` is valid on Postgres so boot-migrate won't crash, but it should be verified/regenerated against a real Postgres before first deploy. Documented in `docs/DEPLOYMENT.md` §7 and the walkthrough; **not** fixed here (out of scope for containers/pipelines).

### Notes

- Six stories (001–006) are delivered as repo config + one code change; their live acceptance is the `docs/DEPLOYMENT.md` §8 checklist, to be run when infrastructure exists.
- **CI `web` job corrected (post-review fix):** this UI is Angular 21 on the **Vitest** runner (`@angular/build:unit-test`), not Karma. The initial job used Karma-era flags (`--browsers=ChromeHeadless`) and a `lint` step that don't apply here, which failed the job. Fixed to `npm test -- --watch=false` + `npm run build -- --configuration=production`; dropped the lint step (no ESLint/`lint` script configured). **UI verified locally:** prod `ng build` succeeds (output `dist/PhotoPrint.UI/browser` — matches the Dockerfile COPY) and `ng test --watch=false` passes **46 files / 395 tests**.
