---
id: 003-serilog-configuration
unit: 001-error-handling-logging
intent: 001-foundation-infrastructure
status: complete
priority: must
created: 2026-05-05T15:25:00Z
assigned_bolt: null
implemented: true
---

# Story: 003-serilog-configuration

## User Story

**As a** developer
**I want** structured logging with Serilog configured for both development and production
**So that** I can debug issues in dev with readable output and analyze production logs with JSON tools

## Acceptance Criteria

- [ ] **Given** the app runs in Development, **When** a log is written, **Then** output is human-readable on the console
- [ ] **Given** the app runs in Production, **When** a log is written, **Then** output is JSON format to a file with daily rolling and 30-day retention
- [ ] **Given** any log entry, **When** examined, **Then** it contains CorrelationId, MachineName, ThreadId, and RequestPath enrichment
- [ ] **Given** a sensitive value (password, token, API key), **When** logged accidentally, **Then** it is NOT present in log output (Serilog destructuring policies)

## Technical Notes

- NuGet packages: `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`
- Configure in `Program.cs` using `UseSerilog()`
- Development: Console sink with `outputTemplate` including timestamp, level, correlationId, message
- Production: File sink with `CompactJsonFormatter`, daily rolling, 30-day `retainedFileCountLimit`
- Enrichment: `.Enrich.WithCorrelationId()` (custom enricher), `.Enrich.WithMachineName()`, `.Enrich.WithThreadId()`
- Add Serilog config section to `appsettings.json`

## Dependencies

### Requires
- 002-correlation-id-middleware (provides CorrelationId for enrichment)

### Enables
- All subsequent stories and units (they all use logging)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Log file disk full | Serilog fails silently (self-log to console if available) |
| Very high log volume | File rolling handles daily rotation; 30-day retention prevents disk fill |

## Out of Scope

- Log aggregation (Seq, ELK, Application Insights) — future enhancement
- Request/response body logging — too verbose for default config
