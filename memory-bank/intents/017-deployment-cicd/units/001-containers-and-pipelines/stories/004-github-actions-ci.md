---
id: 004-github-actions-ci
unit: 001-containers-and-pipelines
intent: 017-deployment-cicd
status: complete
priority: must
created: 2026-05-25T10:20:00Z
assigned_bolt: 040-containers-and-pipelines
implemented: true
---

# Story: 004-github-actions-ci

## User Story

**As** a contributor
**I want** every PR to run build + tests automatically
**So that** broken changes are caught before merge

## Acceptance Criteria

- [ ] `.github/workflows/ci.yml` triggers on `pull_request` and `push` to any branch except `main`.
- [ ] Jobs (matrix where useful):
  - `dotnet`: restore (cached), build, `dotnet test` with Postgres `services:` container.
  - `web`: `npm ci`, `npm run lint`, `npm run test -- --run`, `npm run build`.
- [ ] Warm-cache wall clock < 8 min.
- [ ] Failing tests fail the workflow.
- [ ] Test result and coverage artefacts uploaded.
- [ ] Branch protection on `main` requires both jobs green to merge.

## Technical Notes

```yaml
name: ci
on: [pull_request, push]
jobs:
  dotnet:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16-alpine
        env: { POSTGRES_USER: fototipar, POSTGRES_PASSWORD: fototipar, POSTGRES_DB: fototipar }
        ports: ["5432:5432"]
        options: >-
          --health-cmd pg_isready
          --health-interval 5s
          --health-timeout 5s
          --health-retries 5
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
      - run: dotnet restore src/PhotoPrint.sln
      - run: dotnet build  src/PhotoPrint.sln --no-restore -c Release
      - run: dotnet test   src/PhotoPrint.sln --no-build  -c Release --logger trx
        env:
          ConnectionStrings__Default: Host=localhost;Database=fototipar;Username=fototipar;Password=fototipar

  web:
    runs-on: ubuntu-latest
    defaults: { run: { working-directory: src/PhotoPrint.UI } }
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '22', cache: 'npm', cache-dependency-path: 'src/PhotoPrint.UI/package-lock.json' }
      - run: npm ci
      - run: npm run lint
      - run: npm test -- --run
      - run: npm run build
```

## Dependencies

### Requires
- 001-api-dockerfile (so the image build is also CI-verifiable)

### Enables
- 005-github-actions-deploy

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Flaky integration test | Quarantine with `[Trait("flaky","true")]` filter; do not retry-loop in CI |
| Cache key invalidates often | Document `npm ci` warm-cache assumption |

## Out of Scope

- Code-coverage gate (defer until coverage baseline measured).
