---
intent: 027-architectural-layering
phase: inception
status: context-defined
updated: 2026-06-05T09:30:00Z
---

# Architectural Layering - System Context

## System Overview

A **pure internal refactor** of `PhotoPrint.API` — no external actors, no API surface change, no behaviour change. It reshapes the single assembly into Presentation / Application / Domain / Infrastructure layers, introduces the `Abstractions/` convention, locks the no-repository posture with an analyzer, and adds a handler-per-use-case pattern. The "actors" are the developers who work in the code and the tooling (compiler, analyzers, EF migration generator, CI) that enforce correctness.

## Context Diagram

```mermaid
C4Context
    title System Context - Architectural Layering (internal refactor)

    Person(dev, "Developer", "Works within the layered structure")
    Person(reviewer, "Reviewer", "Checks layer rules per PR")
    System(api, "PhotoPrint.API", "Single ASP.NET Core 8 assembly, re-layered")
    System_Ext(roslyn, "Roslyn analyzers", "BannedApiAnalyzers enforce layer + IQueryable rules")
    System_Ext(ef, "EF Core migration tool", "Verifies zero schema drift")
    System_Ext(ci, "CI", "build + test green after every PR")

    Rel(dev, api, "Edits within Web/Application/Domain/Infrastructure")
    Rel(reviewer, api, "Reviews per-PR namespace shuffles")
    Rel(api, roslyn, "Layer + IQueryable rules enforced at build")
    Rel(api, ef, "Add-Migration NoOpVerify → empty diff")
    Rel(api, ci, "build/test gate each PR")
```

## External Integrations

- **Roslyn analyzers** (`Microsoft.CodeAnalysis.BannedApiAnalyzers`): enforce `Domain/` may not reference EF Core/HttpClient, and no service returns `IQueryable<T>`.
- **EF Core migration tooling**: each refactor PR must produce an empty `Add-Migration` diff (zero schema drift).
- **CI**: `dotnet build && dotnet test` green after every PR — the non-negotiable safety gate.

## High-Level Constraints

- NO new csproj — folder + namespace layering inside one assembly (P22 ADR records why).
- Sequenced PRs: P22 → P21-PR1..PR5 → P23 → P24 → P25; each individually mergeable.
- Lockstep with intent 028 (test architecture) — every structural PR breaks ~25 test files otherwise.

## Key NFR Goals

- Zero behaviour change; 941/948 test baseline maintained after every PR.
- Zero EF migration drift.
- Use-case inventory is grep-able (`find Application -name '*Handler.cs'`).
- Layer violations blocked at PR (analyzer or review checklist).
