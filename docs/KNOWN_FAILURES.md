# Known Test Failures

Read this before filing a bug about a red test. Every entry here is a test that does not pass
on a plain developer machine **for a known reason that is not a defect in the product**. If a
test fails and is not described here, it is a real failure — treat it as one.

**Verified 2026-09-04** by reading the test sources, not by running the suite. What that means
for the numbers below is stated in "What this register cannot tell you".

---

## 1. The S3 suite skips without MinIO

**Surface**: `src/PhotoPrint.Tests/Integration/S3StorageServiceIntegrationTests.cs` — one class,
**10 tests**.

**Mechanism**: each test is a `[SkippableFact]` whose first line is
`Skip.IfNot(_fx.Available, MinioFixture.SkipReason)`. The fixture sets `Available` from four
environment variables being non-empty — `STORAGE_TEST_ENDPOINT`, `STORAGE_TEST_ACCESS_KEY`,
`STORAGE_TEST_SECRET_KEY`, `STORAGE_TEST_BUCKET` — with no network probe, so an unset
environment costs nothing and never hangs.

```sh
grep -c 'SkippableFact' src/PhotoPrint.Tests/Integration/S3StorageServiceIntegrationTests.cs
grep -c 'Skip.IfNot'   src/PhotoPrint.Tests/Integration/S3StorageServiceIntegrationTests.cs
```

Both return 10 once the one prose mention of `[SkippableFact]` in the fixture's doc comment is
discounted, so every skippable test is gated and none can silently pass unverified.

**What you see**: 10 skipped, 0 failed.

**Why it is not a bug**: these are the only tests that exercise real S3 protocol behaviour.
Running them needs an S3-compatible server. CI starts a MinIO container and exports all four
variables, so they run on every push.

**Tracking**: none needed — the gate is the explanation. To run them locally, start MinIO and
export the four variables.

---

## 2. Seventeen PostgreSQL-backed classes **error** — they do not skip

**Surface**: 16 test classes reference `PhotoPrint.Tests.Helpers.PostgresTestDatabase` directly,
plus `Integration/PaymentIdempotencyRelationalTests` which reaches it through
`Integration/PostgresPaymentFactory`. **Four are under `Integration/`; thirteen are under
`Unit/`.**

```sh
grep -rl 'PostgresTestDatabase' src/PhotoPrint.Tests --include=*.cs
# 18 files: the 16 test classes, plus Helpers/PostgresTestDatabase.cs and
# Integration/PostgresPaymentFactory.cs, which are not test classes.
grep -rl 'PostgresPaymentFactory' src/PhotoPrint.Tests --include=*.cs
```

**Mechanism**: the fixture's constructor catches `NpgsqlException` and rethrows
`InvalidOperationException` with the instruction — *"These tests need a reachable PostgreSQL
server … Start PostgreSQL locally or set `POSTGRES_TEST_CONNECTION` to a connection string whose
role may `CREATE DATABASE`."* This is deliberate: a silently-skipped relational test proves
nothing, and the relational layer is where unique indexes, check constraints, `jsonb`, decimal
precision and `ExecuteUpdateAsync` semantics are the behaviour under test. The reasoning and the
fixture's design are in
[`memory-bank/standards/data-stack.md`](../memory-bank/standards/data-stack.md).

**What you see**: errors, not skips — an exception per test, with that message.

**The precondition is not simply "PostgreSQL is installed"**. The fixture needs either a server
at `localhost:5432` reachable as `postgres`/`postgres`, or `POSTGRES_TEST_CONNECTION` pointing
somewhere, and in both cases a role allowed to `CREATE DATABASE`. A developer running PostgreSQL
under a different role, or under a role without that privilege, sees exactly the same errors as
one with no server at all.

**The trap worth knowing**: thirteen of the seventeen sit in `Unit/` namespaces, so the scoped
filters in `CLAUDE.md` — `--filter "FullyQualifiedName~PhotoPrint.Tests.Unit.Services"` and its
siblings — run straight into them. "Just run the unit tests" is not a way to avoid needing a
database.

**Why it is not a bug**: PostgreSQL 16 is the only provider in every environment, so these
tests test the real thing. CI provisions a `postgres:16-alpine` service and exports
`POSTGRES_TEST_CONNECTION`.

**Tracking**: none needed — the gate is the explanation, and it is loud by design.

---

## 3. Where the "7 consistently-failing tests" went

An architecture review on 2026-06-03 read a test count of `941/948` and concluded that seven
tests fail consistently, guessing they were the CI-gated S3 skips. That number does not survive
contact with the sources:

- **Skips are not failures.** The S3 suite skips, cleanly, and there are 10 of them, not 7.
- **The relational classes are the ones that actually go red locally**, and they error rather
  than skip — a category the original count had no room for.
- **The figure is stale.** It was taken three months and several features ago; the suite has
  grown substantially since.

So there is no set of seven consistently-failing tests to enumerate. The two mechanisms above
are the whole of the known-non-passing surface. Nothing has been given a fabricated reason to
make the count come out at seven.

---

## 4. Checked and clean

Searched for, and not found — recorded so the next person does not repeat the search:

- **No skipped or disabled frontend specs.** `grep -rn 'describe\.skip\|it\.skip\|test\.skip\|\.todo\|xit\|xdescribe\|fdescribe\|fit(' src/PhotoPrint.UI/src --include=*.ts` returns nothing. No `.only` either, which would silently narrow a run.
- **No `Fact(Skip = "…")` anywhere** in the API test project — every skip in the suite goes
  through the `SkippableFact` gate above.
- **No test depends on the network, on Docker, on a real clock or on the host operating
  system.** Time is driven through `TimeProvider` and `FakeTimeProvider`; there are no
  `Thread.Sleep` waits, no `DateTime.Now`, no time-zone lookups by id, and no OS-platform
  branches in the tests.
- **Nothing here looks like a genuine defect being normalised.** Every non-passing surface
  traces to an absent local service, and no test asserts wrong behaviour in order to stay
  green. If that ever changes, the entry belongs in the review backlog as a defect, not in this
  file as an expectation.

---

## What this register cannot tell you

This document was written by reading the test sources, deliberately without running the suite —
a full run saturates the machine, and this change touches no code that a test could cover. So:

- The counts above are **classes and gates**, counted by the commands shown. They are exact.
- There is **no pass/fail tally** here, and there should not be one until someone runs the suite
  and records the result. A number in a document that nobody re-measures is how the "7 failing
  tests" claim survived three months.
- If a run turns up a failure that is neither of the two mechanisms above, it is new. File it.

## Keeping this register true

Section 5.4 of the [quarterly audit checklist](ARCHITECTURE_AUDIT_CHECKLIST.md) checks this file
against the gates actually present in the test project: an entry for a gate that no longer
exists, or a gate with no entry here, is a finding.
